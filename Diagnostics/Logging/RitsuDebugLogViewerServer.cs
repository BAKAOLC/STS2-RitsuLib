using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed class RitsuDebugLogViewerServer : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
        };

        private readonly string? _assetRoot;

        private readonly ConcurrentDictionary<RitsuDebugLogSseClient, byte> _clients = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Func<int, RitsuDebugLogRecord[]> _historyProvider;
        private readonly Func<object> _statusProvider;
        private readonly string _token;
        private Task? _acceptTask;
        private TcpListener? _listener;

        public RitsuDebugLogViewerServer(
            string token,
            Func<int, RitsuDebugLogRecord[]> historyProvider,
            Func<object> statusProvider,
            string? assetRoot)
        {
            _token = token;
            _historyProvider = historyProvider;
            _statusProvider = statusProvider;
            _assetRoot = string.IsNullOrWhiteSpace(assetRoot) ? null : assetRoot;
        }

        public int Port { get; private set; }

        public int ClientCount => _clients.Count;

        public string Url => $"http://127.0.0.1:{Port}/?token={Uri.EscapeDataString(_token)}";

        public void Dispose()
        {
            _cts.Cancel();
            _listener?.Stop();
            _clients.Clear();
            _cts.Dispose();
        }

        public void Start(int requestedPort, int fallbackCount)
        {
            var firstPort = Math.Clamp(requestedPort, 1, 65535);
            var maxAttempts = Math.Min(Math.Clamp(fallbackCount + 1, 1, 100), 65536 - firstPort);
            Exception? lastException = null;
            for (var i = 0; i < maxAttempts; i++)
            {
                var port = firstPort + i;
                try
                {
                    _listener = new(IPAddress.Loopback, port);
                    _listener.Start();
                    break;
                }
                catch (Exception ex) when (ex is SocketException or InvalidOperationException)
                {
                    lastException = ex;
                    _listener?.Stop();
                    _listener = null;
                }
            }

            if (_listener == null)
                throw new InvalidOperationException(
                    $"Could not bind local debug viewer port range {firstPort}-{firstPort + maxAttempts - 1}.",
                    lastException);

            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _acceptTask = Task.Run(AcceptLoopAsync);
        }

        public void Broadcast(RitsuDebugLogRecord record)
        {
            if (_clients.IsEmpty)
                return;

            var json = JsonSerializer.Serialize(record, JsonOptions);
            foreach (var client in _clients.Keys)
                client.Enqueue(json);
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener!.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    RitsuDebugLogPipeline.ReportInternalWarning($"Accept loop failed: {ex.Message}");
                    continue;
                }

                _ = Task.Run(() => HandleClientAsync(client), _cts.Token);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.NoDelay = true;
                    await using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 2048, true);
                    var requestLine = await reader.ReadLineAsync(_cts.Token).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(requestLine))
                        return;

                    var parts = requestLine.Split(' ');
                    if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        await WriteTextResponseAsync(stream, 405, "Method Not Allowed", "Only GET is supported.")
                            .ConfigureAwait(false);
                        return;
                    }

                    while (true)
                    {
                        var header = await reader.ReadLineAsync(_cts.Token).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(header))
                            break;
                    }

                    var target = parts[1];
                    var path = target;
                    var query = "";
                    var queryIndex = target.IndexOf('?', StringComparison.Ordinal);
                    if (queryIndex >= 0)
                    {
                        path = target[..queryIndex];
                        query = target[(queryIndex + 1)..];
                    }

                    if (IsStaticAssetRequest(path))
                    {
                        if (await TryWriteStaticAssetAsync(stream, path).ConfigureAwait(false))
                            return;

                        await WriteTextResponseAsync(stream, 404, "Not Found", "Static asset not found.")
                            .ConfigureAwait(false);
                        return;
                    }

                    if (!IsAuthorized(query))
                    {
                        await WriteTextResponseAsync(stream, 401, "Unauthorized", "Invalid debug viewer token.")
                            .ConfigureAwait(false);
                        return;
                    }

                    switch (path)
                    {
                        case "/":
                            if (!await TryWriteIndexAsync(stream).ConfigureAwait(false))
                                await WriteHtmlResponseAsync(stream, RitsuDebugLogViewerStaticAssets.IndexHtml)
                                    .ConfigureAwait(false);
                            return;
                        case "/api/status":
                            await WriteJsonResponseAsync(stream, _statusProvider()).ConfigureAwait(false);
                            return;
                        case "/api/history":
                            await WriteJsonResponseAsync(stream, _historyProvider(ParseLimit(query)))
                                .ConfigureAwait(false);
                            return;
                        case "/api/events":
                            await HandleEventsAsync(stream).ConfigureAwait(false);
                            return;
                        default:
                            await WriteTextResponseAsync(stream, 404, "Not Found", "Not found.").ConfigureAwait(false);
                            return;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex) when (IsClientDisconnect(ex))
                {
                }
                catch (Exception ex)
                {
                    RitsuDebugLogPipeline.ReportInternalWarning($"Client request failed: {ex.Message}");
                }
            }
        }

        private async Task HandleEventsAsync(Stream stream)
        {
            var client = new RitsuDebugLogSseClient();
            _clients.TryAdd(client, 0);

            try
            {
                await WriteUtf8Async(
                    stream,
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: text/event-stream; charset=utf-8\r\n" +
                    "Cache-Control: no-cache\r\n" +
                    "Connection: keep-alive\r\n" +
                    "X-Accel-Buffering: no\r\n\r\n").ConfigureAwait(false);

                while (!_cts.IsCancellationRequested)
                {
                    var json = await client.DequeueAsync(TimeSpan.FromSeconds(15), _cts.Token).ConfigureAwait(false);
                    if (json == null)
                    {
                        await WriteUtf8Async(stream, ": keepalive\n\n").ConfigureAwait(false);
                    }
                    else
                    {
                        await WriteUtf8Async(stream, "event: log\n").ConfigureAwait(false);
                        await WriteUtf8Async(stream, "data: ").ConfigureAwait(false);
                        await WriteUtf8Async(stream, json).ConfigureAwait(false);
                        await WriteUtf8Async(stream, "\n\n").ConfigureAwait(false);
                    }

                    await stream.FlushAsync(_cts.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                _clients.TryRemove(client, out _);
            }
        }

        private bool IsAuthorized(string query)
        {
            return string.Equals(GetQueryValue(query, "token"), _token, StringComparison.Ordinal);
        }

        private static bool IsStaticAssetRequest(string path)
        {
            return !string.Equals(path, "/", StringComparison.Ordinal) &&
                   !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> TryWriteIndexAsync(Stream stream)
        {
            if (string.IsNullOrWhiteSpace(_assetRoot))
                return false;

            var indexPath = Path.Combine(_assetRoot, "index.html");
            if (!File.Exists(indexPath))
                return false;

            await WriteFileResponseAsync(stream, indexPath, "text/html; charset=utf-8").ConfigureAwait(false);
            return true;
        }

        private async Task<bool> TryWriteStaticAssetAsync(Stream stream, string path)
        {
            if (string.IsNullOrWhiteSpace(_assetRoot))
                return false;

            var relative = Uri.UnescapeDataString(path.TrimStart('/')).Replace('/', Path.DirectorySeparatorChar);
            if (relative.Length == 0 || relative.Split(Path.DirectorySeparatorChar).Any(static x => x == ".."))
                return false;

            var fullPath = Path.GetFullPath(Path.Combine(_assetRoot, relative));
            var root = Path.GetFullPath(_assetRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
                return false;

            await WriteFileResponseAsync(stream, fullPath, ResolveContentType(fullPath)).ConfigureAwait(false);
            return true;
        }

        private static int ParseLimit(string query)
        {
            return int.TryParse(GetQueryValue(query, "limit"), out var limit) ? Math.Clamp(limit, 1, 20000) : 5000;
        }

        private static string? GetQueryValue(string query, string key)
        {
            return (from pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                let eq = pair.IndexOf('=', StringComparison.Ordinal)
                let rawKey = eq >= 0 ? pair[..eq] : pair
                where string.Equals(Uri.UnescapeDataString(rawKey), key, StringComparison.Ordinal)
                select eq >= 0 ? pair[(eq + 1)..] : ""
                into rawValue
                select Uri.UnescapeDataString(rawValue.Replace("+", "%20", StringComparison.Ordinal))).FirstOrDefault();
        }

        private static Task WriteHtmlResponseAsync(Stream stream, string html)
        {
            return WriteResponseAsync(stream, 200, "OK", "text/html; charset=utf-8", html);
        }

        private static Task WriteJsonResponseAsync(Stream stream, object payload)
        {
            return WriteResponseAsync(
                stream,
                200,
                "OK",
                "application/json; charset=utf-8",
                JsonSerializer.Serialize(payload, JsonOptions));
        }

        private static async Task WriteFileResponseAsync(Stream stream, string path, string contentType)
        {
            var bodyBytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            await WriteBytesResponseAsync(stream, 200, "OK", contentType, bodyBytes).ConfigureAwait(false);
        }

        private static Task WriteTextResponseAsync(Stream stream, int status, string reason, string text)
        {
            return WriteResponseAsync(stream, status, reason, "text/plain; charset=utf-8", text);
        }

        private static async Task WriteResponseAsync(
            Stream stream,
            int status,
            string reason,
            string contentType,
            string body)
        {
            await WriteBytesResponseAsync(stream, status, reason, contentType, Encoding.UTF8.GetBytes(body))
                .ConfigureAwait(false);
        }

        private static async Task WriteBytesResponseAsync(
            Stream stream,
            int status,
            string reason,
            string contentType,
            byte[] bodyBytes)
        {
            await WriteUtf8Async(
                stream,
                $"HTTP/1.1 {status} {reason}\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Cache-Control: no-cache\r\n" +
                "Connection: close\r\n\r\n").ConfigureAwait(false);
            await stream.WriteAsync(bodyBytes).ConfigureAwait(false);
        }

        private static string ResolveContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".js" => "text/javascript; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                _ => "application/octet-stream",
            };
        }

        private static Task WriteUtf8Async(Stream stream, string text)
        {
            return stream.WriteAsync(Encoding.UTF8.GetBytes(text)).AsTask();
        }

        private static bool IsClientDisconnect(Exception ex)
        {
            return ex switch
            {
                IOException => true,
                ObjectDisposedException => true,
                SocketException
                {
                    SocketErrorCode: SocketError.ConnectionAborted or SocketError.ConnectionReset
                    or SocketError.Shutdown,
                } => true,
                _ when ex.InnerException != null => IsClientDisconnect(ex.InnerException),
                _ => false,
            };
        }
    }
}
