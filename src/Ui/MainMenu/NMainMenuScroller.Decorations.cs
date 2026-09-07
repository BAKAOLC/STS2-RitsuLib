using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace STS2RitsuLib.Ui.MainMenu
{
    internal sealed partial class NMainMenuScroller
    {
        private static readonly AccessTools.FieldRef<NContinueRunInfo, Vector2> RunInfoOrigin =
            AccessTools.FieldRefAccess<NContinueRunInfo, Vector2>("_initPosition");

        private static readonly AccessTools.FieldRef<NContinueRunInfo, Tween?> RunInfoTween =
            AccessTools.FieldRefAccess<NContinueRunInfo, Tween?>("_visTween");

        private static readonly AccessTools.FieldRef<NContinueRunInfo, bool> RunInfoShown =
            AccessTools.FieldRefAccess<NContinueRunInfo, bool>("_isShown");

        private Control? _reticleLeft;
        private Control? _reticleRight;
        private NMainMenuTextButton? _reticleButton;
        private Tween? _reticleTween;
        private float _reticleProgress;
        private NContinueRunInfo? _runInfo;
        private Control? _runInfoButton;
        private Vector2 _runInfoOffset;
        private Tween? _runInfoTween;
        private float _runInfoProgress;

        private void InitializeDecorations()
        {
            _reticleLeft = _mainMenu.GetNodeOrNull<Control>("%ButtonReticleLeft");
            _reticleRight = _mainMenu.GetNodeOrNull<Control>("%ButtonReticleRight");
            _runInfo = _mainMenu.GetNodeOrNull<NContinueRunInfo>("%ContinueRunInfo");
            if (_runInfo?.GetParent() is not Control button || button.GetParent() != this)
                return;
            _runInfoButton = button;
            _runInfoOffset = RunInfoOrigin(_runInfo);
            RunInfoTween(_runInfo)?.Kill();
            RunInfoTween(_runInfo) = null;
            _runInfoProgress = _runInfo.Modulate.A;
            _runInfo.TopLevel = true;
            _runInfo.MouseFilter = MouseFilterEnum.Ignore;
        }

        internal bool AnimateRunInfo(NContinueRunInfo info, bool show)
        {
            if (!Initialized || info != _runInfo)
                return false;
            RunInfoShown(info) = show;
            _runInfoTween?.Kill();
            _runInfoTween = CreateTween();
            _runInfoTween.TweenMethod(Callable.From<float>(value =>
            {
                _runInfoProgress = value;
                UpdateDecorations();
            }), _runInfoProgress, show ? 1f : 0f, 0.2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            return true;
        }

        private void ShowReticles(NMainMenuTextButton button)
        {
            _reticleButton = button;
            _reticleTween?.Kill();
            _reticleProgress = 0f;
            _reticleTween = CreateTween();
            _reticleTween.TweenMethod(Callable.From<float>(value =>
            {
                _reticleProgress = value;
                UpdateDecorations();
            }), 0f, 1f, 0.2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            UpdateDecorations();
        }

        private void HideReticles(NMainMenuTextButton button)
        {
            if (_reticleButton != button)
                return;
            _reticleTween?.Kill();
            _reticleButton = null;
            UpdateDecorations();
        }

        private void UpdateDecorations()
        {
            if (!Initialized)
                return;
            var showReticles = IsActive && IsNavigable(_reticleButton) &&
                               _reticleButton!.GetParent() == this && IsRowFullyVisible(_reticleButton);
            if (_reticleLeft != null && _reticleRight != null &&
                IsInstanceValid(_reticleLeft) && IsInstanceValid(_reticleRight))
            {
                _reticleLeft.Visible = showReticles;
                _reticleRight.Visible = showReticles;
                if (showReticles && _reticleButton!.label is { } label)
                {
                    var y = _reticleButton.GlobalPosition.Y + 5f;
                    var spread = 28f * _reticleProgress;
                    _reticleLeft.GlobalPosition = new(label.GlobalPosition.X - 26f - spread, y);
                    _reticleRight.GlobalPosition = new(label.GlobalPosition.X + label.Size.X - 14f + spread, y);
                    var color = StsColors.gold;
                    color.A = Mathf.Min(1f, _reticleProgress * 4f);
                    _reticleLeft.Modulate = color;
                    _reticleRight.Modulate = color;
                }
            }

            if (_runInfo == null || _runInfoButton == null || !IsInstanceValid(_runInfo) ||
                !IsInstanceValid(_runInfoButton))
                return;
            var showInfo = IsActive && _runInfoButton.IsVisibleInTree() &&
                           IsRowFullyVisible(_runInfoButton) && _runInfoProgress > 0f;
            _runInfo.Visible = showInfo;
            var menuRect = _mainMenu.GetGlobalRect();
            var position = _runInfoButton.GlobalPosition + _runInfoOffset + new Vector2(0f, -20f * _runInfoProgress);
            position.X = Mathf.Clamp(position.X, menuRect.Position.X + EdgePadding,
                Mathf.Max(menuRect.Position.X + EdgePadding, menuRect.End.X - _runInfo.Size.X - EdgePadding));
            position.Y = Mathf.Clamp(position.Y, GlobalPosition.Y,
                Mathf.Max(GlobalPosition.Y, menuRect.End.Y - _runInfo.Size.Y - EdgePadding));
            _runInfo.GlobalPosition = position;
            var infoColor = _runInfo.Modulate;
            infoColor.A = _runInfoProgress;
            _runInfo.Modulate = infoColor;
        }

        private void HideDecorations()
        {
            _reticleTween?.Kill();
            _reticleButton = null;
            _runInfoTween?.Kill();
            _runInfoProgress = 0f;
            if (_runInfo != null && IsInstanceValid(_runInfo))
                RunInfoShown(_runInfo) = false;
            UpdateDecorations();
        }

        private void DisposeDecorations()
        {
            _reticleTween?.Kill();
            _runInfoTween?.Kill();
        }
    }
}
