namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorVisibilityPolicy
    {
        public static Func<bool>? BuildSectionVisibility(IReadOnlyList<ModSettingsMirrorEntryDefinition> entries)
        {
            if (entries.Count == 0)
                return null;

            return () => entries.Any(static entry => Evaluate(entry.VisibleWhen));
        }

        public static bool Evaluate(Func<bool>? predicate)
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
