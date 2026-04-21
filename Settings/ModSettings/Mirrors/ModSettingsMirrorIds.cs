using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorIds
    {
        public static string Entry(string prefix, string name)
        {
            return $"{prefix}_{StringHelper.Slugify(name)}";
        }

        public static string Button(string prefix, string name)
        {
            return $"{prefix}_btn_{StringHelper.Slugify(name)}";
        }

        public static string Section(string title, int index)
        {
            return $"sec_{StringHelper.Slugify(title)}_{index}";
        }
    }
}
