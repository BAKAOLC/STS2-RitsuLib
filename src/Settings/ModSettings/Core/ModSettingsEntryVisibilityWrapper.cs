namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsEntryVisibilityWrapper(
        ModSettingsEntryDefinition inner,
        Func<bool> visibilityPredicate)
        : ModSettingsEntryDecorator(inner)
    {
        public override Func<bool> VisibilityPredicate => EvaluateVisibility;

        private bool EvaluateVisibility()
        {
            return ModSettingsPredicate.Evaluate(Inner.VisibilityPredicate) &&
                   ModSettingsPredicate.Evaluate(visibilityPredicate);
        }
    }
}
