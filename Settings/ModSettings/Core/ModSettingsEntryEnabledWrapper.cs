using Godot;

namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsEntryEnabledWrapper(
        ModSettingsEntryDefinition inner,
        Func<bool> enabledPredicate)
        : ModSettingsEntryDefinition(inner.Id, inner.Label, inner.Description)
    {
        public override Func<bool>? VisibilityPredicate => inner.VisibilityPredicate;
        public override Func<bool>? EnabledPredicate => EvaluateEnabled;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return inner.CreateControl(context);
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

        private bool EvaluateEnabled()
        {
            return Evaluate(inner.EnabledPredicate) && Evaluate(enabledPredicate);
        }

        private static bool Evaluate(Func<bool>? predicate)
        {
            if (predicate == null)
                return true;

            try
            {
                return predicate();
            }
            catch
            {
                return true;
            }
        }
    }
}
