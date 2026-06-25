using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Platform.Steam;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;
using HttpClient = System.Net.Http.HttpClient;

namespace STS2RitsuLib.Platform.Steam
{
    internal sealed class SteamWorkshopManager
    {
        private static readonly HttpClient PreviewHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(12),
        };

        private static readonly SemaphoreSlim PreviewDownloadGate = new(4, 4);

        private readonly Dictionary<string, Texture2D?> _previewTextureCache = new(StringComparer.Ordinal);
        private readonly Lock _previewTextureSyncRoot = new();

        private SteamWorkshopManager()
        {
        }

        public static SteamWorkshopManager Instance { get; } = new();

        public bool IsAvailable => SteamInitializer.Initialized && RitsuSteamWorkshopUpdates.IsAvailable;

        public bool IsSearchAvailable => IsAvailable && RitsuSteamWorkshopUpdates.IsSearchAvailable;

        public Task<IReadOnlyList<RitsuSteamWorkshopItem>> ListSubscribedItemsAsync(
            CancellationToken cancellationToken = default)
        {
            return RitsuSteamWorkshopUpdates.ListSubscribedItemsAsync(cancellationToken);
        }

        public Task<IReadOnlyList<RitsuSteamWorkshopItem>> ListSubscribedItemsFromCacheAsync(
            CancellationToken cancellationToken = default)
        {
            return RitsuSteamWorkshopUpdates.ListSubscribedItemsFromCacheAsync(cancellationToken);
        }

        public Task<IReadOnlyList<RitsuSteamWorkshopItem>> QueryItemsAsync(
            IReadOnlyCollection<ulong> itemIds,
            CancellationToken cancellationToken = default)
        {
            return RitsuSteamWorkshopUpdates.QueryItemsAsync(itemIds, cancellationToken);
        }

        public Task<IReadOnlyList<RitsuSteamWorkshopItem>> SearchItemsAsync(
            string query,
            uint limit = 20,
            CancellationToken cancellationToken = default)
        {
            return RitsuSteamWorkshopUpdates.SearchItemsAsync(query, limit, cancellationToken);
        }

        public RitsuSteamWorkshopActionResult SubscribeFromUi(ulong itemId, string? displayName = null)
        {
            return RequestSubscriptionAction(itemId, displayName, true);
        }

        public RitsuSteamWorkshopActionResult UnsubscribeFromUi(ulong itemId, string? displayName = null)
        {
            return RequestSubscriptionAction(itemId, displayName, false);
        }

        public bool TryOpenWorkshopPage(ulong itemId)
        {
            if (itemId == 0)
                return false;

            var error = OS.ShellOpen($"https://steamcommunity.com/sharedfiles/filedetails/?id={itemId}");
            if (error == Error.Ok)
                return true;

            RitsuLibFramework.Logger.Warn(
                $"[SteamWorkshop] Failed to open Workshop item page for {itemId}. Error={error}.");
            RitsuToastService.ShowWarning(
                L("ritsulib.steamWorkshop.toast.openPageFailed",
                    "Could not open the Steam Workshop item page."),
                L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates"));
            return false;
        }

        public async Task<Texture2D?> LoadPreviewTextureAsync(
            string? previewUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(previewUrl))
                return null;

            var key = previewUrl.Trim();
            lock (_previewTextureSyncRoot)
            {
                if (_previewTextureCache.TryGetValue(key, out var cached))
                    return cached;
            }

            Texture2D? texture = null;
            try
            {
                await PreviewDownloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var bytes = await PreviewHttpClient.GetByteArrayAsync(key, cancellationToken)
                        .ConfigureAwait(false);
                    texture = CreateTexture(bytes);
                }
                finally
                {
                    PreviewDownloadGate.Release();
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SteamWorkshop] Failed to load Workshop preview '{key}': {ex.Message}");
            }

            lock (_previewTextureSyncRoot)
            {
                _previewTextureCache[key] = texture;
            }

