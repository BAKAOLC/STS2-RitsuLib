using Godot;

namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsEntryVisibilityWrapper(
        ModSettingsEntryDefinition inner,
        Func<bool> visibilityPredicate)
        : ModSettingsEntryDefinition(inner.Id, inner.Label, inner.Description)
    {
        public override Func<bool>? VisibilityPredicate => EvaluateVisibility;

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

        private bool EvaluateVisibility()
        {
            return Evaluate(inner.VisibilityPredicate) && Evaluate(visibilityPredicate);
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
