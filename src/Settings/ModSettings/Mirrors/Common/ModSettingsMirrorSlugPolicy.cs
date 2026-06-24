using STS2RitsuLib.Content;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorSlugPolicy
    {
        public static string Normalize(string value)
        {
            return ModContentRegistry.NormalizePublicStem(value);
        }

        public static string PrefixForModId(string modId)
        {
            return Normalize(modId) + "_";
        }
    }
}