            return texture;
        }

        public static bool TryExtractWorkshopItemId(string text, out ulong itemId)
        {
            itemId = 0;
            var trimmed = text.Trim();
            if (ulong.TryParse(trimmed, out itemId) && itemId != 0)
                return true;

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                return false;

            foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length != 2 ||
                    !string.Equals(pair[0], "id", StringComparison.OrdinalIgnoreCase) ||
                    !ulong.TryParse(Uri.UnescapeDataString(pair[1]), out itemId) ||
                    itemId == 0)
                    continue;

                return true;
            }

            return false;
        }

        public static IReadOnlyList<ulong> ParseWorkshopItemIds(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            List<ulong> ids = [];
            HashSet<ulong> seen = [];
            foreach (var token in text.Split(
                         ['\r', '\n', '\t', ' ', ',', ';'],
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (TryExtractWorkshopItemIdFromToken(token, out var itemId) && seen.Add(itemId))
                    ids.Add(itemId);

            if (ids.Count > 0)
                return ids;

            ids.AddRange(EnumerateDigitRunItemIds(text).Where(seen.Add));

            return ids;
        }

        public static string FormatWorkshopItemIdList(IEnumerable<ulong> itemIds)
        {
            return string.Join('\n', itemIds.Where(static id => id != 0).Distinct());
        }

        private static Texture2D? CreateTexture(byte[] bytes)
        {
            if (bytes.Length < 12)
                return null;

            var image = new Image();
            var error = DetectImageFormat(bytes) switch
            {
                PreviewImageFormat.Png => image.LoadPngFromBuffer(bytes),
                PreviewImageFormat.Jpeg => image.LoadJpgFromBuffer(bytes),
                PreviewImageFormat.Webp => image.LoadWebpFromBuffer(bytes),
                _ => Error.FileUnrecognized,
            };
            return error == Error.Ok ? ImageTexture.CreateFromImage(image) : null;
        }

        private static PreviewImageFormat DetectImageFormat(byte[] bytes)
        {
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 &&
                bytes[1] == 0x50 &&
                bytes[2] == 0x4E &&
                bytes[3] == 0x47 &&
                bytes[4] == 0x0D &&
                bytes[5] == 0x0A &&
                bytes[6] == 0x1A &&
                bytes[7] == 0x0A)
                return PreviewImageFormat.Png;

            if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                return PreviewImageFormat.Jpeg;

            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 &&
                bytes[1] == 0x49 &&
                bytes[2] == 0x46 &&
                bytes[3] == 0x46 &&
                bytes[8] == 0x57 &&
                bytes[9] == 0x45 &&
                bytes[10] == 0x42 &&
                bytes[11] == 0x50)
                return PreviewImageFormat.Webp;

            return PreviewImageFormat.Unknown;
        }

        private static bool TryExtractWorkshopItemIdFromToken(string token, out ulong itemId)
        {
            itemId = 0;
            var trimmed = token.Trim().Trim('"', '\'', '[', ']', '(', ')', '<', '>', '{', '}');
            if (TryExtractWorkshopItemId(trimmed, out itemId))
                return true;

            var idIndex = trimmed.IndexOf("id=", StringComparison.OrdinalIgnoreCase);
            if (idIndex < 0)
                return false;

            var value = trimmed[(idIndex + 3)..];
            var end = value.IndexOfAny(['&', '?', '#', '"', '\'', ',', ';', ']', ')', '}']);
            if (end >= 0)
                value = value[..end];

            return ulong.TryParse(Uri.UnescapeDataString(value), out itemId) && itemId != 0;
        }

        private static IEnumerable<ulong> EnumerateDigitRunItemIds(string text)
        {
            var builder = new StringBuilder();
            foreach (var ch in text)
            {
                if (char.IsDigit(ch))
                {
                    builder.Append(ch);
                    continue;
                }

                if (TryFlush(out var itemId))
                    yield return itemId;
            }

            if (TryFlush(out var finalItemId))
                yield return finalItemId;
            yield break;

            bool TryFlush(out ulong itemId)
            {
                itemId = 0;
                if (builder.Length == 0)
                    return false;

                var value = builder.ToString();
                builder.Clear();
                return ulong.TryParse(value, out itemId) && itemId != 0;
            }
        }

        private static RitsuSteamWorkshopActionResult RequestSubscriptionAction(
            ulong itemId,
            string? displayName,
            bool subscribe)
        {
            var action = subscribe ? "subscribe" : "unsubscribe";
            RitsuLibFramework.Logger.Info(
                $"[SteamWorkshop] Workshop {action} requested from UI. Item={itemId}.");
            if (!SteamInitializer.Initialized)
            {
                var result = RitsuSteamWorkshopActionResult.Unavailable(itemId);
                RitsuToastService.ShowWarning(
                    L("ritsulib.steamWorkshop.toast.unavailable",
                        "Steam Workshop is not available in this session."),
                    L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates"));
                return result;
            }

            var actionResult = subscribe
                ? RitsuSteamWorkshopUpdates.RequestSubscribe(itemId)
                : RitsuSteamWorkshopUpdates.RequestUnsubscribe(itemId);
            var title = L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates");
            if (!actionResult.Available)
            {
                RitsuToastService.ShowWarning(
                    actionResult.ErrorMessage ??
                    L("ritsulib.steamWorkshop.toast.unavailable",
                        "Steam Workshop is not available in this session."),
                    title);
                return actionResult;
            }

            if (!actionResult.Accepted)
            {
                RitsuToastService.ShowWarning(
                    Format(
                        subscribe
                            ? "ritsulib.steamWorkshop.toast.subscribeFailed"
                            : "ritsulib.steamWorkshop.toast.unsubscribeFailed",
                        subscribe
                            ? "Steam did not accept the subscribe request for {0}."
                            : "Steam did not accept the unsubscribe request for {0}.",
                        FormatWorkshopItemName(itemId, displayName)),
                    title);
                return actionResult;
            }

            RitsuToastService.ShowInfo(
                Format(
                    subscribe
                        ? "ritsulib.steamWorkshop.toast.subscribeRequested"
                        : "ritsulib.steamWorkshop.toast.unsubscribeRequested",
                    subscribe
                        ? "Asked Steam to subscribe to {0}. Check Steam Downloads and restart after it finishes."
                        : "Asked Steam to unsubscribe from {0}. Restart the game after Steam applies the change.",
                    FormatWorkshopItemName(itemId, displayName)),
                title);
            return actionResult;
        }

        private static string FormatWorkshopItemName(ulong itemId, string? displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? $"Workshop item {itemId}"
                : $"{displayName.Trim()} ({itemId})";
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static string Format(string key, string fallback, params object[] args)
        {
            return string.Format(L(key, fallback), args);
        }

        private enum PreviewImageFormat
        {
            Unknown,
            Png,
            Jpeg,
            Webp,
        }
    }
}
