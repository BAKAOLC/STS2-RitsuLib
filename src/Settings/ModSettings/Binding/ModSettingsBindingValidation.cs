namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsBindingValidation
    {
        internal static T RequireNonNull<T>(T? value, string paramName) where T : class
        {
            ArgumentNullException.ThrowIfNull(value, paramName);
            return value;
        }

        internal static string RequireNonEmpty(string value, string paramName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
            return value;
        }
    }
}
