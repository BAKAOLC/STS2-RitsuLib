using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     Keeps file paths that the FMOD add-on cannot open away from its loose-file loaders.
    /// </summary>
    /// <remarks>
    ///     The game's FMOD add-on installs FMOD file-system callbacks that decode the path FMOD hands back as
    ///     Latin-1 and call <c>get_error()</c> on the result of <c>FileAccess.open</c> without a null check. A path
    ///     with any non-ASCII character therefore fails to open and dereferences null in native code: an access
    ///     violation on Windows and a hung main thread on macOS, with nothing in the managed log. Windows profiles
    ///     with non-ASCII user names hit this through every absolute path under the profile, so those paths are
    ///     replaced by their 8.3 short form when the volume provides one.
    /// </remarks>
    internal static partial class FmodNativeFilePath
    {
        internal static bool TryMakeOpenable(string path, out string openablePath)
        {
            if (Ascii.IsValid(path))
            {
                openablePath = path;
                return true;
            }

            if (OperatingSystem.IsWindows() && Path.IsPathFullyQualified(path) &&
                TryGetShortPath(path, out var shortPath) && Ascii.IsValid(shortPath))
            {
                openablePath = shortPath;
                return true;
            }

            openablePath = string.Empty;
            return false;
        }

        [SupportedOSPlatform("windows")]
        private static unsafe bool TryGetShortPath(string path, out string shortPath)
        {
            shortPath = string.Empty;
            var required = GetShortPathName(path, null, 0);
            if (required == 0)
                return false;

            var buffer = new char[required];
            fixed (char* chars = buffer)
            {
                var written = GetShortPathName(path, chars, required);
                if (written == 0 || written >= required)
                    return false;

                shortPath = new(chars, 0, (int)written);
                return true;
            }
        }

        [LibraryImport("kernel32.dll", EntryPoint = "GetShortPathNameW", StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial uint GetShortPathName(string longPath, char* shortPath, uint bufferLength);
    }
}
