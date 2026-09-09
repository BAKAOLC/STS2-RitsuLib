using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Files
{
    public sealed partial class RitsuFileDialog
    {
        private VBoxContainer CreatePrompt(string title, bool contextMenu = false)
        {
            CloseTransientPopups();
            ClosePrompt();
            _beforePromptFocus = GetViewport().GuiGetFocusOwner();
            _prompt = new() { MouseFilter = MouseFilterEnum.Stop, ZIndex = 1000 };
            _prompt.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(_prompt);
            var shade = new ColorRect
            {
                Color = contextMenu ? Colors.Transparent : RitsuShellTheme.Current.Color.ModalBackdrop,
                MouseFilter = MouseFilterEnum.Stop,
            };
            shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _prompt.AddChild(shade);
            if (contextMenu)
                shade.GuiInput += input =>
                {
                    if (input is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
                        ClosePrompt();
                };
            var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _prompt.AddChild(center);
            var panel = new PanelContainer
            {
                CustomMinimumSize = new(contextMenu ? 280 : 420, 0),
                MouseFilter = MouseFilterEnum.Stop,
            };
            _panels.Add(panel);
            center.AddChild(panel);
            if (contextMenu)
            {
                center.RemoveChild(panel);
                center.QueueFree();
                _prompt.AddChild(panel);
                panel.Position = _inputMode switch
                {
                    PickerInputMode.Touch => _touchPosition,
                    PickerInputMode.Controller => _beforePromptFocus?.GetGlobalRect().GetCenter() ??
                                                  _window.GetGlobalRect().GetCenter(),
                    _ => GetGlobalMousePosition(),
                };
                Callable.From(() =>
                {
                    if (IsInstanceValid(panel) && panel.IsInsideTree())
                        panel.Position = panel.Position.Clamp(Vector2.Zero, (Size - panel.Size).Max(Vector2.Zero));
                }).CallDeferred();
            }

            var margin = new MarginContainer();
            foreach (var side in new[] { "left", "top", "right", "bottom" })
                margin.AddThemeConstantOverride("margin_" + side, 20);
            panel.AddChild(margin);
            var body = new VBoxContainer();
            body.AddThemeConstantOverride("separation", 12);
            margin.AddChild(body);
            var heading = Label("");
            heading.Text = title;
            heading.CustomMinimumSize = new(contextMenu ? 240 : 380, 0);
            heading.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            body.AddChild(heading);
            return body;
        }

        private void ShowPrompt(string title, string message, string confirmLabel, Action confirm)
        {
            var body = CreatePrompt(title);
            var label = Label("");
            label.Text = message;
            label.CustomMinimumSize = new(380, 0);
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            body.AddChild(label);
            var row = Row(body);
            _promptFocus = Button(row, Text("cancel"), ClosePrompt, 110);
            Button(row, confirmLabel, () =>
            {
                ClosePrompt();
                confirm();
            }, 110);
            FocusPrompt();
        }

        private void CreateDirectoryPrompt()
        {
            if (_loading || _directory.Length == 0)
                return;
            var body = CreatePrompt(Text("newFolder"));
            var name = Edit("", "folderName", 380);
            body.AddChild(name);
            var error = Label("");
            error.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            body.AddChild(error);
            var row = Row(body);
            Button(row, Text("cancel"), ClosePrompt, 110);
            Button(row, Text("create"), Create, 110);
            name.TextSubmitted += _ => Create();
            _promptFocus = name;
            FocusPrompt();
            return;

            void Create()
            {
                try
                {
                    var value = name.Text.Trim();
                    if (!IsValidLeafName(value))
                    {
                        error.Text = Text("invalidName");
                        return;
                    }

                    var path = Path.Combine(_directory, value);
                    if (Directory.Exists(path) || File.Exists(path))
                    {
                        error.Text = Text("alreadyExists");
                        return;
                    }

                    Directory.CreateDirectory(path);
                    ClosePrompt();
                    Navigate(path);
                }
                catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
                {
                    error.Text = exception.Message;
                }
            }
        }

        private void ShowFileActions(int index)
        {
            if (_loading || _directory.Length == 0)
                return;
            var entry = (uint)index < _visibleEntries.Length ? _visibleEntries[index] : null;
            var path = entry?.Path ?? _directory;
            var body = CreatePrompt(entry?.Name ?? Text("contents"), true);
            _promptFocus = Button(body, Text("copyPath"), () =>
            {
                DisplayServer.ClipboardSet(path);
                ClosePrompt();
            }, 240);
            Button(body, Text("showInManager"), () =>
            {
                ClosePrompt();
                var error = OS.ShellShowInFileManager(path);
                if (error != Error.Ok)
                    ShowError(error.ToString());
            }, 240);
            Button(body, Text("refresh"), () =>
            {
                ClosePrompt();
                Navigate(_directory, _historyPosition);
            }, 240);
            if (_options.Mode is not (FileDialog.FileModeEnum.OpenFile or FileDialog.FileModeEnum.OpenFiles))
                Button(body, Text("newFolder"), CreateDirectoryPrompt, 240);
            Button(body, Text("cancel"), ClosePrompt, 240);
            FocusPrompt();
        }

        private void ShowFavoriteActions(int index)
        {
            var preferences = RitsuFileDialogPreferences.Current;
            if ((uint)index >= preferences.Favorites.Count)
                return;
            var path = preferences.Favorites[index];
            var body = CreatePrompt(Text("favorites"), true);
            _promptFocus = Button(body, Text("removeFavorite"), () =>
            {
                preferences.Favorites.Remove(path);
                preferences.Save();
                ClosePrompt();
                RefreshSidebar();
            }, 240);
            Button(body, Text("cancel"), ClosePrompt, 240);
            FocusPrompt();
        }

        private void FocusPrompt()
        {
            ApplyTheme();
            _promptFocus?.GrabFocus();
            Callable.From(RebuildFocusGraph).CallDeferred();
        }

        private void ClosePrompt()
        {
            if (_prompt == null)
                return;
            CloseKeyboard();
            _buttons.RemoveAll(button => _prompt.IsAncestorOf(button));
            _labels.RemoveAll(label => _prompt.IsAncestorOf(label));
            _edits.RemoveAll(edit => _prompt.IsAncestorOf(edit));
            _panels.RemoveAll(panel => _prompt.IsAncestorOf(panel));
            _prompt.Hide();
            _prompt.QueueFree();
            _prompt = null;
            _promptFocus = null;
            _lastFocus = _beforePromptFocus;
            if (_beforePromptFocus != null && IsInstanceValid(_beforePromptFocus) &&
                _beforePromptFocus.IsInsideTree() && _beforePromptFocus.IsVisibleInTree())
                _beforePromptFocus.GrabFocus();
            _beforePromptFocus = null;
            Callable.From(RebuildFocusGraph).CallDeferred();
        }

        private static bool IsValidLeafName(string value) =>
            !string.IsNullOrWhiteSpace(value) && value is not ("." or "..") &&
            value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && value.IndexOfAny(['/', '\\']) < 0 &&
            (!OperatingSystem.IsWindows() || !value.EndsWith('.'));
    }
}
