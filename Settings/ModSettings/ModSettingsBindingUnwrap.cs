namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsBindingUnwrap
    {
        internal static IModSettingsValueBinding<TValue> Unwrap<TValue>(IModSettingsValueBinding<TValue> binding)
        {
            while (true)
                switch (binding)
                {
                    case ModSettingsPolicyGatedValueBinding<TValue> p:
                        binding = p.Inner;
                        continue;
                    case ModSettingsRunScopedValueBinding<TValue> r:
                        binding = r.Inner;
                        continue;
                    default:
                        return binding;
                }
        }
    }
}
