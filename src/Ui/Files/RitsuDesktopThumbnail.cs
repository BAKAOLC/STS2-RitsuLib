using System.Diagnostics;
using Godot;

namespace STS2RitsuLib.Ui.Files
{
    internal static class RitsuDesktopThumbnail
    {
        private static bool _unavailable;

        internal static async Task<Image?> LoadAsync(string path, CancellationToken cancellation)
        {
            if (_unavailable)
                return null;
            try
            {
                if (OperatingSystem.IsWindows())
                    return RitsuWindowsThumbnail.Load(path);
                if (OperatingSystem.IsMacOS())
                    return await LoadQuickLookAsync(path, cancellation).ConfigureAwait(false);
                if (OperatingSystem.IsLinux())
                    return await RitsuLinuxThumbnail.LoadAsync(path, cancellation).ConfigureAwait(false);
                return null;
            }
            catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
            {
                _unavailable = true;
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                RitsuLibFramework.Logger.Warn($"[FileDialog] System thumbnail unavailable: {exception.Message}");
                return null;
            }
        }

        private static async Task<Image?> LoadQuickLookAsync(string path, CancellationToken cancellation)
        {
            const string executable = "/usr/bin/qlmanage";
            if (!File.Exists(executable))
                return null;
            var directory = Directory.CreateTempSubdirectory("ritsulib-thumbnail-");
            try
            {
                using var process = new Process();
                process.StartInfo = new(executable)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                };
                foreach (var argument in new[] { "-t", "-s", "160", "-o", directory.FullName, path })
                    process.StartInfo.ArgumentList.Add(argument);
                if (!process.Start())
                    return null;
                var output = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null, cancellation);
                var error = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellation);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                try
                {
                    await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                    await Task.WhenAll(output, error).ConfigureAwait(false);
                    var thumbnail = Path.Combine(directory.FullName, Path.GetFileName(path) + ".png");
                    return process.ExitCode == 0 && File.Exists(thumbnail) ? LoadImage(thumbnail) : null;
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
                {
                    return null;
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                directory.Delete(true);
            }
        }

        internal static Image? LoadImage(string path)
        {
            var image = new Image();
            if (image.Load(path) == Error.Ok && !image.IsEmpty())
                return image;
            image.Dispose();
            return null;
        }
    }
}
