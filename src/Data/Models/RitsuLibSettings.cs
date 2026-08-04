using System.Text.Json.Serialization;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Models
{
    /// <summary>
    ///     <para xml:lang="en">Represents RitsuLib's global JSON settings.</para>
    ///     <para xml:lang="zh-CN">表示 RitsuLib 的全局 JSON 设置。</para>
    /// </summary>
    public sealed class RitsuLibSettings
    {
        /// <summary>
        ///     <para xml:lang="en">The current schema version written when settings are created or normalized.</para>
        ///     <para xml:lang="zh-CN">创建或规范化设置时写入的当前架构版本。</para>
        /// </summary>
        public const int CurrentSchemaVersion = 17;

        internal const double DefaultToastDurationSeconds = 6d;
        internal const string DefaultSettingsOpenHotkey = "Ctrl+Shift+F9";
        internal const string DefaultDebugToolsOpenHotkey = "Ctrl+Shift+F10";
        internal const string DefaultCreaturePickerHotkey = "Ctrl+Shift+F11";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the persisted schema version used by the migration pipeline.</para>
        ///     <para xml:lang="zh-CN">获取或设置迁移流程使用的持久化架构版本。</para>
        /// </summary>
        [JsonPropertyName(ModDataVersion.SchemaVersionProperty)]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether managed mod data is synchronized with the game's remote store while Steam Cloud
        ///         is active.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置在 Steam 云可用时，是否将托管的模组数据与游戏的远端存储同步。</para>
        /// </summary>
        [JsonPropertyName("sync_mod_data_to_steam_cloud")]
        public bool SyncModDataToSteamCloud { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the master switch for debug-compatibility shims. When disabled, the individual flags are
        ///         ignored and patched targets retain their original behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置调试兼容适配的总开关。禁用时将忽略各子选项，补丁目标保持原有行为。
        ///     </para>
        /// </summary>
        [JsonPropertyName("debug_compatibility_mode")]
        public bool DebugCompatibilityMode { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether missing <c>LocTable</c> keys fall back to placeholders and emit one-time
        ///         compatibility warnings.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置缺失的 <c>LocTable</c> 键是否回退到占位文本，并输出一次性的兼容性警告。
        ///     </para>
        /// </summary>
        [JsonPropertyName("debug_compat_loc_table")]
        public bool DebugCompatLocTable { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether framework bridges skip invalid epoch grants and emit one-time compatibility
        ///         warnings.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置框架桥接是否跳过无效的纪元授予，并输出一次性的兼容性警告。</para>
        /// </summary>
        [JsonPropertyName("debug_compat_unlock_epoch")]
        public bool DebugCompatUnlockEpoch { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether characters registered through <c>ModContentRegistry</c> receive an empty
        ///         <c>THE_ARCHITECT</c> dialogue fallback when the game resolves no dialogue.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置当游戏未解析到对话时，是否为通过 <c>ModContentRegistry</c> 注册的角色提供空白的
        ///         <c>THE_ARCHITECT</c> 对话回退。
        ///     </para>
        /// </summary>
        [JsonPropertyName("debug_compat_ancient_architect")]
        public bool DebugCompatAncientArchitect { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether the browser-based debug log viewer starts for this session.</para>
        ///     <para xml:lang="zh-CN">获取或设置是否为当前会话启动浏览器调试日志查看器。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_enabled")]
        public bool DebugLogViewerEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether game logger callbacks are mirrored into the viewer's event stream.</para>
        ///     <para xml:lang="zh-CN">获取或设置是否将游戏日志记录器的回调镜像到查看器的事件流。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_mirror_game_logs")]
        public bool DebugLogViewerMirrorGameLogs { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether the viewer opens in the system browser when no client connects shortly
        ///         after startup.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置启动后短时间内没有客户端连接时，是否在系统浏览器中打开查看器。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_auto_open")]
        public bool DebugLogViewerAutoOpen { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether the viewer binds to all network interfaces for LAN access.</para>
        ///     <para xml:lang="zh-CN">获取或设置查看器是否监听所有网络接口，以允许局域网访问。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_lan_access_enabled")]
        public bool DebugLogViewerLanAccessEnabled { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the debug log viewer's HTTP port.</para>
        ///     <para xml:lang="zh-CN">获取或设置调试日志查看器的 HTTP 端口。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_port")]
        public int DebugLogViewerPort { get; set; } = 18742;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets how many consecutive fallback ports are tried when the preferred port is busy.</para>
        ///     <para xml:lang="zh-CN">获取或设置首选端口被占用时继续尝试的连续备用端口数量。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_port_fallback_count")]
        public int DebugLogViewerPortFallbackCount { get; set; } = 20;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the stable browser access token for the debug log viewer.</para>
        ///     <para xml:lang="zh-CN">获取或设置调试日志查看器使用的稳定浏览器访问令牌。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_access_token")]
        public string DebugLogViewerAccessToken { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the number of recent events retained for newly opened browser sessions.</para>
        ///     <para xml:lang="zh-CN">获取或设置为新打开的浏览器会话保留的最近事件数量。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_ring_buffer_capacity")]
        public int DebugLogViewerRingBufferCapacity { get; set; } = 10000;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the pending-event capacity before the non-blocking debug pipeline drops new
        ///         events.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置非阻塞调试流程开始丢弃新事件前的待处理事件容量。</para>
        /// </summary>
        [JsonPropertyName("debug_log_viewer_queue_capacity")]
        public int DebugLogViewerQueueCapacity { get; set; } = 4096;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether developer-console history navigation preserves the current draft.</para>
        ///     <para xml:lang="zh-CN">获取或设置开发者控制台的历史记录导航是否保留当前草稿。</para>
        /// </summary>
        [JsonPropertyName("dev_console_history_navigation_patch_enabled")]
        public bool DevConsoleHistoryNavigationPatchEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether developer-console autocomplete display and candidate-source enhancements
        ///         are enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置是否启用开发者控制台的自动补全显示与候选来源增强。</para>
        /// </summary>
        [JsonPropertyName("dev_console_autocomplete_enhancements_enabled")]
        public bool DevConsoleAutocompleteEnhancementsEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether showing or hiding the developer console clears its input buffer.</para>
        ///     <para xml:lang="zh-CN">获取或设置显示或隐藏开发者控制台时是否清空输入缓冲区。</para>
        /// </summary>
        [JsonPropertyName("dev_console_clear_input_on_visibility_change")]
        public bool DevConsoleClearInputOnVisibilityChange { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether RitsuLib developer tools are enabled. When enabled, the visual workspace and
        ///         developer-console commands can inspect or modify supported game state. In multiplayer this controls
        ///         local access; changes approved by the host are still applied so every player retains the same run
        ///         state. These tools are disabled by default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置是否启用 RitsuLib 开发者工具。启用后，可视化工作台和开发者控制台指令可以检查或修改
        ///         受支持的游戏状态。在多人模式下，此设置只控制本机入口；主机批准的修改仍会生效，使所有玩家保持
        ///         相同的对局状态。这些工具默认关闭。
        ///     </para>
        /// </summary>
        [JsonPropertyName("developer_tools_enabled")]
        public bool DeveloperToolsEnabled { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether a multiplayer host accepts supported state changes requested by other players.
        ///         The host must also enable the developer tools, and every connected player must use a compatible
        ///         RitsuLib version before a request can be accepted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置多人游戏主机是否接受其他玩家请求的受支持状态修改。主机还必须启用开发者工具，
        ///         且每个在线玩家都必须使用兼容的 RitsuLib 版本，请求才会被接受。
        ///     </para>
        /// </summary>
        [JsonPropertyName("developer_tools_allow_client_requests")]
        public bool DeveloperToolsAllowClientRequests { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the runtime key binding that opens the visual developer-tools workspace. The binding is
        ///         ignored while developer tools are disabled. The binding changes only workspace visibility and does
        ///         not enable or disable the feature.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置用于显示或关闭可视化开发者工具工作台的运行时按键绑定。开发者工具关闭时会忽略此绑定；
        ///         此按键绑定只改变工作台可见性，不会启用或禁用该功能。
        ///     </para>
        /// </summary>
        [JsonPropertyName("debug_tools_open_hotkey")]
        public string DebugToolsOpenHotkey { get; set; } = DefaultDebugToolsOpenHotkey;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the runtime key binding that starts direct combat-creature picking. The binding is
        ///         ignored while developer tools are disabled or creature picking is unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置用于直接开始选择战斗生物的运行时按键绑定。开发者工具关闭或无法选择生物时，
        ///         此按键绑定会被忽略。
        ///     </para>
        /// </summary>
        [JsonPropertyName("creature_picker_hotkey")]
        public string CreaturePickerHotkey { get; set; } = DefaultCreaturePickerHotkey;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the runtime key binding that opens the independent mod settings center.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置用于打开独立模组设置中心的运行时按键绑定。
        ///     </para>
        /// </summary>
        [JsonPropertyName("settings_open_hotkey")]
        public string SettingsOpenHotkey { get; set; } = DefaultSettingsOpenHotkey;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether content-source hover tips are enabled.</para>
        ///     <para xml:lang="zh-CN">获取或设置是否启用内容来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_enabled")]
        public bool ModSourceHoverTipsEnabled { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets how content-source hover tips identify a mod. Valid values are <c>name</c>, <c>id</c>,
        ///         and <c>name_and_id</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置内容来源悬停提示如何标识模组。有效值为 <c>name</c>、<c>id</c> 和
        ///         <c>name_and_id</c>。
        ///     </para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_display_style")]
        public string ModSourceHoverTipsDisplayStyle { get; set; } = "name_and_id";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether base-game content also shows source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置原版内容是否也显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_include_vanilla")]
        public bool ModSourceHoverTipsIncludeVanilla { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether supported hover tips outside inspect and detail screens include source
        ///         information.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置详情界面以外受支持的悬停提示是否包含来源信息。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_include_non_details")]
        public bool ModSourceHoverTipsIncludeNonDetails { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether cards show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置卡牌是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_cards")]
        public bool ModSourceHoverTipsCards { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether relics show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置遗物是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_relics")]
        public bool ModSourceHoverTipsRelics { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether potions show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置药水是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_potions")]
        public bool ModSourceHoverTipsPotions { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether powers show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置能力是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_powers")]
        public bool ModSourceHoverTipsPowers { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether orbs show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置充能球是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_orbs")]
        public bool ModSourceHoverTipsOrbs { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether enchantments show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置附魔是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_enchantments")]
        public bool ModSourceHoverTipsEnchantments { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether afflictions show source hover tips.</para>
        ///     <para xml:lang="zh-CN">获取或设置苦痛是否显示来源悬停提示。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_afflictions")]
        public bool ModSourceHoverTipsAfflictions { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether keyword hover tips show source information.</para>
        ///     <para xml:lang="zh-CN">获取或设置关键词悬停提示是否显示来源信息。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_keywords")]
        public bool ModSourceHoverTipsKeywords { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether event layouts show source information.</para>
        ///     <para xml:lang="zh-CN">获取或设置事件界面是否显示来源信息。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_events")]
        public bool ModSourceHoverTipsEvents { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether creature hover tips show source information.</para>
        ///     <para xml:lang="zh-CN">获取或设置生物悬停提示是否显示来源信息。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_creatures")]
        public bool ModSourceHoverTipsCreatures { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether base-game term hover tips, such as block and energy, show source
        ///         information.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置格挡、能量等原版术语的悬停提示是否显示来源信息。</para>
        /// </summary>
        [JsonPropertyName("mod_source_hover_tips_game_terms")]
        public bool ModSourceHoverTipsGameTerms { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the absolute or Godot <c>user://</c> path for Harmony patch-dump output.</para>
        ///     <para xml:lang="zh-CN">获取或设置 Harmony 补丁转储输出使用的绝对路径或 Godot <c>user://</c> 路径。</para>
        /// </summary>
        [JsonPropertyName("harmony_patch_dump_output_path")]
        public string HarmonyPatchDumpOutputPath { get; set; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether a patch dump is written after the first main-menu load of the session.</para>
        ///     <para xml:lang="zh-CN">获取或设置是否在当前会话首次加载主菜单后写入一次补丁转储。</para>
        /// </summary>
        [JsonPropertyName("harmony_patch_dump_on_first_main_menu")]
        public bool HarmonyPatchDumpOnFirstMainMenu { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the output directory for self-check bundles.</para>
        ///     <para xml:lang="zh-CN">获取或设置自检包的输出目录。</para>
        /// </summary>
        [JsonPropertyName("self_check_output_folder_path")]
        public string SelfCheckOutputFolderPath { get; set; } = "user://ritsulib_self_check";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether one self-check bundle is exported after the session's first main-menu
        ///         load.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置是否在每个会话首次加载主菜单后导出一次自检包。</para>
        /// </summary>
        [JsonPropertyName("self_check_on_first_main_menu")]
        public bool SelfCheckOnFirstMainMenu { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the output directory for developer card PNG batch exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置开发者卡牌 PNG 批量导出的输出目录。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_output_path")]
        public string CardPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether card export filenames use localized titles instead of model IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置卡牌导出文件名是否使用本地化标题而非模型 ID。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_use_localized_file_names")]
        public bool CardPngExportUseLocalizedFileNames { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether card exports include an approximate hover-tip-style column on the right.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置卡牌导出是否包含位于右侧的近似悬停提示样式栏。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_include_hover")]
        public bool CardPngExportIncludeHover { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether upgradable cards also produce an <c>_upgraded.png</c> image.</para>
        ///     <para xml:lang="zh-CN">获取或设置可升级卡牌是否同时导出 <c>_upgraded.png</c> 图像。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_include_upgrades")]
        public bool CardPngExportIncludeUpgrades { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the uniform render scale for exported cards.</para>
        ///     <para xml:lang="zh-CN">获取或设置导出卡牌的统一渲染缩放比例。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_scale")]
        public double CardPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets an optional case-insensitive <c>ModelId.Entry</c> substring filter.</para>
        ///     <para xml:lang="zh-CN">获取或设置可选的 <c>ModelId.Entry</c> 子串筛选条件；匹配时忽略大小写。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_id_filter")]
        public string CardPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the maximum number of base cards to export; <c>0</c> means unlimited.</para>
        ///     <para xml:lang="zh-CN">获取或设置最多导出的基础卡牌数量；<c>0</c> 表示不限制。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_max_base_cards")]
        public int CardPngExportMaxBaseCards { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether exports include registered cards hidden from the in-game card library.</para>
        ///     <para xml:lang="zh-CN">获取或设置导出是否包含已注册但在游戏内卡牌图鉴中隐藏的卡牌。</para>
        /// </summary>
        [JsonPropertyName("card_png_export_include_hidden_from_library")]
        public bool CardPngExportIncludeHiddenFromLibrary { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the output directory for relic-detail PNG exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置遗物详情 PNG 导出的输出目录。</para>
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_output_path")]
        public string RelicDetailPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether relic export filenames use localized titles instead of model IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置遗物导出文件名是否使用本地化标题而非模型 ID。</para>
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_use_localized_file_names")]
        public bool RelicDetailPngExportUseLocalizedFileNames { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the render scale for relic-detail exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置遗物详情导出的渲染缩放比例。</para>
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_scale")]
        public double RelicDetailPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets an optional <c>ModelId.Entry</c> substring filter for relic-detail exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置遗物详情导出使用的可选 <c>ModelId.Entry</c> 子串筛选条件。</para>
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_id_filter")]
        public string RelicDetailPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether relic-detail exports include the right-hand hover column.</para>
        ///     <para xml:lang="zh-CN">获取或设置遗物详情导出是否包含右侧悬停提示栏。</para>
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_include_hover")]
        public bool RelicDetailPngExportIncludeHover { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the output directory for potion-detail PNG exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置药水详情 PNG 导出的输出目录。</para>
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_output_path")]
        public string PotionDetailPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether potion export filenames use localized titles instead of model IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置药水导出文件名是否使用本地化标题而非模型 ID。</para>
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_use_localized_file_names")]
        public bool PotionDetailPngExportUseLocalizedFileNames { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the render scale for potion-detail exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置药水详情导出的渲染缩放比例。</para>
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_scale")]
        public double PotionDetailPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets an optional <c>ModelId.Entry</c> substring filter for potion-detail exports.</para>
        ///     <para xml:lang="zh-CN">获取或设置药水详情导出使用的可选 <c>ModelId.Entry</c> 子串筛选条件。</para>
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_id_filter")]
        public string PotionDetailPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the active UI shell theme ID.</para>
        ///     <para xml:lang="zh-CN">获取或设置当前界面外壳主题的 ID。</para>
        /// </summary>
        [JsonPropertyName("ui_shell_theme_id")]
        public string UiShellThemeId { get; set; } = "default";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the default texture filter inherited by 2D canvas items. Valid values are
        ///         <c>nearest</c>, <c>linear</c>, <c>nearest_mipmap</c>, and <c>linear_mipmap</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置二维画布项目继承的默认纹理过滤模式。有效值为 <c>nearest</c>、<c>linear</c>、
        ///         <c>nearest_mipmap</c> 和 <c>linear_mipmap</c>。
        ///     </para>
        /// </summary>
        [JsonPropertyName("canvas_texture_filter_mode")]
        public string CanvasTextureFilterMode { get; set; } = "linear_mipmap";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether RitsuLib periodically checks its mirrored update manifest.</para>
        ///     <para xml:lang="zh-CN">获取或设置 RitsuLib 是否定期检查其镜像更新清单。</para>
        /// </summary>
        [JsonPropertyName("update_check_enabled")]
        public bool UpdateCheckEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the automatic update-check interval in minutes.</para>
        ///     <para xml:lang="zh-CN">获取或设置自动更新检查的间隔分钟数。</para>
        /// </summary>
        [JsonPropertyName("update_check_interval_minutes")]
        public double UpdateCheckIntervalMinutes { get; set; } = 60d;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether periodic update checks are deferred during combat.</para>
        ///     <para xml:lang="zh-CN">获取或设置战斗期间是否推迟定期更新检查。</para>
        /// </summary>
        [JsonPropertyName("update_check_skip_in_combat")]
        public bool UpdateCheckSkipInCombat { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether RitsuLib checks subscribed Steam Workshop items and requests downloads for
        ///         installed items that require an update.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置 RitsuLib 是否检查已订阅的 Steam 创意工坊项目，并请求下载仍需更新的已安装项目。
        ///     </para>
        /// </summary>
        [JsonPropertyName("steam_workshop_auto_update_check_enabled")]
        public bool SteamWorkshopAutoUpdateCheckEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether the main menu shows a shortcut to RitsuLib's mod settings.</para>
        ///     <para xml:lang="zh-CN">获取或设置主菜单是否显示 RitsuLib 模组设置的快捷入口。</para>
        /// </summary>
        [JsonPropertyName("main_menu_mod_settings_button_enabled")]
        public bool MainMenuModSettingsButtonEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the deterministic final-content cache policy for <c>ModelDb</c>. Valid values are
        ///         <c>off</c>, <c>auto</c>, and <c>force</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置 <c>ModelDb</c> 确定性最终内容缓存策略。有效值为 <c>off</c>、<c>auto</c> 和
        ///         <c>force</c>。
        ///     </para>
        /// </summary>
        [JsonPropertyName("modeldb_deterministic_sort_mode")]
        public string ModelDbDeterministicSortMode { get; set; } = "auto";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets whether global non-blocking toast notifications are enabled.</para>
        ///     <para xml:lang="zh-CN">获取或设置是否启用全局非阻塞通知消息。</para>
        /// </summary>
        [JsonPropertyName("toast_enabled")]
        public bool ToastEnabled { get; set; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the 3-by-3 anchor ID used to position toast notifications.</para>
        ///     <para xml:lang="zh-CN">获取或设置通知消息位置使用的 3×3 锚点 ID。</para>
        /// </summary>
        [JsonPropertyName("toast_anchor")]
        public string ToastAnchor { get; set; } = "topright";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the horizontal pixel offset from the selected toast anchor.</para>
        ///     <para xml:lang="zh-CN">获取或设置通知消息相对于所选锚点的水平像素偏移。</para>
        /// </summary>
        [JsonPropertyName("toast_offset_x")]
        public double ToastOffsetX { get; set; } = -24d;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the vertical pixel offset from the selected toast anchor.</para>
        ///     <para xml:lang="zh-CN">获取或设置通知消息相对于所选锚点的垂直像素偏移。</para>
        /// </summary>
        [JsonPropertyName("toast_offset_y")]
        public double ToastOffsetY { get; set; } = 24d;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the maximum number of simultaneously visible toast notifications; overflow is
        ///         queued.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置同时可见的通知消息数量上限；超出部分将进入队列。</para>
        /// </summary>
        [JsonPropertyName("toast_max_visible")]
        public int ToastMaxVisible { get; set; } = 3;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the default toast display duration in seconds.</para>
        ///     <para xml:lang="zh-CN">获取或设置通知消息的默认显示时长（秒）。</para>
        /// </summary>
        [JsonPropertyName("toast_duration_seconds")]
        public double ToastDurationSeconds { get; set; } = DefaultToastDurationSeconds;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the default toast animation preset ID.</para>
        ///     <para xml:lang="zh-CN">获取或设置通知消息的默认动画预设 ID。</para>
        /// </summary>
        [JsonPropertyName("toast_animation")]
        public string ToastAnimation { get; set; } = "fadeslide";
    }
}
