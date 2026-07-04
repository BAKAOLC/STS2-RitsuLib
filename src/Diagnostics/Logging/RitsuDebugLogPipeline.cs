using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Utils;
using Environment = System.Environment;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal static class RitsuDebugLogPipeline
    {
        private static readonly Lock InitLock = new();
        private static readonly ConcurrentQueue<RitsuDebugLogRecord> Queue = new();
        private static readonly SemaphoreSlim QueueSignal = new(0);
        private static readonly TimeSpan InternalWarningInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan AutoOpenDelay = TimeSpan.FromSeconds(3);
        private static readonly string SessionId = Guid.NewGuid().ToString("N");
        private static readonly DateTimeOffset SessionStartedAtUtc = DateTimeOffset.UtcNow;

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
            lock (InitLock)
            {
                if (_initialized)
                    return;

                try
                {
                    _queueCapacity = Math.Clamp(options.QueueCapacity, 256, 100000);
                    _ring = new(Math.Clamp(options.RingBufferCapacity, 512, 100000));
                    _cts = new();
                    _worker = Task.Run(WorkerLoopAsync);
                    _godotLogListener = new();
                    OS.AddLogger(_godotLogListener);

                    if (options.Enabled)
                        StartServer(options);

                    _initialized = true;
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                    if (_server != null)
                        RitsuLibFramework.Logger.Info(CreateViewerStartMessage(_server));
                }
                catch (Exception ex)
                {
                    CleanupAfterFailedStart();
                    RitsuLibFramework.Logger.Warn($"[DebugLogViewer] Failed to initialize log capture: {ex.Message}");
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
            DrainQueue();
            return _ring?.Snapshot(limit) ?? [];
        }

        public static object BuildStatus()
        {
            return new
            {
                enabled = _initialized,
                sessionId = SessionId,
                sessionStartedAtUtc = SessionStartedAtUtc,
                processId = Environment.ProcessId,
                url = ViewerUrl,
                serverEnabled = _server != null,
                accessMode = _server?.AccessMode ?? "loopback",
                lanAccessEnabled = _server?.LanAccessEnabled ?? false,
                lanUrls = _server?.LanUrls ?? [],
                clients = _server?.ClientCount ?? 0,
                bufferCount = _ring?.Count ?? 0,
                bufferCapacity = _ring?.Capacity ?? 0,
                queueDepth = Volatile.Read(ref _queued),
                queueCapacity = _queueCapacity,
                dropped = Volatile.Read(ref _dropped),
            };
        }

        private static void StartServer(RitsuDebugLogViewerOptions options)
        {
            try
            {
                _server = new(
                    options.AccessToken,
                    options.LanAccessEnabled,
                    Snapshot,
                    BuildStatus,
                    ResolveViewerAssetRoot());
                _server.Start(options.Port, options.PortFallbackCount);

                if (options.AutoOpen)
                    _ = Task.Run(() => AutoOpenViewerIfNoClientAsync(_cts!.Token));
            }
            catch (Exception ex)
            {
                _server?.Dispose();
                _server = null;
                RitsuLibFramework.Logger.Warn($"[DebugLogViewer] Failed to start viewer server: {ex.Message}");
            }
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

        private static string CreateViewerStartMessage(RitsuDebugLogViewerServer server)
        {
            if (!server.LanAccessEnabled)
                return $"[DebugLogViewer] Local debug log viewer listening at {server.Url}";

            var lanUrls = server.LanUrls;
            return lanUrls.Count == 0
                ? $"[DebugLogViewer] LAN debug log viewer listening at {server.Url}; no LAN IPv4 address was found."
                : $"[DebugLogViewer] LAN debug log viewer listening at {server.Url}; LAN URLs: {string.Join(", ", lanUrls)}";
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

                DrainQueue();
            }
        }

        private static void DrainQueue()
        {
            while (Queue.TryDequeue(out var record))
            {
                Interlocked.Decrement(ref _queued);
                _ring?.Add(record);
                _server?.Broadcast(record);
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
            var plainText = RitsuAnsiText.StripControlSequences(text);
            var (logLevel, unwrappedText, unwrappedTextStart) = ParseLevelPrefix(plainText, error);
            var (source, category, body, bodyStartInUnwrappedText) = ParseFormattedLogText(unwrappedText);
            var severityText = logLevel.ToString().ToUpperInvariant();
            var severityNumber = MapSeverityNumber(logLevel);
            var bodyStart = unwrappedTextStart + bodyStartInUnwrappedText;
            var attributes = new Dictionary<string, object?>
            {
                ["log.record.original"] = text,
                ["ritsulib.log.mirrored_from_godot"] = true,
            };
            if (!string.Equals(plainText, text, StringComparison.Ordinal))
            {
                attributes["ritsulib.log.ansi_stripped"] = true;
                attributes["ritsulib.log.plain_text"] = plainText;
            }

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
                BodySegments = BuildBodySegments(text, body, bodyStart),
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
                BodySegments = record.BodySegments is { Count: > 0 }
                    ? record.BodySegments
                    : BuildPlainBodySegments(record.Body),
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

        private static (string? Source, string? Category, string Body, int BodyStart) ParseFormattedLogText(string text)
        {
            var remaining = text.TrimStart();
            var remainingStart = text.Length - remaining.Length;
            if (!TryReadBracketPrefix(remaining, out var first, out remaining))
                return (null, null, text, 0);

            var source = first;
            remainingStart += text[remainingStart..].Length - remaining.Length;
            remaining = remaining.TrimStart();
            remainingStart += text[remainingStart..].Length - remaining.Length;
            string? category = null;
            if (!TryReadBracketPrefix(remaining, out var second, out var afterSecond))
                return remaining.Length == 0
                    ? (source, category, text, 0)
                    : (source, category, remaining, remainingStart);

            category = second;
            remainingStart += remaining.Length - afterSecond.Length;
            remaining = afterSecond.TrimStart();
            remainingStart += afterSecond.Length - remaining.Length;

            return remaining.Length == 0
                ? (source, category, text, 0)
                : (source, category, remaining, remainingStart);
        }

        private static IReadOnlyList<RitsuTextSegment> BuildBodySegments(string rawText, string body, int bodyStart)
        {
            if (string.IsNullOrEmpty(body))
                return [];

            var segments = SliceVisibleSegments(RitsuAnsiText.ParseSegments(rawText), bodyStart, body.Length);
            return segments.Count == 0 ? BuildPlainBodySegments(body) : segments;
        }

        private static IReadOnlyList<RitsuTextSegment> BuildPlainBodySegments(string body)
        {
            return string.IsNullOrEmpty(body) ? [] : [new() { Text = body }];
        }

        private static IReadOnlyList<RitsuTextSegment> SliceVisibleSegments(
            IReadOnlyList<RitsuTextSegment> segments,
            int start,
            int length)
        {
            if (segments.Count == 0 || start < 0 || length <= 0)
                return [];

            var result = new List<RitsuTextSegment>();
            var end = start + length;
            var position = 0;
            foreach (var segment in segments)
            {
                var nextPosition = position + segment.Text.Length;
                if (nextPosition <= start)
                {
                    position = nextPosition;
                    continue;
                }

                if (position >= end)
                    break;

                var segmentStart = Math.Max(0, start - position);
                var segmentEnd = Math.Min(segment.Text.Length, end - position);
                if (segmentEnd > segmentStart)
                    result.Add(segment with { Text = segment.Text[segmentStart..segmentEnd] });

                position = nextPosition;
            }

            return result;
        }

        private static (LogLevel Level, string Text, int TextStart) ParseLevelPrefix(string text, bool error)
        {
            if (error)
            {
                var (strippedText, strippedTextStart) = StripKnownLevelPrefix(text);
                return (LogLevel.Error, strippedText, strippedTextStart);
            }

            var trimmed = text.TrimStart();
            var trimmedStart = text.Length - trimmed.Length;
            if (TryReadBracketPrefix(trimmed, out var bracketLevel, out var afterBracket) &&
                TryParseLogLevel(bracketLevel, out var level))
            {
                var afterBracketStart = trimmedStart + trimmed.Length - afterBracket.Length;
                var afterBracketTrimmed = afterBracket.TrimStart();
                var afterBracketTrimmedStart = afterBracketStart + afterBracket.Length - afterBracketTrimmed.Length;
                return (level, afterBracketTrimmed, afterBracketTrimmedStart);
            }

            var colon = trimmed.IndexOf(':');
            if (colon is <= 0 or > 10 ||
                !TryParseLogLevel(trimmed[..colon], out level)) return (LogLevel.Info, text, 0);
            var afterColon = trimmed[(colon + 1)..];
            var afterColonTrimmed = afterColon.TrimStart();
            var afterColonTrimmedStart = trimmedStart + colon + 1 + afterColon.Length - afterColonTrimmed.Length;
            return (level, afterColonTrimmed, afterColonTrimmedStart);
        }

        private static (string Text, int TextStart) StripKnownLevelPrefix(string text)
        {
            var trimmed = text.TrimStart();
            var trimmedStart = text.Length - trimmed.Length;
            if (!TryReadBracketPrefix(trimmed, out var bracketLevel, out var afterBracket) ||
                !TryParseLogLevel(bracketLevel, out _)) return (text, 0);
            var afterBracketStart = trimmedStart + trimmed.Length - afterBracket.Length;
            var afterBracketTrimmed = afterBracket.TrimStart();
            return (afterBracketTrimmed, afterBracketStart + afterBracket.Length - afterBracketTrimmed.Length);
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

            if (!text.StartsWith('['))
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
