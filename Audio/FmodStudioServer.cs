using System.Text;
using Godot;
using STS2RitsuLib.Audio.Internal;
using Array = Godot.Collections.Array;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     FMOD Studio bank and path probes. For gameplay sounds that should follow vanilla mixer settings, use
    ///     <see cref="GameFmod.Studio" /> instead.
    ///     FMOD Studio bank 和路径探测。对于应跟随原版 mixer 设置的游戏音效，请改用
    ///     <see cref="GameFmod.Studio" />。
    /// </summary>
    public static class FmodStudioServer
    {
        /// <summary>
        ///     The GDExtension <c>FmodBank</c> destructor calls <c>unload_bank</c>; callers must retain the returned ref
        ///     or the bank is unloaded when the Variant goes out of scope (see <c>studio/fmod_bank.cpp</c> destructor).
        ///     GDExtension <c>FmodBank</c> 析构函数会调用 <c>unload_bank</c>；调用方必须保留返回的 ref，
        ///     否则 Variant 离开作用域时 bank 会被卸载（见 <c>studio/fmod_bank.cpp</c> 析构函数）。
        /// </summary>
        private static readonly Lock LoadedBankPinsGate = new();

        private static readonly Dictionary<string, GodotObject> LoadedBankPins = [];

        private static readonly StringName[] GuidMappingInjectCandidates =
        [
            new("register_guid_path_mappings_from_file"),
            new("inject_guid_mappings_from_file"),
            new("register_strings_from_guid_file"),
            new("load_guid_mapping_file"),
        ];

        /// <summary>
        ///     Returns the Godot <c>FmodServer</c> singleton when present.
        ///     存在时返回 Godot <c>FmodServer</c> 单例。
        /// </summary>
        public static GodotObject? TryGet()
        {
            return FmodStudioGateway.TryGetServer();
        }

        /// <summary>
        ///     Loads a bank from <paramref name="resourcePath" /> using <paramref name="mode" />.
        ///     使用 <paramref name="mode" /> 从 <paramref name="resourcePath" /> 加载 bank。
        /// </summary>
        public static bool TryLoadBank(string resourcePath, FmodStudioLoadBankMode mode = FmodStudioLoadBankMode.Normal)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                RitsuLibFramework.Logger.Warn("[Audio] FMOD load_bank: empty path.");
                return false;
            }

            if (!FileAccess.FileExists(resourcePath))
            {
                RitsuLibFramework.Logger.Warn($"[Audio] FMOD load_bank: file not found: {resourcePath}");
                return false;
            }

            if (!FmodStudioGateway.TryCall(out var result, FmodStudioMethodNames.LoadBank, resourcePath, (int)mode))
                return false;

            switch (result.VariantType)
            {
                case Variant.Type.Bool:
                    return result.AsBool();
                case Variant.Type.Nil:
                    return false;
                default:
                {
                    var bank = result.AsGodotObject();
                    if (bank is null || !GodotObject.IsInstanceValid(bank))
                        return false;

                    lock (LoadedBankPinsGate)
                    {
                        LoadedBankPins[resourcePath] = bank;
                    }

                    return true;
                }
            }
        }

        /// <summary>
        ///     Unloads a previously loaded bank (releases any pin held by <see cref="TryLoadBank" />).
        ///     卸载先前加载的 bank（释放 <see cref="TryLoadBank" /> 持有的任何 pin）。
        /// </summary>
        public static bool TryUnloadBank(string resourcePath)
        {
            bool hadPin;
            lock (LoadedBankPinsGate)
            {
                hadPin = LoadedBankPins.Remove(resourcePath);
            }

            return hadPin || FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadBank, resourcePath);
        }

        /// <summary>
        ///     Blocks until non-blocking bank loads finish (matches <c>FmodServer.wait_for_all_loads</c>).
        ///     阻塞直到非阻塞 bank 加载完成（匹配 <c>FmodServer.wait_for_all_loads</c>）。
        /// </summary>
        public static void TryWaitForAllLoads()
        {
            FmodStudioGateway.TryCall(FmodStudioMethodNames.WaitForAllLoads);
        }

        /// <summary>
        ///     Null when the query fails; otherwise whether FMOD is still loading banks (see <c>FmodServer.banks_still_loading</c>
        ///     ).
        ///     查询失败时为 null；否则表示 FMOD 是否仍在加载 bank（见 <c>FmodServer.banks_still_loading</c>
        ///     ）。
        /// </summary>
        public static bool? TryBanksStillLoading()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.BanksStillLoading))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Validates <paramref name="guidMapResourcePath" /> exists, loads guids.txt-style mappings, applies native
        ///     injection when available, and logs success (with event path count) or failure.
        ///     验证 <paramref name="guidMapResourcePath" /> 存在，加载 guids.txt 风格映射，应用可用的原生
        ///     注入，并记录成功（含事件路径数量）或失败。
        /// </summary>
        /// <param name="guidMapResourcePath">
        ///     e.g. <c>res://Mod/banks/MyMod.guids.txt</c> — lines <c>{guid} bank:/…</c>, <c>bus:/…</c>,
        ///     <c>event:/…</c>.
        ///     <c>event:/…</c>。
        ///     例如 <c>res://Mod/banks/MyMod.guids.txt</c> - 行格式为 <c>{guid} bank:/…</c>、<c>bus:/…</c>、
        ///     <c>event:/…</c>。
        ///     <c>event:/…</c>。
        /// </param>
        /// <returns>
        ///     True when mappings were applied per <see cref="TryApplyStudioGuidMappingsCore" />.
        ///     按 <see cref="TryApplyStudioGuidMappingsCore" /> 应用映射时为 true。
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
        ///     Parses an FMOD Studio <c>GUIDs.txt</c>-style listing (same shape as Celeste/Everest <c>IngestGUIDs</c> inputs),
        ///     registers <c>event:/…</c> path → GUID mappings for RitsuLib fallbacks, and attempts optional
        ///     <c>FmodServer</c> hooks when the runtime exposes them. Prefer <see cref="TryLoadStudioGuidMappings" /> for
        ///     existence checks and outcome logging.
        ///     解析 FMOD Studio <c>GUIDs.txt</c> 风格列表（与 Celeste/Everest <c>IngestGUIDs</c> 输入形状相同），
        ///     为 RitsuLib 后备逻辑注册 <c>event:/…</c> 路径到 GUID 的映射，并在运行时暴露相关接口时尝试可选的
        ///     <c>FmodServer</c> hook。优先使用 <see cref="TryLoadStudioGuidMappings" /> 进行存在性检查和结果记录。
        /// </summary>
        /// <param name="resourcePath">
        ///     Project path to the text file (e.g. <c>res://Mod/banks/MyMod.guids.txt</c>). Each non-empty line:
        ///     <c>{guid} bank:/…</c>, <c>{guid} bus:/…</c>, or <c>{guid} event:/…</c>.
        ///     文本文件的项目路径（例如 <c>res://Mod/banks/MyMod.guids.txt</c>）。每个非空行：
        ///     <c>{guid} bank:/…</c>、<c>{guid} bus:/…</c> 或 <c>{guid} event:/…</c>。
        /// </param>
        /// <returns>
        ///     False when the file is missing or unparsable; otherwise true when at least one <c>event:/</c> mapping was
        ///     loaded and/or an addon injection call succeeded.
        ///     文件缺失或无法解析时为 false；否则当至少加载一个 <c>event:/</c> 映射和/或 addon 注入调用
        ///     成功时为 true。
        /// </returns>
        public static bool TryInjectStudioGuidMappings(string resourcePath)
        {
            if (TryApplyStudioGuidMappingsCore(resourcePath)) return true;
            RitsuLibFramework.Logger.Warn($"[Audio] FMOD guid map: missing or unreadable file: {resourcePath}");
            return false;
        }

        private static bool TryApplyStudioGuidMappingsCore(string resourcePath)
        {
            if (!FmodStudioGuidPathTable.TryLoadFromResourceFile(resourcePath))
                return false;

            var injected = TryCallNativeGuidInject(resourcePath);
            WarnIfMappedEventGuidsUnresolved();
            return injected || FmodStudioGuidPathTable.EventMappingCount > 0;
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
        ///     Null when the probe fails; otherwise whether the event path exists in loaded data.
        ///     探测失败时为 null；否则表示事件路径是否存在于已加载数据中。
        /// </summary>
        public static bool? TryCheckEventPath(string eventPath)
        {
            if (string.IsNullOrWhiteSpace(eventPath))
                return false;

            if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventPath, out _))
                return true;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventPath, eventPath))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Null when the probe fails; otherwise whether the bus path is valid.
        ///     探测失败时为 null；否则表示 bus 路径是否有效。
        /// </summary>
        public static bool? TryCheckBusPath(string busPath)
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckBusPath, busPath))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Resolves a Studio event description from a GUID; null when missing or on failure.
        ///     从 GUID 解析 Studio 事件描述；缺失或失败时为 null。
        /// </summary>
        public static GodotObject? TryGetEventDescriptionFromGuid(string eventGuid)
        {
            if (string.IsNullOrWhiteSpace(eventGuid))
                return null;

            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetEventFromGuid, normalized)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Null when the probe fails; otherwise whether the GUID resolves in the loaded Studio cache.
        ///     探测失败时为 null；否则表示该 GUID 是否能在已加载的 Studio cache 中解析。
        /// </summary>
        public static bool? TryCheckEventGuid(string eventGuid)
        {
            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
                return null;

            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventGuid, normalized))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Returns all buses currently exposed by FMOD (empty when unavailable).
        ///     返回 FMOD 当前暴露的所有 bus（不可用时为空）。
        /// </summary>
        public static Array TryGetAllBuses()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllBuses))
                return new();

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray() : new();
        }

        /// <summary>
        ///     Count of loaded Studio banks; <c>-1</c> when <c>FmodServer.get_all_banks</c> is unavailable or fails.
        ///     已加载 Studio bank 的数量；<c>FmodServer.get_all_banks</c> 不可用或失败时为 <c>-1</c>。
        /// </summary>
        public static int TryGetLoadedBankCount()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllBanks))
                return -1;

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray().Count : -1;
        }

        /// <summary>
        ///     Count of event descriptions in the Studio cache; <c>-1</c> when
        ///     <c>FmodServer.get_all_event_descriptions</c> is unavailable or fails.
        ///     Studio cache 中事件描述的数量；
        ///     <c>FmodServer.get_all_event_descriptions</c> 不可用或失败时为 <c>-1</c>。
        /// </summary>
        public static int TryGetLoadedEventDescriptionCount()
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetAllEventDescriptions))
                return -1;

            return v.VariantType == Variant.Type.Array ? v.AsGodotArray().Count : -1;
        }

        /// <summary>
        ///     <c>FmodBank.get_event_description_count</c> for the bank loaded from <paramref name="bankResourcePath" />
        ///     (must match <c>get_godot_res_path</c> on the bank); <c>-1</c> when not found or on failure.
        ///     对从 <paramref name="bankResourcePath" /> 加载的 bank 调用 <c>FmodBank.get_event_description_count</c>
        ///     （必须匹配 bank 上的 <c>get_godot_res_path</c>）；未找到或失败时为 <c>-1</c>。
        /// </summary>
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
                var bank = item.AsGodotObject();
                if (bank is null)
                    continue;

                string path;
                try
                {
                    path = bank.Call("get_godot_res_path").AsString();
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(path, bankResourcePath, StringComparison.Ordinal))
                    continue;

                try
                {
                    return bank.Call("get_event_description_count").AsInt64();
                }
                catch
                {
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Logs Studio <c>event:/…</c> paths reported by <c>FmodBank.get_description_list</c> for an already-loaded
        ///     bank (single framework info line). Does not log global cache totals.
        ///     记录已加载 bank 由 <c>FmodBank.get_description_list</c> 报告的 Studio <c>event:/…</c> 路径
        ///     （单条框架 info 日志）。不记录全局 cache 总数。
        /// </summary>
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
                var bank = item.AsGodotObject();
                if (bank is null)
                    continue;

                string resPath;
                try
                {
                    resPath = bank.Call("get_godot_res_path").AsString();
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(resPath, bankResourcePath, StringComparison.Ordinal))
                    continue;

                var paths = new List<string>();
                try
                {
                    var listVar = bank.Call("get_description_list");
                    if (listVar.VariantType != Variant.Type.Array)
                        return paths;

                    paths.AddRange(listVar.AsGodotArray().Select(d => d.AsGodotObject())
                        .Select(desc => desc.Call("get_path").AsString()));
                }
                catch
                {
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
                    RitsuLibFramework.Logger.Error($"[Audio] FMOD guid inject {method}: {ex.Message}");
                }
            }

            return false;
        }
    }
}
