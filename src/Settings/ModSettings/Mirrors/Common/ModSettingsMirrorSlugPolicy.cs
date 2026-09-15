namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorSlugPolicy
    {
        public static string Normalize(string value)
        {
            return Sts2ModManagerCompat.NormalizePublicStem(value);
        }

        public static string PrefixForModId(string modId)
        {
            return Normalize(modId) + "_";
        }
    }
}
