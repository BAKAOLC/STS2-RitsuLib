using Godot;

namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsEntryHoverTipWrapper(
        ModSettingsEntryDefinition inner,
        ModSettingsText title,
        ModSettingsText description)
        : ModSettingsEntryDefinition(inner.Id, inner.Label, inner.Description)
    {
        public override Func<bool>? VisibilityPredicate => inner.VisibilityPredicate;
        public override Func<bool>? EnabledPredicate => inner.EnabledPredicate;

        internal override bool CanResetToDefault => inner.CanResetToDefault;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            var control = inner.CreateControl(context);
            ModSettingsNativeHoverTip.Attach(
                control,
                () => ModSettingsUiContext.Resolve(title),
                () => ModSettingsUiContext.Resolve(description));
            return control;
        }

        internal override void CollectChromeBindingSnapshots(
            Dictionary<string, ModSettingsChromeBindingSnapshot> target)
        {
            inner.CollectChromeBindingSnapshots(target);
        }

        internal override bool TryPasteChromeBindingSnapshot(ModSettingsChromeBindingSnapshot snap,
            IModSettingsUiActionHost host)
        {
            return inner.TryPasteChromeBindingSnapshot(snap, host);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return inner.TryResetToDefault(host);
        }
    }
}
