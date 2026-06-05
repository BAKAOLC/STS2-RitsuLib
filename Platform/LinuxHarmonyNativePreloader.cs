using System.Runtime.InteropServices;

namespace STS2RitsuLib.Platform
{
    internal static class LinuxHarmonyNativePreloader
    {
        // ReSharper disable IdentifierTypo, StringLiteralTypo
        private const int RtldNow = 2;
        private const int RtldGlobal = 0x100;
        private const int DlopenFlags = RtldNow | RtldGlobal;

        private static readonly Lock SyncRoot = new();
        private static readonly List<IntPtr> Handles = [];
        private static bool _attempted;

        private static readonly string[] CandidateLibraries =
        [
            "libgcc_s.so.1",
            "libunwind.so.8",
            "libunwind.so",
        ];

        public static void EnsureLoaded(Action<string> info, Action<string> warn)
        {
            ArgumentNullException.ThrowIfNull(info);
            ArgumentNullException.ThrowIfNull(warn);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return;

            lock (SyncRoot)
            {
                if (_attempted)
                    return;

                _attempted = true;
                TryLoadLinuxNativeLibraries(info, warn);
            }
        }

#pragma warning disable CA2101
#pragma warning disable SYSLIB1054
        [DllImport("libdl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr Dlopen(string filename, int flags);

        [DllImport("libdl.so.2", EntryPoint = "dlerror")]
        private static extern IntPtr Dlerror();
#pragma warning restore SYSLIB1054
#pragma warning restore CA2101

        private static void TryLoadLinuxNativeLibraries(Action<string> info, Action<string> warn)
        {
            var loaded = new List<string>();
            var failures = new List<string>();

            try
            {
                foreach (var library in CandidateLibraries)
                {
                    var handle = Dlopen(library, DlopenFlags);
                    if (handle != IntPtr.Zero)
                    {
                        Handles.Add(handle);
                        loaded.Add(library);
                        continue;
                    }

                    failures.Add($"{library}: {GetLastDlError()}");
                }
            }
            catch (DllNotFoundException ex)
            {
                warn($"Linux Harmony native preload skipped: libdl.so.2 is unavailable ({ex.Message}).");
                return;
            }
            catch (EntryPointNotFoundException ex)
            {
                warn($"Linux Harmony native preload skipped: libdl.so.2 is missing dlopen/dlerror ({ex.Message}).");
                return;
            }
            catch (Exception ex)
            {
                warn($"Linux Harmony native preload failed unexpectedly: {ex.Message}");
                return;
            }

            if (loaded.Count > 0)
            {
                info($"Linux Harmony native preload loaded with RTLD_GLOBAL: {string.Join(", ", loaded)}.");
                return;
            }

            warn(
                "Linux Harmony native preload could not load libgcc/libunwind candidates; Harmony may still work if the host already exports unwind symbols. "
                + $"Tried: {string.Join("; ", failures)}");
        }

        private static string GetLastDlError()
        {
            var error = Dlerror();
            return error == IntPtr.Zero
                ? "unknown dlopen error"
                : Marshal.PtrToStringAnsi(error) ?? "unknown dlopen error";
        }
        // ReSharper restore IdentifierTypo, StringLiteralTypo
    }
}
