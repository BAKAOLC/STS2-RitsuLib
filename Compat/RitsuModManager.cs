using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Stable query helpers over the host ModManager.
    ///     宿主 ModManager 的稳定查询辅助接口。
    /// </summary>
    public static class RitsuModManager
    {
        /// <summary>
        ///     Returns all detected mods, including disabled, failed, duplicate, and runtime-added entries.
        ///     返回所有已检测到的 mod，包括禁用、失败、重复以及运行时新增的条目。
        /// </summary>
        public static IReadOnlyList<RitsuModInfo> GetKnownMods()
        {
            return Sts2ModManagerCompat.BuildModInfos();
        }

        /// <summary>
        ///     Returns detected entries for a mod id. Pass <paramref name="source" /> to distinguish local and Steam
        ///     Workshop copies with the same id.
        ///     返回指定 mod id 的已检测条目。传入 <paramref name="source" /> 可区分同 ID 的本地与 Steam Workshop 副本。
        /// </summary>
        public static IReadOnlyList<RitsuModInfo> GetKnownMods(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.BuildModInfos(modId, source);
        }

        /// <summary>
        ///     Gets the best current entry for a mod id. Without a source filter, loaded/local entries are preferred over
        ///     disabled Steam Workshop duplicates.
        ///     获取指定 mod id 当前最有效的条目。未指定来源时，已加载/本地条目优先于被禁用的 Steam Workshop 重复副本。
        /// </summary>
        public static bool TryGetModInfo(string modId, out RitsuModInfo? info, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.TryGetBestModInfo(modId, source, out info);
        }

        /// <summary>
        ///     Returns true when ModManager has detected an entry for the mod id.
        ///     当 ModManager 已检测到指定 mod id 的条目时返回 true。
        /// </summary>
        public static bool ModExists(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.TryGetBestModInfo(modId, source, out _);
        }

        /// <summary>
        ///     Returns true when the mod is either pending load or already loaded in this session.
        ///     当该 mod 在本会话中等待加载或已经加载时返回 true。
        /// </summary>
        public static bool WillModLoad(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.BuildModInfos(modId, source).Any(mod => mod.WillLoad);
        }

        /// <summary>
        ///     Returns true when the mod has loaded successfully in this session.
        ///     当该 mod 已在本会话中成功加载时返回 true。
        /// </summary>
        public static bool IsModLoaded(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.BuildModInfos(modId, source).Any(mod => mod.IsLoaded);
        }

        private static void ValidateModId(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        }
    }

    /// <summary>
    ///     Mod source reported through RitsuLib's stable ModManager wrapper.
    ///     RitsuLib 稳定 ModManager 包装层报告的 mod 来源。
    /// </summary>
    public enum RitsuModSource
    {
        /// <summary>
        ///     Source could not be mapped from the host API.
        ///     无法从宿主 API 映射来源。
        /// </summary>
        Unknown,

        /// <summary>
        ///     Mod was discovered from the local mods directory.
        ///     mod 来自本地 mods 目录。
        /// </summary>
        ModsDirectory,

        /// <summary>
        ///     Mod was discovered from Steam Workshop.
        ///     mod 来自 Steam Workshop。
        /// </summary>
        SteamWorkshop,
    }

    /// <summary>
    ///     Mod load state reported through RitsuLib's stable ModManager wrapper.
    ///     RitsuLib 稳定 ModManager 包装层报告的 mod 加载状态。
    /// </summary>
    public enum RitsuModLoadState
    {
        /// <summary>
        ///     State could not be mapped from the host API.
        ///     无法从宿主 API 映射状态。
        /// </summary>
        Unknown,

        /// <summary>
        ///     ModManager has detected the mod, but loading has not been attempted yet.
        ///     ModManager 已检测到该 mod，但尚未尝试加载。
        /// </summary>
        Pending,

        /// <summary>
        ///     The mod loaded successfully in this session.
        ///     该 mod 已在本会话中成功加载。
        /// </summary>
        Loaded,

        /// <summary>
        ///     The mod was detected but failed to load.
        ///     该 mod 已检测到，但加载失败。
        /// </summary>
        Failed,

        /// <summary>
        ///     The mod is disabled for this session.
        ///     该 mod 在本会话中被禁用。
        /// </summary>
        Disabled,

        /// <summary>
        ///     The mod was disabled because another copy with the same id takes precedence.
        ///     该 mod 因同 ID 的另一个副本优先而被禁用。
        /// </summary>
        DisabledDuplicate,

        /// <summary>
        ///     The mod was detected after startup and cannot load until a later session.
        ///     该 mod 在启动后才被检测到，需之后的会话才可能加载。
        /// </summary>
        AddedAtRuntime,
    }

    /// <summary>
    ///     Stable snapshot of a detected ModManager entry.
    ///     ModManager 已检测条目的稳定快照。
    /// </summary>
    public sealed record RitsuModInfo(
        string Id,
        string Name,
        string? Author,
        string? Version,
        RitsuModLoadState State,
        RitsuModSource Source,
        bool AffectsGameplay,
        string? AssemblyName,
        string? AssemblyVersion,
        IReadOnlyList<LocString> Errors,
        ulong? WorkshopItemId = null)
    {
        /// <summary>
        ///     True when the mod is either pending load or already loaded in this session.
        ///     当该 mod 在本会话中等待加载或已经加载时为 true。
        /// </summary>
        public bool WillLoad => State is RitsuModLoadState.Pending or RitsuModLoadState.Loaded;

        /// <summary>
        ///     True when the mod loaded successfully in this session.
        ///     当该 mod 已在本会话中成功加载时为 true。
        /// </summary>
        public bool IsLoaded => State == RitsuModLoadState.Loaded;

        /// <summary>
        ///     True when this entry is a Steam Workshop copy disabled in favor of another copy with the same id.
        ///     当此条目是因同 ID 其他副本优先而被禁用的 Steam Workshop 副本时为 true。
        /// </summary>
        public bool IsDisabledSteamWorkshopDuplicate =>
            Source == RitsuModSource.SteamWorkshop && State == RitsuModLoadState.DisabledDuplicate;
    }
}
