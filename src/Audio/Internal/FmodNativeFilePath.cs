using System.Text;
using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    internal static class FmodNativeFilePath
    {
        internal static bool TryMakeOpenable(string path, out string openablePath)
        {
            if (Ascii.IsValid(path))
            {
                openablePath = path;
                return true;
            }

            var localized = ProjectSettings.LocalizePath(path);
            if (Ascii.IsValid(localized) &&
                (localized.StartsWith("user://", StringComparison.Ordinal) ||
                 localized.StartsWith("res://", StringComparison.Ordinal)))
            {
                openablePath = localized;
                return true;
            }

            return path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
                ? FmodPackedAudioResourceCache.TryMaterialize(path, out openablePath)
                : FmodPackedAudioResourceCache.TryMaterializeFile(path, out openablePath);
        }
    }
}
