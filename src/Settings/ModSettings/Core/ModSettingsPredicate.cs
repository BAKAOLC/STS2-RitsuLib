namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsPredicate
    {
        internal static bool Evaluate(Func<bool>? predicate)
        {
            if (predicate == null)
                return true;

            try
            {
                return predicate();
            }
            catch
            {
                // Preserve the established fail-open behavior for third-party predicates.
                return true;
            }
        }
    }
}
