using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugModelValueOverrides
    {
        internal const int MaximumDynamicVariableCount = 128;
        internal const int MaximumValue = 999_999_999;
        internal const int MinimumValue = -999_999_999;

        internal static bool IsEditable(DynamicVar dynamicVar)
        {
            ArgumentNullException.ThrowIfNull(dynamicVar);
            return dynamicVar is not StringVar &&
                   dynamicVar.BaseValue == decimal.Truncate(dynamicVar.BaseValue) &&
                   dynamicVar.BaseValue is >= MinimumValue and <= MaximumValue;
        }

        internal static RitsuDebugActionCheck Validate(
            DynamicVarSet dynamicVars,
            IReadOnlyDictionary<string, int>? overrides)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            if (overrides == null)
                return RitsuDebugActionCheck.Ok;
            if (overrides.Count > MaximumDynamicVariableCount)
                return RitsuDebugActionCheck.Fail(
                    "model.dynamicVarLimit",
                    "Model values can change at most {0} dynamic variables.",
                    MaximumDynamicVariableCount);

            foreach (var (key, value) in overrides)
            {
                if (string.IsNullOrWhiteSpace(key) || key.Length > 64)
                    return RitsuDebugActionCheck.Fail(
                        "model.dynamicVarKeyRequired",
                        "A valid dynamic-variable key is required.");
                if (value is < MinimumValue or > MaximumValue)
                    return RitsuDebugActionCheck.Fail(
                        "model.dynamicVarRange",
                        "Model values must be between {0} and {1}.",
                        MinimumValue,
                        MaximumValue);
                if (!dynamicVars.TryGetValue(key, out var dynamicVar))
                    return RitsuDebugActionCheck.Fail(
                        "model.dynamicVarMissing",
                        "The selected model has no dynamic variable named '{0}'.",
                        key);
                if (!IsEditable(dynamicVar))
                    return RitsuDebugActionCheck.Fail(
                        "model.dynamicVarUnsupported",
                        "Dynamic variable '{0}' is not an editable integer value.",
                        key);
            }

            return RitsuDebugActionCheck.Ok;
        }

        internal static void Apply(
            DynamicVarSet dynamicVars,
            IReadOnlyDictionary<string, int>? overrides)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            if (overrides == null)
                return;
            foreach (var (key, value) in overrides)
            {
                var dynamicVar = dynamicVars[key];
                dynamicVar.BaseValue = value;
                dynamicVar.ResetToBase();
            }
        }
    }
}
