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
            catch (Exception ex)
            {
                var method = predicate.Method;
                var predicateName = $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Predicate '{predicateName}' failed and was treated as false: {ex}");
                return false;
            }
        }
    }
}
