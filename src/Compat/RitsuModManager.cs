using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">Provides a stable API for querying the host's mod manager.</para>
    ///     <para xml:lang="zh-CN">提供查询宿主模组管理器的稳定 API。</para>
    /// </summary>
    public static class RitsuModManager
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns every detected mod entry, including disabled, failed, duplicate, and runtime-added entries.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回所有检测到的模组条目，包括已禁用、加载失败、重复及运行时新增的条目。</para>
        /// </summary>
        public static IReadOnlyList<RitsuModInfo> GetKnownMods()
        {
            return Sts2ModManagerCompat.BuildModInfos();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns detected entries for a mod ID. Use <paramref name="source" /> to distinguish local and Steam
        ///         Workshop copies with the same ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回指定模组 ID 的已检测条目。可用 <paramref name="source" /> 区分 ID 相同的本地副本和
        ///         Steam 创意工坊副本。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<RitsuModInfo> GetKnownMods(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.BuildModInfos(modId, source);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the preferred current entry for a mod ID. Without a source filter, loaded and local entries rank
        ///         ahead of disabled Steam Workshop duplicates.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取指定模组 ID 当前优先级最高的条目。未指定来源时，已加载和本地条目的优先级高于被禁用的 Steam
        ///         创意工坊重复副本。
        ///     </para>
        /// </summary>
        public static bool TryGetModInfo(string modId, out RitsuModInfo? info, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.TryGetBestModInfo(modId, source, out info);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether the host detected an entry for the mod ID.</para>
        ///     <para xml:lang="zh-CN">返回宿主是否检测到指定模组 ID 的条目。</para>
        /// </summary>
        public static bool ModExists(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.TryGetBestModInfo(modId, source, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether the mod is pending load or already loaded in this session.</para>
        ///     <para xml:lang="zh-CN">返回该模组在本次会话中是否等待加载或已加载。</para>
        /// </summary>
        public static bool WillModLoad(string modId, RitsuModSource? source = null)
        {
            ValidateModId(modId);
            return Sts2ModManagerCompat.BuildModInfos(modId, source).Any(mod => mod.WillLoad);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether the mod loaded successfully in this session.</para>
        ///     <para xml:lang="zh-CN">返回该模组在本次会话中是否已成功加载。</para>
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
    ///     <para xml:lang="en">Identifies a mod source reported through RitsuLib's stable API.</para>
    ///     <para xml:lang="zh-CN">标识通过 RitsuLib 稳定 API 报告的模组来源。</para>
    /// </summary>
    public enum RitsuModSource
    {
        /// <summary>
        ///     <para xml:lang="en">The source could not be mapped from the host API.</para>
        ///     <para xml:lang="zh-CN">无法从宿主 API 映射来源。</para>
        /// </summary>
        Unknown,

        /// <summary>
        ///     <para xml:lang="en">The mod was discovered in the local mods directory.</para>
        ///     <para xml:lang="zh-CN">模组来自本地 mods 目录。</para>
        /// </summary>
        ModsDirectory,

        /// <summary>
        ///     <para xml:lang="en">The mod was discovered through Steam Workshop.</para>
        ///     <para xml:lang="zh-CN">模组来自 Steam 创意工坊。</para>
        /// </summary>
        SteamWorkshop,
    }

    /// <summary>
    ///     <para xml:lang="en">Identifies a mod load state reported through RitsuLib's stable API.</para>
    ///     <para xml:lang="zh-CN">标识通过 RitsuLib 稳定 API 报告的模组加载状态。</para>
    /// </summary>
    public enum RitsuModLoadState
    {
        /// <summary>
        ///     <para xml:lang="en">The state could not be mapped from the host API.</para>
        ///     <para xml:lang="zh-CN">无法从宿主 API 映射状态。</para>
        /// </summary>
        Unknown,

        /// <summary>
        ///     <para xml:lang="en">The host detected the mod but has not attempted to load it.</para>
        ///     <para xml:lang="zh-CN">宿主已检测到该模组，但尚未尝试加载。</para>
        /// </summary>
        Pending,

        /// <summary>
        ///     <para xml:lang="en">The mod loaded successfully in this session.</para>
        ///     <para xml:lang="zh-CN">该模组已在本次会话中成功加载。</para>
        /// </summary>
        Loaded,

        /// <summary>
        ///     <para xml:lang="en">The mod was detected but failed to load.</para>
        ///     <para xml:lang="zh-CN">已检测到该模组，但加载失败。</para>
        /// </summary>
        Failed,

        /// <summary>
        ///     <para xml:lang="en">The mod is disabled for this session.</para>
        ///     <para xml:lang="zh-CN">该模组在本次会话中被禁用。</para>
        /// </summary>
        Disabled,

        /// <summary>
        ///     <para xml:lang="en">The mod was disabled because another copy with the same ID takes precedence.</para>
        ///     <para xml:lang="zh-CN">该模组因另一个 ID 相同的副本优先而被禁用。</para>
        /// </summary>
        DisabledDuplicate,

        /// <summary>
        ///     <para xml:lang="en">The mod was detected after startup and cannot load until a later session.</para>
        ///     <para xml:lang="zh-CN">该模组在启动后才被检测到，需在之后的会话中加载。</para>
        /// </summary>
        AddedAtRuntime,
    }

    /// <summary>
    ///     <para xml:lang="en">Contains a stable snapshot of a detected mod entry.</para>
    ///     <para xml:lang="zh-CN">包含已检测模组条目的稳定快照。</para>
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
        ///     <para xml:lang="en">Gets whether the mod is pending load or already loaded in this session.</para>
        ///     <para xml:lang="zh-CN">获取该模组在本次会话中是否等待加载或已加载。</para>
        /// </summary>
        public bool WillLoad => State is RitsuModLoadState.Pending or RitsuModLoadState.Loaded;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the mod loaded successfully in this session.</para>
        ///     <para xml:lang="zh-CN">获取该模组在本次会话中是否已成功加载。</para>
        /// </summary>
        public bool IsLoaded => State == RitsuModLoadState.Loaded;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether this is a Steam Workshop copy disabled in favor of another copy with the same ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此条目是否为因另一个 ID 相同副本优先而被禁用的 Steam 创意工坊副本。
        ///     </para>
        /// </summary>
        public bool IsDisabledSteamWorkshopDuplicate =>
            Source == RitsuModSource.SteamWorkshop && State == RitsuModLoadState.DisabledDuplicate;
    }
}
