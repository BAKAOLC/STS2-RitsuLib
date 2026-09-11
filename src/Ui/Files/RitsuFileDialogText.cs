using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Ui.Files
{
    internal static class RitsuFileDialogText
    {
        private static readonly Lazy<I18N> Localization =
            new(() => I18N.CreateForAssets("RitsuLib-FileDialog", "localization/file-dialog"));

        internal static string Get(string key)
        {
            return Localization.Value.Get(key, key);
        }
    }
}
