using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Windows;

namespace STS2RitsuLib.Ui.Files
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A themed filesystem picker with file, directory and save modes, controller navigation,
    ///         image thumbnails, and persistent favorites, recent directories and display preferences.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用库主题的文件系统选择器，支持文件、目录及保存模式、手柄导航、图像缩略图，
    ///         并保存收藏夹、最近目录及显示偏好。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Use Show on the Godot main thread. Each dialog owns its modal layer and is freed on completion or
    ///         when its owner leaves the scene tree. Opening a save dialog selects a path; it does not write the file.
    ///         Only explicitly confirming Create Folder creates a filesystem directory.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 Godot 主线程调用 Show。每个对话框持有独立模态层，完成或所属节点离开场景树时释放。
    ///         保存对话框只选择路径，不写入文件；只有显式确认新建文件夹才会创建文件系统目录。
    ///     </para>
    /// </remarks>
    public sealed partial class RitsuFileDialog : Control, IScreenContext
    {
        private static readonly List<RitsuFileDialog> OpenDialogs = [];
        private readonly RitsuFileThumbnailCache _thumbnails = new();
        private readonly List<string> _history = [];
        private readonly List<Control> _regions = [];
        private readonly List<LineEdit> _edits = [];
        private readonly List<Label> _labels = [];
        private RitsuFileDialogOptions _options = new();
        private Action<IReadOnlyList<string>>? _selected;
        private Action? _canceled;
        private CanvasLayer _layer = null!;
        private RitsuFloatingWindow _window = null!;
        private Control? _previousFocus;
        private Control? _lastFocus;
        private Input.MouseModeEnum _previousMouseMode;
        private RitsuFileList _files = null!;
        private RitsuFileList _favorites = null!;
        private RitsuFileList _recent = null!;
        private LineEdit _path = null!;
        private LineEdit _filename = null!;
        private LineEdit _nameFilter = null!;
        private Control _nameFilterRow = null!;
        private Label _status = null!;
        private ModSettingsTextButton _back = null!;
        private ModSettingsTextButton _forward = null!;
        private ModSettingsTextButton _favorite = null!;
        private ModSettingsTextButton _hidden = null!;
        private ModSettingsTextButton _grid = null!;
        private ModSettingsTextButton _list = null!;
        private ModSettingsTextButton _filterToggle = null!;
        private ModSettingsTextButton _accept = null!;
        private ModSettingsDropdownChoiceControl<string> _drives = null!;
        private Control? _prompt;
        private Control? _promptFocus;
        private Control? _beforePromptFocus;
        private bool _keyboardOpen;
        private bool _closing;
        private bool _attached;
        private bool _loading;
        private int _historyPosition = -1;
        private int _filterIndex;
        private string _directory = "";
        private RitsuFileEntry[] _entries = [];
        private RitsuFileEntry[] _visibleEntries = [];
        private string[][] _filterPatterns = [];
        private CancellationTokenSource? _listingCancellation;
        private Task<RitsuFileEntry[]>? _listing;
        private string _listingDirectory = "";
        private int? _listingHistoryPosition;
        private double _thumbnailRefresh;

        /// <summary>
        ///     <para xml:lang="en">Initializes an unattached dialog for Godot. Use Show to configure and display a picker.</para>
        ///     <para xml:lang="zh-CN">为 Godot 初始化未挂载的对话框；使用 Show 配置并显示选择器。</para>
        /// </summary>
        public RitsuFileDialog()
        {
            MouseFilter = MouseFilterEnum.Stop;
        }

        internal static RitsuFileDialog? ActiveDialog =>
            OpenDialogs.LastOrDefault(dialog => IsInstanceValid(dialog) && dialog.IsInsideTree() && !dialog._closing);

        Control IScreenContext.DefaultFocusedControl =>
            _promptFocus is { } promptFocus && IsInstanceValid(promptFocus) && promptFocus.IsVisibleInTree()
                ? promptFocus
                : _lastFocus is { } lastFocus && IsInstanceValid(lastFocus) && lastFocus.IsVisibleInTree()
                    ? lastFocus
                    : _files;

        /// <summary>
        ///     <para xml:lang="en">Opens a modal picker above its owner, retaining the owner's focus for restoration.</para>
        ///     <para xml:lang="zh-CN">在所属节点上方打开模态选择器，保留所属窗口焦点以便关闭后恢复。</para>
        /// </summary>
        /// <param name="owner">
        ///     <para xml:lang="en">A live node in the main viewport's scene tree. Its lifetime bounds the dialog's lifetime.</para>
        ///     <para xml:lang="zh-CN">主视口场景树中的有效节点；其生命周期决定对话框的最长生命周期。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Selection and persistence options, validated and copied before opening.</para>
        ///     <para xml:lang="zh-CN">选择及持久化选项，在打开前校验并复制。</para>
        /// </param>
        /// <param name="onSelected">
        ///     <para xml:lang="en">
        ///         Called once on the main thread after the modal closes with a read-only snapshot of absolute paths.
        ///         Save mode may return a new path. Recoverable callback exceptions are logged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         模态窗口关闭后，在主线程调用一次，传入绝对路径的只读快照；保存模式可返回新路径。
        ///         可恢复的回调异常会记录到日志。
        ///     </para>
        /// </param>
        /// <param name="onCanceled">
        ///     <para xml:lang="en">Optional callback called once on cancel or owner removal, instead of onSelected.</para>
        ///     <para xml:lang="zh-CN">取消或所属节点被移除时调用一次的可选回调，与 onSelected 互斥。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The displayed dialog. It frees itself; callers may cancel it while it remains valid.</para>
        ///     <para xml:lang="zh-CN">已显示的对话框；它会自行释放，调用方可在其有效期间取消。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">An option or required argument is invalid.</para>
        ///     <para xml:lang="zh-CN">选项或必需参数无效。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The owner is invalid or is not inside the main viewport's scene tree.</para>
        ///     <para xml:lang="zh-CN">所属节点无效或不在主视口的场景树中。</para>
        /// </exception>
        public static RitsuFileDialog Show(Node owner, RitsuFileDialogOptions options,
            Action<IReadOnlyList<string>> onSelected, Action? onCanceled = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(onSelected);
            if (!IsInstanceValid(owner) || !owner.IsInsideTree() || owner.GetViewport() != owner.GetTree().Root)
                throw new InvalidOperationException("The picker owner must be inside the main viewport's scene tree.");
            var captured = options.Snapshot();
            var dialog = new RitsuFileDialog
            {
                _options = captured,
                _selected = onSelected,
                _canceled = onCanceled,
                _previousFocus = owner.GetViewport().GuiGetFocusOwner(),
                _previousMouseMode = Input.MouseMode,
                Name = "RitsuFileDialog",
            };
            var layer = new CanvasLayer { Name = "RitsuFileDialogLayer", Layer = RitsuUiLayer.Dialog };
            dialog._layer = layer;
            owner.AddChild(layer);
            layer.AddChild(dialog);
            return dialog;
        }

        /// <summary>
        ///     <para xml:lang="en">Cancels the picker and restores focus. Repeated calls before deletion have no effect.</para>
        ///     <para xml:lang="zh-CN">取消选择器并恢复焦点；删除前重复调用无额外效果。</para>
        /// </summary>
        public void Cancel()
        {
            Finish(null);
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            _attached = true;
            OpenDialogs.Add(this);
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            BuildLayout();
            RitsuShellThemeRuntime.ThemeChanged += ApplyTheme;
            GetViewport().SizeChanged += FitViewport;
            _thumbnails.Evicting += ResetThumbnail;
            ApplyTheme();
            FitViewport();
            ActiveScreenContext.Instance.Update();
            Navigate(ResolveInitialDirectory());
            Callable.From(() =>
            {
                if (!_closing && IsInsideTree())
                {
                    _files.GrabFocus();
                    RebuildFocusGraph();
                }
            }).CallDeferred();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            if (!_closing && _attached)
                Finish(null);
            RitsuShellThemeRuntime.ThemeChanged -= ApplyTheme;
            if (GetViewport() is { } viewport)
                viewport.SizeChanged -= FitViewport;
            _listingCancellation?.Cancel();
            var cancellation = _listingCancellation;
            if (_listing != null)
                _ = _listing.ContinueWith(_ => cancellation?.Dispose(), TaskScheduler.Default);
            else
                cancellation?.Dispose();
            if (IsInstanceValid(_files))
                _files.Clear();
            _thumbnails.Dispose();
            _fileIcon?.Dispose();
            _folderIcon?.Dispose();
            _refreshIcon?.Dispose();
            _favoriteIcon?.Dispose();
            OpenDialogs.Remove(this);
            base._ExitTree();
        }

        private void Finish(IReadOnlyList<string>? paths)
        {
            if (_closing)
                return;
            var wasActive = ActiveDialog == this;
            _closing = true;
            Hide();
            _listingCancellation?.Cancel();
            OpenDialogs.Remove(this);
            CloseKeyboard();
            ActiveScreenContext.Instance.Update();
            if (wasActive)
            {
                Input.MouseMode = ActiveDialog == null ? _previousMouseMode : Input.MouseModeEnum.Visible;
                var target = _previousFocus;
                Callable.From(() =>
                {
                    if (ActiveDialog is { } active)
                        ((IScreenContext)active).DefaultFocusedControl?.GrabFocus();
                    else if (target != null && IsInstanceValid(target) && target.IsInsideTree() &&
                             target.IsVisibleInTree())
                        target.GrabFocus();
                }).CallDeferred();
            }

            var selected = _selected;
            var canceled = _canceled;
            _selected = null;
            _canceled = null;
            if (IsInstanceValid(_layer) && !_layer.IsQueuedForDeletion())
                _layer.QueueFree();
            try
            {
                if (paths == null)
                    canceled?.Invoke();
                else
                    selected?.Invoke(Array.AsReadOnly<string>([.. paths]));
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                RitsuLibFramework.Logger.Warn($"[FileDialog] Completion callback failed: {exception}");
            }
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            if (_closing)
                return;
            CompleteListing();
            _thumbnails.Drain();
            _thumbnailRefresh -= delta;
            if (_thumbnailRefresh <= 0)
            {
                _thumbnailRefresh = 0.12;
                RefreshThumbnails();
            }

            if (ActiveDialog != this)
                return;
            var focus = GetViewport().GuiGetFocusOwner();
            if (focus != null && IsAncestorOf(focus))
                _lastFocus = focus;
            else
                ((IScreenContext)this).DefaultFocusedControl?.GrabFocus();
        }

        private void FitViewport()
        {
            Size = GetViewportRect().Size;
            Callable.From(RebuildFocusGraph).CallDeferred();
        }

        private static string Text(string key) => RitsuFileDialogText.Get(key);
    }
}
