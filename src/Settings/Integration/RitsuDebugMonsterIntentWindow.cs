using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugMonsterIntentWindow : VBoxContainer
    {
        private readonly uint _combatId;
        private readonly RitsuDebugMonsterIntentCanvas _graph;
        private readonly Label _identity;
        private readonly ulong _requesterNetId;
        private readonly ScrollContainer _scroll;
        private readonly Label _status;
        private readonly ulong _targetPlayerNetId;
        private bool _refreshQueued;
        private bool _targetAvailable;

        internal RitsuDebugMonsterIntentWindow(uint combatId, ulong requesterNetId, ulong targetPlayerNetId)
        {
            _combatId = combatId;
            _requesterNetId = requesterNetId;
            _targetPlayerNetId = targetPlayerNetId;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 4);

            _identity = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _identity.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            _identity.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Grip);
            _identity.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            AddChild(_identity);

            var toolbar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toolbar.AddThemeConstantOverride("separation", 4);
            toolbar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            toolbar.AddChild(CreateCompactActionButton(
                L("ritsulib.debugTools.action.performMonsterIntent", "Perform current intent"),
                ModSettingsButtonTone.Accent,
                () => Submit((requester, target) =>
                    RitsuDebugCombatActions.SubmitPerformMonsterIntent(requester, target, _combatId)),
                128f));
            toolbar.AddChild(CreateCompactActionButton(
                L("ritsulib.debugTools.action.stunMonster", "Stun"),
                ModSettingsButtonTone.Normal,
                () => Submit((requester, target) =>
                    RitsuDebugCombatActions.SubmitStunMonster(requester, target, _combatId)),
                64f));
            AddChild(toolbar);

            _scroll = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(_scroll);
            AddChild(_scroll);
            _graph = new(true);
            _graph.ContentMinimumSizeChanged += OnGraphContentMinimumSizeChanged;
            _graph.MoveRequested += moveId => Submit((requester, target) =>
                RitsuDebugCombatActions.SubmitSetMonsterIntent(requester, target, _combatId, moveId));
            _scroll.AddChild(_graph);

            _status = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _status.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            _status.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Grip);
            _status.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(_status);
        }

        public override void _Ready()
        {
            RitsuDebugActionProtocol.ActionExecuted += OnActionExecuted;
            CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
            CombatManager.Instance.CombatEnded += OnCombatEnded;
            RefreshTarget();
        }

        public override void _ExitTree()
        {
            RitsuDebugActionProtocol.ActionExecuted -= OnActionExecuted;
            CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
            CombatManager.Instance.CombatEnded -= OnCombatEnded;
            _graph.ContentMinimumSizeChanged -= OnGraphContentMinimumSizeChanged;
            base._ExitTree();
        }

        private void OnGraphContentMinimumSizeChanged(Vector2 size)
        {
            _scroll.CustomMinimumSize = new(
                Math.Min(size.X, 720f),
                Math.Min(size.Y, 440f));
        }

        private void Submit(Func<Player, Player, RitsuDebugActionSubmission> action)
        {
            if (!TryResolvePlayers(out var requester, out var target))
            {
                ShowFailure(L("ritsulib.debugTools.localPlayerMissing",
                    "The local or target player is unavailable."));
                return;
            }

            try
            {
                var result = action(requester, target);
                if (!result.Accepted)
                {
                    ShowFailure(result.Message);
                    return;
                }

                SetStatus(result.Message, false);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugToolsUi] Floating intent action failed: {ex}");
                ShowFailure(L("ritsulib.debugTools.requestFailed",
                    "The requested change could not be submitted. See the game log for details."));
            }
        }

        private bool TryResolvePlayers(out Player requester, out Player target)
        {
            var players = RunManager.Instance?.DebugOnlyGetState()?.Players;
            requester = players?.FirstOrDefault(player => player.NetId == _requesterNetId)!;
            target = players?.FirstOrDefault(player => player.NetId == _targetPlayerNetId)!;
            return requester != null && target != null;
        }

        private void OnActionExecuted(RitsuDebugActionExecutionResult result)
        {
            if (result.TargetPlayerNetId != _targetPlayerNetId ||
                !RitsuDebugCombatActions.IsMonsterIntentActionFor(result, _combatId))
                return;
            if (result.Success)
                SetStatus(L("ritsulib.debugTools.changeApplied", "The requested change was applied."), false);
            else
                ShowFailure(result.Message);
            QueueRefresh();
        }

        private void OnCombatStateChanged(CombatState _)
        {
            QueueRefresh();
        }

        private void OnCombatEnded(CombatRoom _)
        {
            QueueRefresh();
        }

        private void QueueRefresh()
        {
            if (_refreshQueued || !IsInsideTree())
                return;
            _refreshQueued = true;
            Callable.From(() =>
            {
                _refreshQueued = false;
                if (IsInsideTree())
                    RefreshTarget();
            }).CallDeferred();
        }

        private void RefreshTarget()
        {
            var creature = RitsuDebugCombatActions.FindCreature(_combatId);
            _graph.Refresh(creature);
            _identity.Text = creature == null
                ? L("ritsulib.debugTools.intentTargetUnavailable", "The selected monster is no longer available.")
                : string.Format(
                    L("ritsulib.debugTools.intentWindowIdentity",
                        "HP {0}/{1} · Block {2} · Current intent: {3}"),
                    creature.CurrentHp,
                    creature.MaxHp,
                    creature.Block,
                    _graph.CurrentMoveTitle);
            var available = creature?.Monster?.MoveStateMachine != null && !creature.IsDead;
            if (!available)
                SetStatus(L("ritsulib.debugTools.intentTargetUnavailable",
                    "The selected monster is no longer available."), true);
            else if (!_targetAvailable)
                SetStatus(string.Empty, false);
            _targetAvailable = available;
        }

        private void ShowFailure(string message)
        {
            SetStatus(message, true);
            RitsuToastService.ShowWarning(message, L("ritsulib.debugTools.toastTitle", "Developer tools"));
        }

        private void SetStatus(string message, bool failure)
        {
            _status.Text = message;
            _status.Visible = !string.IsNullOrWhiteSpace(message);
            _status.AddThemeColorOverride("font_color", failure
                ? RitsuShellTheme.Current.Component.TextButton.Danger.Fg
                : RitsuShellTheme.Current.Text.LabelSecondary);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static ModSettingsTextButton CreateCompactActionButton(
            string text,
            ModSettingsButtonTone tone,
            Action action,
            float width)
        {
            var button = new ModSettingsTextButton(text, tone, action)
            {
                CustomMinimumSize = new(width, 28f),
            };
            button.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HintSmall);
            return button;
        }
    }
}
