namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorIds
    {
        public static string Slug(string name)
        {
            return ModSettingsMirrorSlugPolicy.Normalize(name);
        }

        public static string Entry(string prefix, string name)
        {
            return $"{prefix}_{Slug(name)}";
        }

        public static string Button(string prefix, string name)
        {
            return $"{prefix}_btn_{Slug(name)}";
        }

        public static string Section(string title, int index)
        {
            return $"sec_{Slug(title)}_{index}";
        }
    }
}
