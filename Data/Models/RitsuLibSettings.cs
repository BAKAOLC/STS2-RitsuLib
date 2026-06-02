using System.Text.Json.Serialization;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Models
{
    /// <summary>
    ///     Global JSON settings blob for RitsuLib itself (schema version and debug flags).
    ///     RitsuLib 自身的全局 JSON 设置数据块（schema 版本和调试标志）。
    /// </summary>
    public sealed class RitsuLibSettings
    {
        /// <summary>
        ///     Current schema version written by the library when creating or normalizing settings.
        ///     库在创建或规范化设置时写入的当前 schema 版本。
        /// </summary>
        public const int CurrentSchemaVersion = 11;

        /// <summary>
        ///     Persisted schema version used by the migration pipeline
        ///     (<see cref="ModDataVersion.SchemaVersionProperty" />).
        ///     迁移管线使用的持久化 schema 版本
        ///     （<see cref="ModDataVersion.SchemaVersionProperty" />）。
        /// </summary>
        [JsonPropertyName(ModDataVersion.SchemaVersionProperty)]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        ///     When true and Steam Cloud is active for the session, RitsuLib keeps managed mod data in sync with
        ///     vanilla’s remote store after saves and on profile init / switch.
        ///     为 true 且本会话启用 Steam Cloud 时，RitsuLib 会在保存后以及档案初始化/切换时，让托管 mod 数据与原版远端存储保持同步。
        /// </summary>
        [JsonPropertyName("sync_mod_data_to_steam_cloud")]
        public bool SyncModDataToSteamCloud { get; set; }

        /// <summary>
        ///     Master switch: when false, sub-flags are ignored and shim logic no-ops so patched targets follow vanilla
        ///     code paths (<c>LocTable</c>, epoch grants, <c>THE_ARCHITECT</c> load, etc.).
        ///     总开关：为 false 时忽略子标志且 shim 逻辑空操作，使补丁目标沿用原版代码路径（<c>LocTable</c>、epoch 授予、
        ///     <c>THE_ARCHITECT</c> 加载等）。
        /// </summary>
        [JsonPropertyName("debug_compatibility_mode")]
        public bool DebugCompatibilityMode { get; set; }

        /// <summary>
        ///     When master is on: soft-fail missing <c>LocTable</c> keys with placeholders and one-time
        ///     <c>[Localization][DebugCompat]</c> warnings. Default true (on new installs and after schema migration).
        ///     总开关开启时：缺失 <c>LocTable</c> 键会以占位符软失败，并输出一次性 <c>[Localization][DebugCompat]</c> 警告。
        ///     默认 true（新安装和 schema 迁移后）。
        /// </summary>
        [JsonPropertyName("debug_compat_loc_table")]
        public bool DebugCompatLocTable { get; set; } = true;

        /// <summary>
        ///     When master and this flag are on: skip invalid epoch grants on framework bridges with one-time
        ///     <c>[Unlocks][DebugCompat]</c> warnings. Otherwise invalid ids use the original grant path (vanilla).
        ///     Default true.
        ///     总开关和此标志都开启时：框架桥接上的无效 epoch 授予会被跳过，并输出一次性
        ///     <c>[Unlocks][DebugCompat]</c> 警告。否则无效 ID 会走原始授予路径（原版）。默认 true。
        /// </summary>
        [JsonPropertyName("debug_compat_unlock_epoch")]
        public bool DebugCompatUnlockEpoch { get; set; } = true;

        /// <summary>
        ///     When master is on: inject empty-lines <c>THE_ARCHITECT</c> dialogue for <c>ModContentRegistry</c>
        ///     characters when vanilla resolves none. Default true.
        ///     总开关开启时：当原版没有解析到对话时，为 <c>ModContentRegistry</c> 角色注入空行
        ///     <c>THE_ARCHITECT</c> 对话。默认 true。
        /// </summary>
        [JsonPropertyName("debug_compat_ancient_architect")]
        public bool DebugCompatAncientArchitect { get; set; } = true;

        /// <summary>
        ///     Starts the loopback-only browser debug log viewer for this session.
        ///     为本会话启动仅监听 loopback 的浏览器调试日志查看器。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_enabled")]
        public bool DebugLogViewerEnabled { get; set; } = true;

        /// <summary>
        ///     Mirrors game logger callbacks into the debug log viewer event stream.
        ///     将游戏 logger 回调镜像到调试日志查看器事件流。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_mirror_game_logs")]
        public bool DebugLogViewerMirrorGameLogs { get; set; } = true;

        /// <summary>
        ///     When true, opens the debug log viewer in the system browser if no browser client connects shortly after startup.
        ///     为 true 时，启动后短时间内若没有浏览器客户端连接，则在系统浏览器中打开调试日志查看器。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_auto_open")]
        public bool DebugLogViewerAutoOpen { get; set; }

        /// <summary>
        ///     Loopback HTTP port for the debug log viewer.
        ///     调试日志查看器的 loopback HTTP 端口。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_port")]
        public int DebugLogViewerPort { get; set; } = 18742;

        /// <summary>
        ///     Number of consecutive ports to try after <see cref="DebugLogViewerPort" /> when the preferred port is busy.
        ///     首选端口被占用时，在 <see cref="DebugLogViewerPort" /> 后继续尝试的连续端口数量。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_port_fallback_count")]
        public int DebugLogViewerPortFallbackCount { get; set; } = 20;

        /// <summary>
        ///     Stable browser access token for the loopback debug log viewer.
        ///     本机调试日志查看器使用的稳定浏览器访问 token。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_access_token")]
        public string DebugLogViewerAccessToken { get; set; } = "";

        /// <summary>
        ///     Number of recent events retained in memory for newly opened browser sessions.
        ///     为新打开的浏览器会话保留在内存中的最近事件数量。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_ring_buffer_capacity")]
        public int DebugLogViewerRingBufferCapacity { get; set; } = 10000;

        /// <summary>
        ///     Maximum pending event count before the non-blocking debug pipeline starts dropping new events.
        ///     非阻塞调试管道开始丢弃新事件前允许排队的最大事件数。
        /// </summary>
        [JsonPropertyName("debug_log_viewer_queue_capacity")]
        public int DebugLogViewerQueueCapacity { get; set; } = 4096;

        /// <summary>
        ///     When true, cards, relics, and potions append a hover tip showing their source mod.
        ///     为 true 时，卡牌、遗物和药水会追加显示其来源 mod 的悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_enabled")]
        public bool ModSourceHoverTipsEnabled { get; set; }

        /// <summary>
        ///     When true, vanilla cards, relics, and potions also show source hover tips.
        ///     为 true 时，原版卡牌、遗物和药水也会显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_include_vanilla")]
        public bool ModSourceHoverTipsIncludeVanilla { get; set; }

        /// <summary>
        ///     When true, selected hover tips outside inspect/detail screens also include source tips.
        ///     为 true 时，详情/检查界面以外的部分悬停提示也会显示来源。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_include_non_details")]
        public bool ModSourceHoverTipsIncludeNonDetails { get; set; }

        /// <summary>
        ///     Shows source hover tips for cards.
        ///     为卡牌显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_cards")]
        public bool ModSourceHoverTipsCards { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for relics.
        ///     为遗物显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_relics")]
        public bool ModSourceHoverTipsRelics { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for potions.
        ///     为药水显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_potions")]
        public bool ModSourceHoverTipsPotions { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for powers.
        ///     为能力显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_powers")]
        public bool ModSourceHoverTipsPowers { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for orbs.
        ///     为充能球显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_orbs")]
        public bool ModSourceHoverTipsOrbs { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for enchantments.
        ///     为附魔显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_enchantments")]
        public bool ModSourceHoverTipsEnchantments { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for afflictions.
        ///     为苦痛显示来源悬停提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_afflictions")]
        public bool ModSourceHoverTipsAfflictions { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for keyword tooltips.
        ///     为关键词悬停提示显示来源。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_keywords")]
        public bool ModSourceHoverTipsKeywords { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for event layouts.
        ///     为事件界面显示来源提示。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_events")]
        public bool ModSourceHoverTipsEvents { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for creature hover tips.
        ///     为生物悬停提示显示来源。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_creatures")]
        public bool ModSourceHoverTipsCreatures { get; set; } = true;

        /// <summary>
        ///     Shows source hover tips for base game term tooltips such as block and energy.
        ///     为格挡、能量等基础游戏术语悬停提示显示来源。
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_game_terms")]
        public bool ModSourceHoverTipsGameTerms { get; set; } = true;

        /// <summary>
        ///     Absolute path or Godot <c>user://</c> path for Harmony patch dump output (text log).
        ///     Harmony 补丁转储输出（文本日志）的绝对路径或 Godot <c>user://</c> 路径。
        /// </summary>
        [JsonPropertyName("harmony_patch_dump_output_path")]
        public string HarmonyPatchDumpOutputPath { get; set; } = string.Empty;

        /// <summary>
        ///     When true, writes a dump once when the main menu first finishes loading this session (deferred).
        ///     为 true 时，在本会话主菜单首次加载完成后延迟写入一次转储。
        /// </summary>
        [JsonPropertyName("harmony_patch_dump_on_first_main_menu")]
        public bool HarmonyPatchDumpOnFirstMainMenu { get; set; }

        /// <summary>
        ///     Output folder for self-check bundles (report + harmony dump + copied godot.log + zip).
        ///     自检包输出文件夹（报告 + Harmony 转储 + 复制的 godot.log + zip）。
        /// </summary>
        [JsonPropertyName("self_check_output_folder_path")]
        public string SelfCheckOutputFolderPath { get; set; } = "user://ritsulib_self_check";

        /// <summary>
        ///     When true, runs one self-check bundle export after the first main-menu load each session.
        ///     为 true 时，每个会话首次加载主菜单后运行一次自检包导出。
        /// </summary>
        [JsonPropertyName("self_check_on_first_main_menu")]
        public bool SelfCheckOnFirstMainMenu { get; set; }

        /// <summary>
        ///     Output directory for dev card PNG batch export (absolute path or <c>user://</c>).
        ///     开发用卡牌 PNG 批量导出的输出目录（绝对路径或 <c>user://</c>）。
        /// </summary>
        [JsonPropertyName("card_png_export_output_path")]
        public string CardPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     When true, export layout includes a right-hand hover-tip style column (approximation, not in-game tooltip
        ///     positioning).
        ///     为 true 时，导出布局包含右侧悬停提示样式列（近似效果，不是游戏内 tooltip 定位）。
        /// </summary>
        [JsonPropertyName("card_png_export_include_hover")]
        public bool CardPngExportIncludeHover { get; set; }

        /// <summary>
        ///     When true, also writes <c>_upgraded.png</c> for upgradable cards.
        ///     为 true 时，也为可升级卡牌写入 <c>_upgraded.png</c>。
        /// </summary>
        [JsonPropertyName("card_png_export_include_upgrades")]
        public bool CardPngExportIncludeUpgrades { get; set; } = true;

        /// <summary>
        ///     Uniform scale for rendered cards (slider domain; clamped when exporting).
        ///     渲染卡牌的统一缩放（滑块范围；导出时会钳制）。
        /// </summary>
        [JsonPropertyName("card_png_export_scale")]
        public double CardPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     Optional substring filter on <c>ModelId.Entry</c> (ordinal ignore-case); empty exports all.
        ///     <c>ModelId.Entry</c> 上的可选子串过滤（序号忽略大小写）；为空则导出全部。
        /// </summary>
        [JsonPropertyName("card_png_export_id_filter")]
        public string CardPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     Maximum number of <em>base</em> cards to process; <c>0</c> means no limit.
        ///     要处理的<em>基础</em>卡牌最大数量；<c>0</c> 表示无限制。
        /// </summary>
        [JsonPropertyName("card_png_export_max_base_cards")]
        public int CardPngExportMaxBaseCards { get; set; }

        /// <summary>
        ///     When true, export includes cards that are registered but hidden from the in-game card library.
        ///     为 true 时，导出包含已注册但在游戏内卡牌图鉴中隐藏的卡牌。
        /// </summary>
        [JsonPropertyName("card_png_export_include_hidden_from_library")]
        public bool CardPngExportIncludeHiddenFromLibrary { get; set; }

        /// <summary>
        ///     Output directory for relic inspect detail PNG export.
        ///     遗物检查详情 PNG 导出的输出目录。
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_output_path")]
        public string RelicDetailPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     Render scale for relic detail export.
        ///     遗物详情导出的渲染缩放。
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_scale")]
        public double RelicDetailPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     Optional <c>ModelId.Entry</c> substring for relic detail export; empty = all.
        ///     遗物详情导出的可选 <c>ModelId.Entry</c> 子串；为空表示全部。
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_id_filter")]
        public string RelicDetailPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     When true, relic detail export includes the right-hand hover column.
        ///     为 true 时，遗物详情导出包含右侧悬停列。
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_include_hover")]
        public bool RelicDetailPngExportIncludeHover { get; set; } = true;

        /// <summary>
        ///     Output directory for potion lab focus detail PNG export.
        ///     药水实验室焦点详情 PNG 导出的输出目录。
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_output_path")]
        public string PotionDetailPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     Render scale for potion detail export.
        ///     药水详情导出的渲染缩放。
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_scale")]
        public double PotionDetailPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     Optional <c>ModelId.Entry</c> substring for potion detail export; empty = all.
        ///     药水详情导出的可选 <c>ModelId.Entry</c> 子串；为空表示全部。
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_id_filter")]
        public string PotionDetailPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     Active shell theme id (e.g. <c>default</c>).
        ///     当前活动的 shell 主题 ID（例如 <c>default</c>）。
        /// </summary>
        [JsonPropertyName("ui_shell_theme_id")]
        public string UiShellThemeId { get; set; } = "default";

        /// <summary>
        ///     When true, RitsuLib checks its mirrored update manifest once after the first main menu load.
        ///     为 true 时，RitsuLib 会在首次进入主菜单后检查一次镜像更新 manifest。
        /// </summary>
        [JsonPropertyName("update_check_enabled")]
        public bool UpdateCheckEnabled { get; set; } = true;

        /// <summary>
        ///     Enables global non-blocking toast notifications.
        ///     启用全局非阻塞 toast 通知。
        /// </summary>
        [JsonPropertyName("toast_enabled")]
        public bool ToastEnabled { get; set; } = true;

        /// <summary>
        ///     3x3 anchor id for toast placement (<c>topright</c>, <c>middlecenter</c>, etc.).
        ///     toast 放置使用的 3x3 锚点 ID（<c>topright</c>、<c>middlecenter</c> 等）。
        /// </summary>
        [JsonPropertyName("toast_anchor")]
        public string ToastAnchor { get; set; } = "topright";

        /// <summary>
        ///     Horizontal offset from the selected anchor in pixels.
        ///     相对于所选锚点的水平偏移（像素）。
        /// </summary>
        [JsonPropertyName("toast_offset_x")]
        public double ToastOffsetX { get; set; } = -24d;

        /// <summary>
        ///     Vertical offset from the selected anchor in pixels.
        ///     相对于所选锚点的垂直偏移（像素）。
        /// </summary>
        [JsonPropertyName("toast_offset_y")]
        public double ToastOffsetY { get; set; } = 24d;

        /// <summary>
        ///     Maximum number of toasts visible at once; overflow is queued.
        ///     同时可见的 toast 最大数量；超出的会排队。
        /// </summary>
        [JsonPropertyName("toast_max_visible")]
        public int ToastMaxVisible { get; set; } = 3;

        /// <summary>
        ///     Default toast display duration (seconds) when requests do not override it.
        ///     请求未覆盖时的默认 toast 显示时长（秒）。
        /// </summary>
        [JsonPropertyName("toast_duration_seconds")]
        public double ToastDurationSeconds { get; set; } = 3.5d;

        /// <summary>
        ///     Default animation preset id (<c>fade</c>, <c>fadeslide</c>, <c>fadescale</c>).
        ///     默认动画预设 ID（<c>fade</c>、<c>fadeslide</c>、<c>fadescale</c>）。
        /// </summary>
        [JsonPropertyName("toast_animation")]
        public string ToastAnimation { get; set; } = "fadeslide";
    }
}
