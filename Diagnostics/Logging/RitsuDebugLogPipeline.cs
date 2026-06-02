using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal static class RitsuDebugLogPipeline
    {
        private static readonly Lock InitLock = new();
        private static readonly ConcurrentQueue<RitsuDebugLogRecord> Queue = new();
        private static readonly SemaphoreSlim QueueSignal = new(0);
        private static readonly TimeSpan InternalWarningInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan AutoOpenDelay = TimeSpan.FromSeconds(3);

        private static CancellationTokenSource? _cts;
        private static RitsuDebugLogRingBuffer? _ring;
        private static RitsuDebugLogViewerServer? _server;
        private static Task? _worker;
        private static bool _initialized;
        private static RitsuDebugGodotLogListener? _godotLogListener;
        private static int _queueCapacity;
        private static int _queued;
        private static long _nextId;
        private static long _dropped;
        private static DateTimeOffset _lastInternalWarning;

        public static string? ViewerUrl => _server?.Url;

        public static void Initialize(RitsuDebugLogViewerOptions options)
        {
            if (!options.Enabled)
                return;

            lock (InitLock)
            {
                if (_initialized)
                    return;

                try
                {
                    _queueCapacity = Math.Clamp(options.QueueCapacity, 256, 100000);
                    _ring = new(Math.Clamp(options.RingBufferCapacity, 512, 100000));
                    _cts = new();

                    _server = new(options.AccessToken, Snapshot, BuildStatus, ResolveViewerAssetRoot());
                    _server.Start(options.Port, options.PortFallbackCount);

                    _worker = Task.Run(WorkerLoopAsync);
                    if (options.MirrorGameLogs)
                    {
                        _godotLogListener = new();
                        OS.AddLogger(_godotLogListener);
                    }

                    if (options.AutoOpen)
                        _ = Task.Run(() => AutoOpenViewerIfNoClientAsync(_cts.Token));

                    _initialized = true;
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                    RitsuLibFramework.Logger.Info(
                        $"[DebugLogViewer] Local debug log viewer listening at {_server.Url}");
                }
                catch (Exception ex)
                {
                    CleanupAfterFailedStart();
                    RitsuLibFramework.Logger.Warn($"[DebugLogViewer] Failed to start local viewer: {ex.Message}");
                }
            }
        }

        public static void Emit(RitsuDebugLogRecord record)
        {
            if (!_initialized)
                return;

            if (Interlocked.Increment(ref _queued) > _queueCapacity)
            {
                Interlocked.Decrement(ref _queued);
                Interlocked.Increment(ref _dropped);
                return;
            }

            Queue.Enqueue(Normalize(record));
            QueueSignal.Release();
        }

        public static RitsuDebugLogRecord[] Snapshot(int limit)
        {
            return _ring?.Snapshot(limit) ?? [];
        }

        public static object BuildStatus()
        {
            return new
            {
                enabled = _initialized,
                url = ViewerUrl,
                clients = _server?.ClientCount ?? 0,
                bufferCount = _ring?.Count ?? 0,
                bufferCapacity = _ring?.Capacity ?? 0,
                queueDepth = Volatile.Read(ref _queued),
                queueCapacity = _queueCapacity,
                dropped = Volatile.Read(ref _dropped),
            };
        }

        public static void ReportInternalWarning(string message)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastInternalWarning < InternalWarningInterval)
                return;

            _lastInternalWarning = now;
            RitsuLibFramework.Logger.Warn($"[DebugLogViewer] {message}");
        }

        public static (bool Success, string Message) TryOpenViewerInBrowser()
        {
            var url = ViewerUrl;
            if (string.IsNullOrWhiteSpace(url))
                return (false, "RitsuLib debug log viewer is not running.");

            var error = OS.ShellOpen(url);
            return error == Error.Ok
                ? (true, $"Opened RitsuLib debug log viewer: {url}")
                : (false, $"Error {error}: Could not open browser. URL: {url}");
        }

        private static async Task WorkerLoopAsync()
        {
            var token = _cts!.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await QueueSignal.WaitAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                while (Queue.TryDequeue(out var record))
                {
                    Interlocked.Decrement(ref _queued);
                    _ring?.Add(record);
                    _server?.Broadcast(record);
                }
            }
        }

        internal static void EmitGodotLogMessage(string message, bool error)
        {
            Emit(CreateFromGodotLog(message, error));
        }

        private static async Task AutoOpenViewerIfNoClientAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(AutoOpenDelay, token).ConfigureAwait(false);
                if (token.IsCancellationRequested || _server == null || _server.ClientCount > 0)
                    return;

                var result = TryOpenViewerInBrowser();
                if (!result.Success)
                    ReportInternalWarning(result.Message);
            }
            catch (OperationCanceledException)
            {
            }
        }

        internal static void EmitGodotLogError(
            string function,
            string file,
            int line,
            string code,
            string rationale,
            int errorType,
            string scriptBacktrace)
        {
            var body = $"Error occurred [{errorType}]: {rationale}\n{code}\n{file}:{line} @ {function}()";
            if (!string.IsNullOrWhiteSpace(scriptBacktrace))
                body += $"\n{scriptBacktrace}";

            Emit(new()
            {
                Timestamp = DateTimeOffset.UtcNow,
                SeverityText = "ERROR",
                SeverityNumber = 17,
                Body = body,
                Source = "Godot",
                Category = "EngineError",
                LoggerName = "Godot",
                CodeFilePath = file,
                CodeFunctionName = function,
                CodeLineNumber = line,
                Attributes = new Dictionary<string, object?>
                {
                    ["ritsulib.log.source"] = "Godot",
                    ["ritsulib.log.category"] = "EngineError",
                    ["godot.error.type"] = errorType,
                    ["godot.error.code"] = code,
                    ["godot.error.rationale"] = rationale,
                    ["godot.error.script_backtrace"] = scriptBacktrace,
                },
            });
        }

        private static RitsuDebugLogRecord CreateFromGodotLog(string text, bool error)
        {
            var (logLevel, unwrappedText) = ParseLevelPrefix(text, error);
            var (source, category, body) = ParseFormattedLogText(unwrappedText);
            var severityText = logLevel.ToString().ToUpperInvariant();
            var severityNumber = MapSeverityNumber(logLevel);
            var attributes = new Dictionary<string, object?>
            {
                ["log.record.original"] = text,
                ["ritsulib.log.mirrored_from_godot"] = true,
            };

            if (!string.IsNullOrWhiteSpace(source))
                attributes["ritsulib.log.source"] = source;

            if (!string.IsNullOrWhiteSpace(category))
                attributes["ritsulib.log.category"] = category;

            return new()
            {
                Timestamp = DateTimeOffset.UtcNow,
                SeverityText = severityText,
                SeverityNumber = severityNumber,
                Body = body,
                Source = source,
                Category = category,
                LoggerName = source,
                Attributes = attributes,
            };
        }

        private static RitsuDebugLogRecord Normalize(RitsuDebugLogRecord record)
        {
            var timestamp = record.Timestamp == default ? DateTimeOffset.UtcNow : record.Timestamp.ToUniversalTime();
            var id = record.Id > 0 ? record.Id : Interlocked.Increment(ref _nextId);
            var attributes = new Dictionary<string, object?>(record.Attributes);

            if (!string.IsNullOrWhiteSpace(record.Source))
                attributes.TryAdd("ritsulib.log.source", record.Source);

            if (!string.IsNullOrWhiteSpace(record.Category))
                attributes.TryAdd("ritsulib.log.category", record.Category);

            return record with
            {
                Id = id,
                Timestamp = timestamp,
                TimeUnixNano = ToUnixNanoString(timestamp),
                Resource = new Dictionary<string, object?>
                {
                    ["service.name"] = Const.ModId,
                    ["service.version"] = Const.Version,
                },
                Scope = new Dictionary<string, object?>
                {
                    ["name"] = "STS2RitsuLib.Diagnostics.Logging",
                    ["version"] = Const.Version,
                },
                Attributes = attributes,
            };
        }

        private static (string? Source, string? Category, string Body) ParseFormattedLogText(string text)
        {
            var remaining = text.TrimStart();
            if (!TryReadBracketPrefix(remaining, out var first, out remaining))
                return (null, null, text);

            var source = first;
            remaining = remaining.TrimStart();
            string? category = null;
            if (!TryReadBracketPrefix(remaining, out var second, out var afterSecond))
                return (source, category, remaining.Length == 0 ? text : remaining);
            category = second;
            remaining = afterSecond.TrimStart();

            return (source, category, remaining.Length == 0 ? text : remaining);
        }

        private static (LogLevel Level, string Text) ParseLevelPrefix(string text, bool error)
        {
            if (error)
                return (LogLevel.Error, StripKnownLevelPrefix(text));

            var trimmed = text.TrimStart();
            if (TryReadBracketPrefix(trimmed, out var bracketLevel, out var afterBracket) &&
                TryParseLogLevel(bracketLevel, out var level))
                return (level, afterBracket.TrimStart());

            var colon = trimmed.IndexOf(':');
            if (colon is > 0 and <= 10 &&
                TryParseLogLevel(trimmed[..colon], out level))
                return (level, trimmed[(colon + 1)..].TrimStart());

            return (LogLevel.Info, text);
        }

        private static string StripKnownLevelPrefix(string text)
        {
            var trimmed = text.TrimStart();
            if (TryReadBracketPrefix(trimmed, out var bracketLevel, out var afterBracket) &&
                TryParseLogLevel(bracketLevel, out _))
                return afterBracket.TrimStart();

            return text;
        }

        private static bool TryParseLogLevel(string value, out LogLevel level)
        {
            level = value.Trim().ToUpperInvariant() switch
            {
                "VERYDEBUG" or "VDB" => LogLevel.VeryDebug,
                "LOAD" => LogLevel.Load,
                "DEBUG" or "DBG" => LogLevel.Debug,
                "INFO" => LogLevel.Info,
                "WARN" or "WARNING" => LogLevel.Warn,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Info,
            };

            return value.Trim().ToUpperInvariant() is
                "VERYDEBUG" or "VDB" or "LOAD" or "DEBUG" or "DBG" or "INFO" or "WARN" or "WARNING" or "ERROR";
        }

        private static bool TryReadBracketPrefix(string text, out string value, out string remaining)
        {
            value = "";
            remaining = text;

            if (!text.StartsWith("[", StringComparison.Ordinal))
                return false;

            var end = text.IndexOf(']', 1);
            if (end <= 1)
                return false;

            value = text[1..end];
            remaining = text[(end + 1)..];
            return true;
        }

        private static int MapSeverityNumber(LogLevel level)
        {
            return level switch
            {
                LogLevel.VeryDebug => 1,
                LogLevel.Debug => 5,
                LogLevel.Load => 9,
                LogLevel.Info => 9,
                LogLevel.Warn => 13,
                LogLevel.Error => 17,
                _ => 9,
            };
        }

        private static string ToUnixNanoString(DateTimeOffset timestamp)
        {
            return ((timestamp.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100L).ToString();
        }

        private static string? ResolveViewerAssetRoot()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrWhiteSpace(assemblyDir))
                return null;

            return EnumerateViewerAssetRoots(assemblyDir)
                .FirstOrDefault(candidate => File.Exists(Path.Combine(candidate, "index.html")));
        }

        private static IEnumerable<string> EnumerateViewerAssetRoots(string assemblyDir)
        {
            yield return Path.Combine(assemblyDir, "viewer");

            var compatDir = new DirectoryInfo(assemblyDir);
            var libDir = compatDir.Parent;
            var modRoot = libDir?.Parent;
            if (libDir != null &&
                modRoot != null &&
                string.Equals(libDir.Name, "lib", StringComparison.OrdinalIgnoreCase))
                yield return Path.Combine(modRoot.FullName, "viewer");

            yield return Path.Combine(AppContext.BaseDirectory, "viewer");
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            Shutdown();
        }

        private static void Shutdown()
        {
            lock (InitLock)
            {
                if (!_initialized)
                    return;

                _cts?.Cancel();
                _server?.Dispose();
                _godotLogListener = null;
                _cts?.Dispose();
                _server = null;
                _cts = null;
                _initialized = false;
            }
        }

        private static void CleanupAfterFailedStart()
        {
            _cts?.Cancel();
            _server?.Dispose();
            _godotLogListener = null;
            _cts?.Dispose();
            _server = null;
            _cts = null;
            _ring = null;
            _initialized = false;

            while (Queue.TryDequeue(out _))
            {
            }

            Volatile.Write(ref _queued, 0);
        }
    }
}
