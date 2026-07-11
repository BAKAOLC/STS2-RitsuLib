namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsEntryEnabledWrapper(
        ModSettingsEntryDefinition inner,
        Func<bool> enabledPredicate)
        : ModSettingsEntryDecorator(inner)
    {
        public override Func<bool> EnabledPredicate => EvaluateEnabled;

        private bool EvaluateEnabled()
        {
            return ModSettingsPredicate.Evaluate(Inner.EnabledPredicate) &&
                   ModSettingsPredicate.Evaluate(enabledPredicate);
        }
    }
}
