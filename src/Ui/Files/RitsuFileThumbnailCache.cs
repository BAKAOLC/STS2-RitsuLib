using System.Collections.Concurrent;
using Godot;

namespace STS2RitsuLib.Ui.Files
{
    internal sealed class RitsuFileThumbnailCache : IDisposable
    {
        private const int Capacity = 128;

        private static readonly HashSet<string> ImageExtensions =
            new([".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga", ".svg", ".dds", ".ktx", ".exr", ".hdr"],
                StringComparer.OrdinalIgnoreCase);

        private readonly Lock _sync = new();
        private readonly SemaphoreSlim _slots = new(2);
        private readonly CancellationTokenSource _lifetime = new();
        private readonly ConcurrentQueue<(string Key, Image? Image)> _ready = new();
        private readonly HashSet<string> _pending = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (ImageTexture? Texture, long Used)> _cache = new(StringComparer.Ordinal);
        private readonly List<Task> _tasks = [];
        private long _clock;
        private int _generation;
        private bool _disposed;

        internal event Action<string>? Evicting;

        internal static string Key(RitsuFileEntry entry)
        {
            return $"{entry.Path}\n{entry.Modified}\n{entry.Length}";
        }

        internal void ChangeDirectory()
        {
            Interlocked.Increment(ref _generation);
        }

        internal Texture2D? Get(RitsuFileEntry entry)
        {
            if (entry.IsDirectory)
                return null;
            var key = Key(entry);
            if (_cache.TryGetValue(key, out var cached))
            {
                _cache[key] = (cached.Texture, ++_clock);
                return cached.Texture;
            }

            lock (_sync)
            {
                if (_disposed || _pending.Count >= 32 || !_pending.Add(key))
                    return null;
                var generation = _generation;
                _tasks.Add(Task.Run(() => LoadAsync(entry, key, generation)));
            }

            return null;
        }

        internal void Drain()
        {
            lock (_sync)
            {
                foreach (var task in _tasks.Where(task => task.IsCompleted))
                    task.GetAwaiter().GetResult();
                _tasks.RemoveAll(task => task.IsCompleted);
            }

            while (_ready.TryDequeue(out var result))
            {
                lock (_sync)
                    _pending.Remove(result.Key);
                ImageTexture? texture = null;
                using (result.Image)
                    if (result.Image != null)
                        texture = ImageTexture.CreateFromImage(result.Image);
                if (_cache.Remove(result.Key, out var previous))
                    previous.Texture?.Dispose();
                _cache[result.Key] = (texture, ++_clock);
                while (_cache.Count > Capacity)
                {
                    var oldest = _cache.MinBy(pair => pair.Value.Used);
                    Evicting?.Invoke(oldest.Key);
                    _cache.Remove(oldest.Key);
                    oldest.Value.Texture?.Dispose();
                }
            }
        }

        private async Task LoadAsync(RitsuFileEntry entry, string key, int generation)
        {
            Image? image = null;
            var acquired = false;
            var queued = false;
            try
            {
                await _slots.WaitAsync(_lifetime.Token).ConfigureAwait(false);
                acquired = true;
                if (generation != Volatile.Read(ref _generation))
                    return;
                image = await RitsuDesktopThumbnail.LoadAsync(entry.Path, _lifetime.Token).ConfigureAwait(false);
                if (image == null && ImageExtensions.Contains(Path.GetExtension(entry.Name)))
                    image = RitsuDesktopThumbnail.LoadImage(entry.Path);
                if (image != null)
                {
                    if (image.IsCompressed() && image.Decompress() != Error.Ok)
                    {
                        image.Dispose();
                        image = null;
                    }

                    if (image != null)
                    {
                        var scale = Math.Min(1f, 160f / Math.Max(image.GetWidth(), image.GetHeight()));
                        image.Resize(Math.Max(1, (int)(image.GetWidth() * scale)),
                            Math.Max(1, (int)(image.GetHeight() * scale)));
                        image.Convert(Image.Format.Rgba8);
                    }
                }

                lock (_sync)
                {
                    if (_disposed || generation != _generation)
                        return;
                    _ready.Enqueue((key, image));
                    queued = true;
                    image = null;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[FileDialog] Could not load thumbnail '{entry.Path}': {exception.Message}");
                lock (_sync)
                    if (!_disposed && generation == _generation)
                    {
                        _ready.Enqueue((key, null));
                        queued = true;
                    }
            }
            finally
            {
                image?.Dispose();
                if (acquired)
                    _slots.Release();
                if (!queued)
                    lock (_sync)
                        _pending.Remove(key);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _lifetime.Cancel();
                while (_ready.TryDequeue(out var result))
                    result.Image?.Dispose();
                foreach (var cached in _cache.Values)
                    cached.Texture?.Dispose();
                _cache.Clear();
                _ = Task.WhenAll(_tasks).ContinueWith(_ =>
                {
                    _slots.Dispose();
                    _lifetime.Dispose();
                }, TaskScheduler.Default);
            }
        }
    }

    internal sealed record RitsuFileEntry(
        string Path,
        string Name,
        bool IsDirectory,
        bool Hidden,
        long Length,
        long Modified);
}
