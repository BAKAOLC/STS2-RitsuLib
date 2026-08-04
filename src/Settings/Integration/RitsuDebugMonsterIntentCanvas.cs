using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using Timer = Godot.Timer;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugMonsterIntentCanvas : Control
    {
        private const float MoveNodeHeight = 98f;
        private const float MoveNodeWidth = 104f;
        private const float ExternalGraphPadding = 18f;
        private const float FlatListGap = 8f;
        private const int FlatListMaximumColumns = 4;
        private static readonly Color CurrentGlow = new(0.72f, 0.92f, 1f);
        private static readonly Color GroupBorder = new(0.34f, 0.67f, 0.79f, 0.9f);
        private static readonly Color LabelColor = new(0.96f, 0.94f, 0.87f);
        private readonly bool _graphMode;
        private readonly Control _hoverAnchor;
        private readonly Timer _hoverTimer;
        private readonly Dictionary<string, Rect2> _nodeRects = new(StringComparer.Ordinal);
        private readonly List<RitsuDebugIntentGraphNode> _nodes = [];
        private string? _currentMoveId;
        private Font? _displayFont;
        private RitsuDebugIntentGraphView? _externalGraphView;
        private string? _hoveredNodeId;
        private Font? _intentValueFont;
        private bool _layoutQueued;
        private RitsuDebugIntentGraphNode? _pendingHoverNode;
        private string? _unavailableText;

        internal RitsuDebugMonsterIntentCanvas(bool graphMode)
        {
            _graphMode = graphMode;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
            MouseFilter = MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.None;
            ClipContents = false;
            MouseExited += ClearHover;
            Resized += QueueLayout;
            _hoverAnchor = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
                FocusMode = FocusModeEnum.None,
            };
            AddChild(_hoverAnchor);
            _hoverTimer = new()
            {
                OneShot = true,
                WaitTime = RitsuShellTooltipTiming.StandardDelaySeconds,
            };
            _hoverTimer.Timeout += ShowPendingHoverTip;
            AddChild(_hoverTimer);
        }

        internal string CurrentMoveTitle { get; private set; } = string.Empty;

        internal event Action<Vector2>? ContentMinimumSizeChanged;

        internal event Action<string>? MoveRequested;

        public override void _Ready()
        {
            _displayFont = ResolveDisplayFont();
            _intentValueFont = ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres");
            RebuildLayout();
        }

        public override void _ExitTree()
        {
            HideHoverTip();
            RemoveExternalGraph();
            base._ExitTree();
        }

        internal void Refresh(Creature? creature)
        {
            HideHoverTip();
            RemoveExternalGraph();
            _nodes.Clear();
            _currentMoveId = null;
            CurrentMoveTitle = string.Empty;
            _hoveredNodeId = null;
            _unavailableText = null;
            if (creature?.Monster?.MoveStateMachine == null)
            {
                _unavailableText = L("ritsulib.debugTools.intentTargetUnavailable",
                    "The selected monster is no longer available.");
                QueueLayout();
                return;
            }

            _currentMoveId = creature.Monster.NextMove.Id;
            var titles = BuildMoveTitles(creature);
            var machine = creature.Monster.MoveStateMachine;
            foreach (var move in machine.States.Values
                         .OfType<MoveState>()
                         .Where(static move => move.Id is not ("UNSET_MOVE" or "STUNNED"))
                         .OrderBy(static move => move.Id, StringComparer.Ordinal))
                _nodes.Add(CreateMoveNode(creature, move, ResolveMoveTitle(creature, move, titles)));

            CurrentMoveTitle = _nodes.FirstOrDefault(node => node.Id == _currentMoveId)?.Title ??
                               (_currentMoveId == "STUNNED"
                                   ? L("ritsulib.debugTools.action.stunMonster", "Stunned")
                                   : _currentMoveId ?? string.Empty);

            switch (_graphMode)
            {
                case true when RitsuDebugIntentGraphInterop.TryCreate(creature, out var graphView):
                    _externalGraphView = graphView;
                    AddChild(graphView!.Control);
                    break;
                case false:
                {
                    var current = _nodes.FirstOrDefault(node => node.Id == _currentMoveId);
                    _nodes.Clear();
                    if (current != null)
                    {
                        _nodes.Add(current);
                    }
                    else if (creature.Monster.NextMove is { Id: not "UNSET_MOVE" } currentMove)
                    {
                        var title = currentMove.Id == "STUNNED"
                            ? L("ritsulib.debugTools.action.stunMonster", "Stunned")
                            : ResolveMoveTitle(creature, currentMove, titles);
                        _nodes.Add(CreateMoveNode(creature, currentMove, title));
                    }
                    else
                    {
                        _unavailableText = L("ritsulib.debugTools.unavailable", "Unavailable");
                    }

                    break;
                }
            }

            QueueLayout();
        }

        public override void _Draw()
        {
            if (_externalGraphView != null)
                return;

            if (_unavailableText != null)
            {
                DrawCenteredText(
                    RitsuShellTheme.Current.Font.Body,
                    _unavailableText,
                    new(Vector2.Zero, Size),
                    RitsuShellTheme.Current.Metric.FontSize.Secondary,
                    RitsuShellTheme.Current.Text.Hint,
                    Size.Y * 0.5f + 6f);
                return;
            }

            foreach (var node in _nodes)
                if (_nodeRects.TryGetValue(node.Id, out var rect))
                    DrawNode(node, rect);
        }

        public override void _GuiInput(InputEvent @event)
        {
            switch (@event)
            {
                case InputEventMouseMotion motion:
                    SetHover(FindNodeAt(motion.Position));
                    break;
                case InputEventMouseButton
                {
                    Pressed: true,
                    ButtonIndex: MouseButton.Left,
                } button when _graphMode:
                    var node = FindNodeAt(button.Position);
                    if (node != null)
                    {
                        MoveRequested?.Invoke(node.Id);
                        AcceptEvent();
                    }

                    break;
            }
        }

        private void QueueLayout()
        {
            if (_layoutQueued || !IsInsideTree())
                return;
            _layoutQueued = true;
            Callable.From(() =>
            {
                _layoutQueued = false;
                if (IsInsideTree())
                    RebuildLayout();
            }).CallDeferred();
        }

        private void RebuildLayout()
        {
            _nodeRects.Clear();
            if (_externalGraphView != null)
            {
                SetContentMinimumSize(_externalGraphView.MinimumSize +
                                      Vector2.One * (ExternalGraphPadding * 2f));
                var layoutSize = Size.Max(CustomMinimumSize);
                var graphOffset = (layoutSize - _externalGraphView.MinimumSize) * 0.5f;
                foreach (var node in _nodes)
                    if (_externalGraphView.MoveRects.TryGetValue(node.Id, out var rect))
                        _nodeRects[node.Id] = new(rect.Position + graphOffset, rect.Size);
                _externalGraphView.Control.Position = graphOffset;
                QueueRedraw();
                return;
            }

            if (_nodes.Count == 0)
            {
                SetContentMinimumSize(new(0f, _graphMode ? 180f : MoveNodeHeight));
                QueueRedraw();
                return;
            }

            if (_graphMode)
            {
                var columns = Math.Min(FlatListMaximumColumns, _nodes.Count);
                var rows = Mathf.CeilToInt(_nodes.Count / (float)columns);
                var cellWidth = _nodes.Max(GetNodeWidth);
                for (var index = 0; index < _nodes.Count; index++)
                {
                    var row = index / columns;
                    var column = index % columns;
                    var nodeWidth = GetNodeWidth(_nodes[index]);
                    _nodeRects[_nodes[index].Id] = new(
                        column * (cellWidth + FlatListGap) + (cellWidth - nodeWidth) * 0.5f,
                        row * (MoveNodeHeight + FlatListGap),
                        nodeWidth,
                        MoveNodeHeight);
                }

                SetContentMinimumSize(new(
                    columns * cellWidth + Math.Max(0, columns - 1) * FlatListGap,
                    rows * MoveNodeHeight + Math.Max(0, rows - 1) * FlatListGap));
                QueueRedraw();
                return;
            }

            var previewNodeWidth = GetNodeWidth(_nodes[0]);
            _nodeRects[_nodes[0].Id] = new(0f, 0f, previewNodeWidth, MoveNodeHeight);
            SetContentMinimumSize(new(previewNodeWidth, MoveNodeHeight));
            QueueRedraw();
        }

        private void SetContentMinimumSize(Vector2 size)
        {
            if (CustomMinimumSize == size)
                return;
            CustomMinimumSize = size;
            ContentMinimumSizeChanged?.Invoke(size);
        }

        private void DrawNode(RitsuDebugIntentGraphNode node, Rect2 rect)
        {
            var theme = RitsuShellTheme.Current;
            var font = _displayFont ?? theme.Font.BodyBold;
            var current = node.Id == _currentMoveId;
            var hovered = node.Id == _hoveredNodeId;
            var visibleIntents = node.Intents.Take(3).ToArray();
            const float iconSize = 48f;
            const float stride = 52f;
            var startX = rect.GetCenter().X - (visibleIntents.Length - 1) * stride * 0.5f;

            if (hovered)
                DrawRect(rect.Grow(-4f), GroupBorder with { A = 0.9f }, false, 1.5f, true);
            if (current)
            {
                DrawRect(rect.Grow(-2f), CurrentGlow with { A = 0.09f });
                DrawRect(rect.Grow(-3f), CurrentGlow with { A = 0.22f }, false, 4f, true);
                DrawRect(rect.Grow(-4f), CurrentGlow with { A = 0.72f }, false, 1.5f, true);
            }

            for (var index = 0; index < visibleIntents.Length; index++)
            {
                var intent = visibleIntents[index];
                var center = new Vector2(startX + index * stride, rect.Position.Y + 32f);
                if (intent.Icon != null)
                    DrawTextureRect(intent.Icon,
                        new(center.X - iconSize * 0.5f, center.Y - iconSize * 0.5f, iconSize, iconSize), false);
                if (!string.IsNullOrWhiteSpace(intent.Value))
                {
                    var valueFont = _intentValueFont ?? font;
                    var valuePosition = new Vector2(center.X - iconSize * 0.5f + 8f, center.Y + 23f);
                    DrawStringOutline(
                        valueFont,
                        valuePosition,
                        intent.Value,
                        fontSize: 16,
                        size: 10,
                        modulate: new(0f, 0f, 0f, 0.82f));
                    DrawString(valueFont, valuePosition, intent.Value, fontSize: 16, modulate: Colors.White);
                }
            }

            switch (visibleIntents.Length)
            {
                case 0:
                    DrawCenteredText(font, "—", rect, theme.Metric.FontSize.Secondary,
                        theme.Text.Hint, 38f);
                    break;
                case > 1:
                    var groupWidth = (visibleIntents.Length - 1) * stride + iconSize + 10f;
                    DrawRect(new(rect.GetCenter().X - groupWidth * 0.5f, rect.Position.Y + 2f, groupWidth, 65f),
                        GroupBorder, false, 1.5f, true);
                    break;
            }

            DrawCenteredText(
                font,
                node.Title,
                new(rect.Position.X + 4f, rect.End.Y - 27f, rect.Size.X - 8f, 22f),
                theme.Metric.FontSize.HintSmall,
                current || hovered ? Colors.White : LabelColor,
                16f);
        }

        private static float GetNodeWidth(RitsuDebugIntentGraphNode node)
        {
            var iconCount = Math.Clamp(node.Intents.Count, 1, 3);
            return Math.Max(MoveNodeWidth, 64f + (iconCount - 1) * 52f);
        }

        private RitsuDebugIntentGraphNode? FindNodeAt(Vector2 position)
        {
            foreach (var node in _nodes)
                if (_nodeRects.TryGetValue(node.Id, out var rect) && rect.HasPoint(position))
                    return node;

            return null;
        }

        private void SetHover(RitsuDebugIntentGraphNode? node)
        {
            var nodeId = node?.Id;
            if (_hoveredNodeId == nodeId)
                return;
            HideHoverTip();
            _hoveredNodeId = nodeId;
            if (node != null)
            {
                _pendingHoverNode = node;
                _hoverTimer.Start();
            }

            MouseDefaultCursorShape = _graphMode && node != null
                ? CursorShape.PointingHand
                : CursorShape.Arrow;
            QueueRedraw();
        }

        private void ShowNodeHoverTip(RitsuDebugIntentGraphNode node)
        {
            if (node.HoverTips.Count == 0 || !_nodeRects.TryGetValue(node.Id, out var rect))
                return;
            SetHoverAnchor(rect);
            NHoverTipSet.CreateAndShow(
                    _hoverAnchor,
                    node.HoverTips,
                    HoverTip.GetHoverTipAlignment(_hoverAnchor))
                ?.SetFollowOwner();
        }

        private void SetHoverAnchor(Rect2 rect)
        {
            _hoverAnchor.Position = rect.Position;
            _hoverAnchor.Size = rect.Size;
        }

        private void HideHoverTip()
        {
            _hoverTimer.Stop();
            _pendingHoverNode = null;
            if (IsInstanceValid(_hoverAnchor))
                NHoverTipSet.Remove(_hoverAnchor);
        }

        private void ShowPendingHoverTip()
        {
            if (_pendingHoverNode is not { } node || node.Id != _hoveredNodeId)
                return;

            ShowNodeHoverTip(node);
        }

        private void ClearHover()
        {
            SetHover(null);
        }

        private void RemoveExternalGraph()
        {
            if (_externalGraphView == null)
                return;
            var control = _externalGraphView.Control;
            _externalGraphView = null;
            if (!IsInstanceValid(control))
                return;
            if (control.GetParent() == this)
                RemoveChild(control);
            control.QueueFree();
        }

        private static RitsuDebugIntentGraphNode CreateMoveNode(Creature creature, MoveState move, string title)
        {
            var targets = creature.CombatState?.PlayerCreatures ?? [];
            var intents = new List<RitsuDebugIntentGraphVisual>(move.Intents.Count);
            var hoverTips = new List<IHoverTip>(move.Intents.Count);
            foreach (var intent in move.Intents)
            {
                Texture2D? icon = null;
                var value = string.Empty;
                try
                {
                    var tip = intent.GetHoverTip(targets, creature);
                    hoverTips.Add(tip);
                    icon = intent.GetTexture(targets, creature);
                    if (intent is AttackIntent or StatusIntent)
                        value = (intent.GetIntentLabel(targets, creature).GetFormattedText() ?? string.Empty)
                            .StripBbCode()
                            .Trim();
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not render intent '{intent.IntentType}' for '{creature.ModelId}': {ex.Message}");
                }

                intents.Add(new(icon, value));
            }

            return new(move.Id, title, intents, hoverTips);
        }

        private static Dictionary<string, string> BuildMoveTitles(Creature creature)
        {
            var titles = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                var visuals = NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals;
                foreach (var move in creature.Monster!.GenerateBestiaryMoveList(visuals))
                    if (!string.IsNullOrWhiteSpace(move.stateId) && !string.IsNullOrWhiteSpace(move.displayName) &&
                        !move.displayName.Equals(move.stateId, StringComparison.Ordinal))
                        titles.TryAdd(move.stateId, move.displayName.Trim());
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not build the intent titles for '{creature.ModelId}': {ex.Message}");
            }

            return titles;
        }

        private static string ResolveMoveTitle(
            Creature creature,
            MoveState move,
            IReadOnlyDictionary<string, string> titles)
        {
            if (titles.TryGetValue(move.Id, out var title))
                return title;
            try
            {
                var targets = creature.CombatState?.PlayerCreatures ?? [];
                var intentTitle = move.Intents.FirstOrDefault()?.GetHoverTip(targets, creature).Title;
                if (!string.IsNullOrWhiteSpace(intentTitle))
                    return intentTitle;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not resolve the intent title for '{creature.ModelId}': {ex.Message}");
            }

            return L("ritsulib.debugTools.unnamedMonsterIntent", "Unnamed intent");
        }

        private void DrawCenteredText(
            Font font,
            string text,
            Rect2 rect,
            int fontSize,
            Color color,
            float baselineOffset)
        {
            var trimmed = TrimToWidth(font, text, fontSize, rect.Size.X);
            DrawString(font, new(rect.Position.X, rect.Position.Y + baselineOffset), trimmed,
                HorizontalAlignment.Center, rect.Size.X, fontSize, color);
        }

        private static string TrimToWidth(Font font, string text, int fontSize, float width)
        {
            if (font.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize).X <= width)
                return text;
            const string ellipsis = "…";
            var low = 0;
            var high = text.Length;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                var candidate = text[..middle] + ellipsis;
                if (font.GetStringSize(candidate, HorizontalAlignment.Left, -1f, fontSize).X <= width)
                    low = middle;
                else
                    high = middle - 1;
            }

            return text[..low] + ellipsis;
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static Font ResolveDisplayFont()
        {
            var language = LocManager.Instance?.Language;
            if (!string.IsNullOrWhiteSpace(language))
            {
                var localized = FontManager.GetSubstituteFont(language, FontType.Bold);
                if (localized != null)
                    return localized;
            }

            return ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres") ??
                   RitsuShellTheme.Current.Font.BodyBold;
        }
    }

    internal sealed record RitsuDebugIntentGraphVisual(Texture2D? Icon, string Value);

    internal sealed record RitsuDebugIntentGraphNode(
        string Id,
        string Title,
        IReadOnlyList<RitsuDebugIntentGraphVisual> Intents,
        IReadOnlyList<IHoverTip> HoverTips);
}
