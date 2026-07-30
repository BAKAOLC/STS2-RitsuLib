using System.Security.Cryptography;
using System.Text;
using Godot;
using STS2RitsuLib.Audio.Internal;
using Array = Godot.Collections.Array;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides direct FMOD Studio bank loading, GUID mapping, cache probes, and bank diagnostics
    ///         through the Godot FMOD add-on.
    ///     </para>
    ///     <para xml:lang="zh-CN">通过 Godot FMOD 插件提供直接的 FMOD Studio 音频库加载、GUID 映射、缓存探测和音频库诊断。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         These operations bypass the game's mixer-facing playback API. Use
    ///         <see cref="GameFmod.Studio" /> for gameplay audio that should follow vanilla routing.
    ///     </para>
    ///     <para xml:lang="zh-CN">这些操作不经过游戏面向混音器的播放 API。需要遵循原版路由的游戏音频请使用 <see cref="GameFmod.Studio" />。</para>
    /// </remarks>
    public static class FmodStudioServer
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Protects references returned by <c>load_bank</c>, because the add-on's <c>FmodBank</c>
        ///         destructor unloads its bank when the last reference is released.
        ///     </para>
        ///     <para xml:lang="zh-CN">保留 <c>load_bank</c> 返回的引用，因为插件的 <c>FmodBank</c> 析构函数会在最后一个引用释放时卸载对应音频库。</para>
        /// </summary>
        private static readonly Lock LoadedBankPinsGate = new();

        private static readonly Dictionary<string, GodotObject> LoadedBankPins = [];

        private static readonly StringName BankGetGodotResourcePath = new("get_godot_res_path");
        private static readonly StringName BankGetEventDescriptionCount = new("get_event_description_count");
        private static readonly StringName BankGetDescriptionList = new("get_description_list");
        private static readonly StringName EventDescriptionGetPath = new("get_path");

        private static readonly StringName[] GuidMappingInjectCandidates =
        [
            new("register_guid_path_mappings_from_file"),
            new("inject_guid_mappings_from_file"),
            new("register_strings_from_guid_file"),
            new("load_guid_mapping_file"),
        ];

        /// <summary>
        ///     <para xml:lang="en">Attempts to retrieve the valid Godot <c>FmodServer</c> singleton.</para>
        ///     <para xml:lang="zh-CN">尝试获取有效的 Godot <c>FmodServer</c> 单例。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The valid singleton, or null when it is absent or lookup fails.</para>
        ///     <para xml:lang="zh-CN">有效单例；不存在或查找失败时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryGet()
        {
            return FmodStudioGateway.TryGetServer();
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to load and retain an FMOD Studio bank from a Godot resource path.</para>
        ///     <para xml:lang="zh-CN">尝试从 Godot 资源路径加载并保留 FMOD Studio 音频库。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The existing Godot resource path of the bank file.</para>
        ///     <para xml:lang="zh-CN">现有音频库文件的 Godot 资源路径。</para>
        /// </param>
        /// <param name="mode">
        ///     <para xml:lang="en">The bank-loading mode passed to <c>FmodServer.load_bank</c>.</para>
        ///     <para xml:lang="zh-CN">传递给 <c>FmodServer.load_bank</c> 的音频库加载模式。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when loading returns a valid retained bank object or a compatible
        ///         boolean success result; otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">加载返回可保留的有效音频库对象或兼容的布尔成功结果时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryLoadBank(string resourcePath, FmodStudioLoadBankMode mode = FmodStudioLoadBankMode.Normal)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                RitsuLibFramework.Logger.Warn("[Audio] FMOD load_bank: empty path.");
                return false;
            }

            if (!FileAccess.FileExists(resourcePath))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD load_bank: file not found: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
                return false;
            }

            if (!FmodStudioGateway.TryCall(out var result, FmodStudioMethodNames.LoadBank, resourcePath, (int)mode))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD load_bank call failed: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
                return false;
            }

            switch (result.VariantType)
            {
                case Variant.Type.Bool:
                    if (result.AsBool())
                        return true;

                    RitsuLibFramework.Logger.Warn(
                        $"[Audio] FMOD load_bank returned false: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
                    return false;
                case Variant.Type.Nil:
                    RitsuLibFramework.Logger.Warn(
                        $"[Audio] FMOD load_bank returned nil: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
                    return false;
                case Variant.Type.Object:
                {
                    var bank = result.AsGodotObject();
                    if (bank is null || !GodotObject.IsInstanceValid(bank))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Audio] FMOD load_bank returned invalid {result.VariantType}: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
                        return false;
                    }

                    lock (LoadedBankPinsGate)
                    {
                        LoadedBankPins[resourcePath] = bank;
                    }

                    return true;
                }
                default:
                    RitsuLibFramework.Logger.Warn(
                        $"[Audio] FMOD load_bank returned unsupported {result.VariantType}: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
                    return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Logs Godot-side file, resource, hash, header, and globalized-path facts for a bank path.</para>
        ///     <para xml:lang="zh-CN">记录音频库路径在 Godot 侧的文件、资源、哈希、文件头和全局化路径信息。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The bank resource path to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的音频库资源路径。</para>
        /// </param>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Native <c>FMOD_RESULT</c> values are unavailable through this managed wrapper and appear only
        ///         when the GDExtension itself logs them.
        ///     </para>
        ///     <para xml:lang="zh-CN">此托管包装无法取得原生 <c>FMOD_RESULT</c>；只有 GDExtension 自身记录时才能看到这些结果。</para>
        /// </remarks>
        public static void LogBankResourceDiagnostics(string resourcePath)
        {
            RitsuLibFramework.Logger.Info(
                $"[Audio] FMOD bank resource diagnostics: {resourcePath}; {DescribeResourceForDiagnostics(resourcePath)}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Releases this class's retained bank reference, or invokes <c>unload_bank</c> when a runtime
        ///         provides that method and no reference is retained.
        ///     </para>
        ///     <para xml:lang="zh-CN">释放此类保留的音频库引用；如果没有保留引用且运行时提供 <c>unload_bank</c>，则调用该方法。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The exact bank resource path used for loading.</para>
        ///     <para xml:lang="zh-CN">加载时使用的准确音频库资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when a retained reference is removed or the optional runtime method
        ///         completes; otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">成功移除保留引用或可选运行时方法调用完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryUnloadBank(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return false;

            bool hadPin;
            lock (LoadedBankPinsGate)
            {
                hadPin = LoadedBankPins.Remove(resourcePath);
            }

            return hadPin || FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadBank, resourcePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to block until pending non-blocking bank loads finish and the add-on cache is updated.</para>
        ///     <para xml:lang="zh-CN">尝试阻塞，直至待处理的非阻塞音频库加载完成并更新插件缓存。</para>
        /// </summary>
        public static void TryWaitForAllLoads()
        {
            FmodStudioGateway.TryCall(FmodStudioMethodNames.WaitForAllLoads);
        }

        /// <summary>
        ///     <para xml:lang="en">Queries whether the FMOD add-on still has pending bank loads.</para>
        ///     <para xml:lang="zh-CN">查询 FMOD 插件是否仍有待完成的音频库加载。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> or <see langword="false" /> for a valid boolean result; null when the
        ///         method is unavailable, invocation fails, or another Variant type is returned.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         取得有效布尔结果时为 <see langword="true" /> 或 <see langword="false" />；方法不可用、调用失败或返回其他 Variant 类型时为
        ///         <see langword="null" />。
        ///     </para>
        /// </returns>
        public static bool? TryBanksStillLoading()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.BanksStillLoading))
                return null;

            return v.VariantType == Variant.Type.Bool ? v.AsBool() : null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Validates and applies a <c>GUIDs.txt</c>-style mapping file, then logs the resulting total
        ///         event-path mapping count.
        ///     </para>
        ///     <para xml:lang="zh-CN">验证并应用 <c>GUIDs.txt</c> 格式的映射文件，然后记录处理后的事件路径映射总数。</para>
        /// </summary>
        /// <param name="guidMapResourcePath">
        ///     <para xml:lang="en">
        ///         The Godot resource path of the text file, whose relevant lines use <c>{guid} event:/…</c>. Bank
        ///         and bus lines are ignored by the managed fallback table.
        ///     </para>
        ///     <para xml:lang="zh-CN">文本文件的 Godot 资源路径；相关行使用 <c>{guid} event:/…</c> 格式。托管回退表会忽略音频库和总线行。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when at least one event mapping from this file is parsed or an optional
        ///         native injection method succeeds; otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">成功解析此文件中的至少一个事件映射，或可选原生注入方法成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryLoadStudioGuidMappings(string guidMapResourcePath)
        {
            if (string.IsNullOrWhiteSpace(guidMapResourcePath))
            {
                RitsuLibFramework.Logger.Warn("[Audio] FMOD guid map: empty path.");
                return false;
            }

            if (!FileAccess.FileExists(guidMapResourcePath))
            {
                RitsuLibFramework.Logger.Warn($"[Audio] FMOD guid map file not found: {guidMapResourcePath}");
                return false;
            }

            if (!TryApplyStudioGuidMappingsCore(guidMapResourcePath))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD guid map failed (unreadable or no usable event:/ mappings): {guidMapResourcePath}");
                return false;
            }

            var n = FmodStudioGuidPathTable.EventMappingCount;
            RitsuLibFramework.Logger.Info($"[Audio] FMOD guid map OK: {guidMapResourcePath} ({n} event path(s))");
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Parses and merges <c>event:/…</c>-to-GUID entries for RitsuLib fallbacks, then attempts any
        ///         compatible native mapping-injection method provided by the runtime.
        ///     </para>
        ///     <para xml:lang="zh-CN">解析并合并供 RitsuLib 回退逻辑使用的 <c>event:/…</c> 到 GUID 条目，然后尝试运行时提供的兼容原生映射注入方法。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The Godot resource path of the <c>GUIDs.txt</c>-style text file.</para>
        ///     <para xml:lang="zh-CN"><c>GUIDs.txt</c> 格式文本文件的 Godot 资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when at least one event mapping from this file is parsed or native
        ///         injection succeeds; otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">成功解析此文件中的至少一个事件映射，或原生注入成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Prefer <see cref="TryLoadStudioGuidMappings" /> when explicit existence validation and
        ///         success-count logging are desired.
        ///     </para>
        ///     <para xml:lang="zh-CN">需要显式验证文件存在并记录成功映射数量时，优先使用 <see cref="TryLoadStudioGuidMappings" />。</para>
        /// </remarks>
        public static bool TryInjectStudioGuidMappings(string resourcePath)
        {
            if (TryApplyStudioGuidMappingsCore(resourcePath)) return true;
            RitsuLibFramework.Logger.Warn($"[Audio] FMOD guid map could not be applied: {resourcePath}");
            return false;
        }

        private static bool TryApplyStudioGuidMappingsCore(string resourcePath)
        {
            if (!FmodStudioGuidPathTable.TryLoadFromResourceFile(resourcePath, out var parsedEventMappings))
                return false;

            var injected = TryCallNativeGuidInject(resourcePath);
            WarnIfMappedEventGuidsUnresolved();
            return injected || parsedEventMappings > 0;
        }

        private static void WarnIfMappedEventGuidsUnresolved()
        {
            foreach (var (path, guid) in FmodStudioGuidPathTable.SnapshotEventMappings())
            {
                if (TryCheckEventGuid(guid) != false)
                    continue;

                RitsuLibFramework.Logger.Warn(
                    "[Audio] guids.txt: GUID not found in loaded FMOD Studio data — " +
                    $"event '{path}', GUID '{guid}'. Load matching banks before injection and regenerate GUIDs.txt from the same build.");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Checks whether an event path is registered in the managed GUID table or present in the add-on's
        ///         loaded cache.
        ///     </para>
        ///     <para xml:lang="zh-CN">检查事件路径是否已注册到托管 GUID 表，或存在于插件已加载的缓存中。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The event path to check.</para>
        ///     <para xml:lang="zh-CN">要检查的事件路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> for any registered mapping or a positive native probe,
        ///         <see langword="false" /> for a blank path or negative native probe, and null when the native probe is
        ///         unavailable or invalid.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         存在任意注册映射或原生探测为肯定结果时为 <see langword="true" />；路径为空白或原生探测为否定结果时为 <see langword="false" />
        ///         ；原生探测不可用或无效时为 <see langword="null" />。
        ///     </para>
        /// </returns>
        public static bool? TryCheckEventPath(string eventPath)
        {
            if (string.IsNullOrWhiteSpace(eventPath))
                return false;

            if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventPath, out _))
                return true;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventPath, eventPath))
                return null;

            return v.VariantType == Variant.Type.Bool ? v.AsBool() : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Checks whether a bus path is present in the add-on's loaded cache.</para>
        ///     <para xml:lang="zh-CN">检查总线路径是否存在于插件已加载的缓存中。</para>
        /// </summary>
        /// <param name="busPath">
        ///     <para xml:lang="en">The bus path to check.</para>
        ///     <para xml:lang="zh-CN">要检查的总线路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> or <see langword="false" /> for a valid probe, <see langword="false" />
        ///         for a blank path, and null when invocation is unavailable or returns an invalid type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         有效探测结果为 <see langword="true" /> 或 <see langword="false" />；路径为空白时为 <see langword="false" />
        ///         ；调用不可用或返回无效类型时为 <see langword="null" />。
        ///     </para>
        /// </returns>
        public static bool? TryCheckBusPath(string busPath)
        {
            if (string.IsNullOrWhiteSpace(busPath))
                return false;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckBusPath, busPath))
                return null;

            return v.VariantType == Variant.Type.Bool ? v.AsBool() : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to resolve a valid FMOD Studio event description from a GUID.</para>
        ///     <para xml:lang="zh-CN">尝试根据 GUID 解析有效的 FMOD Studio 事件描述。</para>
        /// </summary>
        /// <param name="eventGuid">
        ///     <para xml:lang="en">The event GUID to normalize and resolve.</para>
        ///     <para xml:lang="zh-CN">要规范化并解析的事件 GUID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The valid description object, or null when the GUID is blank, malformed, absent, or cannot be
        ///         resolved.
        ///     </para>
        ///     <para xml:lang="zh-CN">有效的描述对象；GUID 为空白、格式错误、不存在或无法解析时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryGetEventDescriptionFromGuid(string eventGuid)
        {
            if (string.IsNullOrWhiteSpace(eventGuid))
                return null;

            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return null;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetEventFromGuid, normalized))
                return null;

            if (v.VariantType != Variant.Type.Object)
                return null;

            var description = v.AsGodotObject();
            return description is not null && GodotObject.IsInstanceValid(description) ? description : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Checks whether an event GUID resolves in the add-on's loaded Studio cache.</para>
        ///     <para xml:lang="zh-CN">检查事件 GUID 是否能在插件已加载的 Studio 缓存中解析。</para>
        /// </summary>
        /// <param name="eventGuid">
        ///     <para xml:lang="en">The event GUID to normalize and check.</para>
        ///     <para xml:lang="zh-CN">要规范化并检查的事件 GUID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> or <see langword="false" /> for a valid probe; null when normalization
        ///         or invocation fails or an invalid result type is returned.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         有效探测结果为 <see langword="true" /> 或 <see langword="false" />；规范化或调用失败，或返回无效结果类型时为
        ///         <see langword="null" />。
        ///     </para>
        /// </returns>
        public static bool? TryCheckEventGuid(string eventGuid)
        {
            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return null;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventGuid, normalized))
                return null;

            return v.VariantType == Variant.Type.Bool ? v.AsBool() : null;
        }

        /// <summary>
        ///     <para xml:lang="en">Retrieves the FMOD Studio buses currently returned by the add-on.</para>
        ///     <para xml:lang="zh-CN">获取插件当前返回的 FMOD Studio 总线。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The returned Godot array, or a new empty array when unavailable or of an unexpected type.</para>
        ///     <para xml:lang="zh-CN">插件返回的 Godot 数组；不可用或类型不符合预期时返回新的空数组。</para>
        /// </returns>
        public static Array TryGetAllBuses()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllBuses))
                return new();

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray() : new();
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of Studio banks currently returned by <c>FmodServer.get_all_banks</c>.</para>
        ///     <para xml:lang="zh-CN">获取 <c>FmodServer.get_all_banks</c> 当前返回的 Studio 音频库数量。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The array count, or <c>-1</c> when the method is unavailable, invocation fails, or the result
        ///         is not an array.
        ///     </para>
        ///     <para xml:lang="zh-CN">数组条目数；方法不可用、调用失败或结果不是数组时为 <c>-1</c>。</para>
        /// </returns>
        public static int TryGetLoadedBankCount()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllBanks))
                return -1;

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray().Count : -1;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of event descriptions currently reported by the add-on's Studio cache.</para>
        ///     <para xml:lang="zh-CN">获取插件 Studio 缓存当前报告的事件描述数量。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The array count, or <c>-1</c> when the method is unavailable, invocation fails, or the result
        ///         is not an array.
        ///     </para>
        ///     <para xml:lang="zh-CN">数组条目数；方法不可用、调用失败或结果不是数组时为 <c>-1</c>。</para>
        /// </returns>
        public static int TryGetLoadedEventDescriptionCount()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllEventDescriptions))
                return -1;

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray().Count : -1;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the event-description count reported by the loaded bank whose Godot resource path matches
        ///         exactly.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取 Godot 资源路径准确匹配的已加载音频库所报告的事件描述数量。</para>
        /// </summary>
        /// <param name="bankResourcePath">
        ///     <para xml:lang="en">The exact value returned by the bank's <c>get_godot_res_path</c> method.</para>
        ///     <para xml:lang="zh-CN">音频库 <c>get_godot_res_path</c> 方法返回的准确值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The reported 64-bit count, or <c>-1</c> for a blank path, unavailable enumeration, no matching
        ///         bank, missing methods, or invocation failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">报告的 64 位数量；路径为空白、无法枚举、没有匹配音频库、缺少方法或调用失败时为 <c>-1</c>。</para>
        /// </returns>
        public static long TryGetLoadedBankEventDescriptionCount(string bankResourcePath)
        {
            if (string.IsNullOrWhiteSpace(bankResourcePath))
                return -1;

            if (!FmodStudioGateway.TryCall(out var banksVar, FmodStudioMethodNames.GetAllBanks))
                return -1;

            if (banksVar.VariantType != Variant.Type.Array)
                return -1;

            foreach (var item in banksVar.AsGodotArray())
            {
                if (item.VariantType != Variant.Type.Object)
                    continue;

                var bank = item.AsGodotObject();
                if (bank is null || !GodotObject.IsInstanceValid(bank) ||
                    !bank.HasMethod(BankGetGodotResourcePath))
                    continue;

                string path;
                try
                {
                    path = bank.Call(BankGetGodotResourcePath).AsString();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[Audio] FMOD bank resource-path inspection: {ex}");
                    continue;
                }

                if (!string.Equals(path, bankResourcePath, StringComparison.Ordinal))
                    continue;

                if (!bank.HasMethod(BankGetEventDescriptionCount))
                    return -1;

                try
                {
                    return bank.Call(BankGetEventDescriptionCount).AsInt64();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[Audio] FMOD bank event-count inspection: {ex}");
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Logs up to forty event paths reported by an already-loaded bank's <c>get_description_list</c>
        ///         result.
        ///     </para>
        ///     <para xml:lang="zh-CN">记录已加载音频库通过 <c>get_description_list</c> 报告的最多四十个事件路径。</para>
        /// </summary>
        /// <param name="bankResourcePath">
        ///     <para xml:lang="en">The exact Godot resource path of the loaded bank; blank paths are ignored.</para>
        ///     <para xml:lang="zh-CN">已加载音频库的准确 Godot 资源路径；空白路径会被忽略。</para>
        /// </param>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Unreadable or missing banks and banks with no events produce warnings. The method does not
        ///         report global cache totals.
        ///     </para>
        ///     <para xml:lang="zh-CN">无法读取、未找到或不含事件的音频库会产生警告。此方法不报告全局缓存总数。</para>
        /// </remarks>
        public static void TryLogLoadedStudioBankEvents(string bankResourcePath)
        {
            if (string.IsNullOrWhiteSpace(bankResourcePath))
                return;

            var paths = TryCollectLoadedBankEventPaths(bankResourcePath);
            if (paths is null)
            {
                RitsuLibFramework.Logger.Warn($"[Audio] FMOD bank not loaded or unreadable: {bankResourcePath}");
                return;
            }

            if (paths.Count == 0)
            {
                RitsuLibFramework.Logger.Warn(
                    "[Audio] FMOD bank has no events — rebuild banks from FMOD Studio or verify the exported .bank.");
                return;
            }

            const int maxListed = 40;
            var sb = new StringBuilder(256);
            var n = Math.Min(paths.Count, maxListed);
            for (var i = 0; i < n; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(paths[i]);
            }

            if (paths.Count > maxListed)
                sb.Append(" … (+").Append(paths.Count - maxListed).Append(" more)");

            RitsuLibFramework.Logger.Info(
                $"[Audio] FMOD bank {bankResourcePath} ({paths.Count} event{(paths.Count == 1 ? "" : "s")}): {sb}");
        }

        private static List<string>? TryCollectLoadedBankEventPaths(string bankResourcePath)
        {
            if (!FmodStudioGateway.TryCall(out var banksVar, FmodStudioMethodNames.GetAllBanks) ||
                banksVar.VariantType != Variant.Type.Array)
                return null;

            foreach (var item in banksVar.AsGodotArray())
            {
                if (item.VariantType != Variant.Type.Object)
                    continue;

                var bank = item.AsGodotObject();
                if (bank is null || !GodotObject.IsInstanceValid(bank) ||
                    !bank.HasMethod(BankGetGodotResourcePath))
                    continue;

                string resPath;
                try
                {
                    resPath = bank.Call(BankGetGodotResourcePath).AsString();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[Audio] FMOD bank resource-path enumeration: {ex}");
                    continue;
                }

                if (!string.Equals(resPath, bankResourcePath, StringComparison.Ordinal))
                    continue;

                if (!bank.HasMethod(BankGetDescriptionList))
                    return null;

                var paths = new List<string>();
                try
                {
                    var listVar = bank.Call(BankGetDescriptionList);
                    if (listVar.VariantType != Variant.Type.Array)
                        return null;

                    foreach (var descriptionValue in listVar.AsGodotArray())
                    {
                        if (descriptionValue.VariantType != Variant.Type.Object)
                            return null;

                        var description = descriptionValue.AsGodotObject();
                        if (description is null || !GodotObject.IsInstanceValid(description) ||
                            !description.HasMethod(EventDescriptionGetPath))
                            return null;

                        paths.Add(description.Call(EventDescriptionGetPath).AsString());
                    }
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[Audio] FMOD bank event-path enumeration: {ex}");
                    return null;
                }

                return paths;
            }

            return null;
        }

        private static bool TryCallNativeGuidInject(string resourcePath)
        {
            var server = FmodStudioGateway.TryGetServer();
            if (server is null)
                return false;

            foreach (var method in GuidMappingInjectCandidates)
            {
                if (!server.HasMethod(method))
                    continue;

                try
                {
                    var r = server.Call(method, resourcePath);
                    if (r.VariantType == Variant.Type.Bool && !r.AsBool())
                        continue;

                    return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD guid inject {method}: {ex}");
                }
            }

            return false;
        }

        private static string DescribeResourceForDiagnostics(string resourcePath)
        {
            var parts = new List<string>
            {
                $"fileExists={FileAccess.FileExists(resourcePath)}",
                $"resourceExists={ResourceLoader.Exists(resourcePath)}",
            };

            try
            {
                var bytes = FileAccess.GetFileAsBytes(resourcePath);
                parts.Add($"bytes={bytes.Length}");
                if (bytes.Length > 0)
                {
                    parts.Add($"sha256={Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}");
                    parts.Add($"head={DescribeHead(bytes)}");
                }
            }
            catch (Exception ex)
            {
                parts.Add($"readError={ex}");
            }

            try
            {
                var resource = ResourceLoader.Load<Resource>(resourcePath);
                parts.Add(resource is null
                    ? "resourceType=<null>"
                    : $"resourceType={resource.GetClass()}; resourcePath={resource.ResourcePath}");
            }
            catch (Exception ex)
            {
                parts.Add($"resourceLoadError={ex}");
            }

            try
            {
                var globalized = ProjectSettings.GlobalizePath(resourcePath);
                if (!string.IsNullOrWhiteSpace(globalized) &&
                    !string.Equals(globalized, resourcePath, StringComparison.Ordinal))
                    parts.Add($"globalized={globalized}");
            }
            catch (Exception ex)
            {
                parts.Add($"globalizeError={ex}");
            }

            parts.Add("nativeResult=unavailable-from-managed-wrapper");
            return string.Join("; ", parts);
        }

        private static string DescribeHead(byte[] bytes)
        {
            var n = Math.Min(bytes.Length, 16);
            var hex = Convert.ToHexString(bytes, 0, n).ToLowerInvariant();
            var ascii = new char[n];
            for (var i = 0; i < n; i++)
                ascii[i] = bytes[i] is >= 32 and <= 126 ? (char)bytes[i] : '.';

            return $"{hex}/{new string(ascii)}";
        }
    }
}
