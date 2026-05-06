namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsUiControlFactoryHelper
    {
        public static string ResolveDescription(ModSettingsText? description)
        {
            return ModSettingsUiContext.ResolveBindingDescriptionBody(description);
        }
    }
}
