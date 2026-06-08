using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Audio.Internal;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Audio.Patches
{
    /// <summary>
    ///     Container for Harmony prefixes on <see cref="NAudioManager" />: guids.txt-only <c>event:/…</c> paths (mod banks
    ///     without strings.bank). Mirrors <c>audio_manager_proxy.gd</c> loop/music queues and routing through the same
    ///     <see cref="NAudioManager" /> entry points as vanilla.
    ///     <see cref="NAudioManager" /> 上 Harmony prefix 的容器：仅存在于 guids.txt 的 <c>event:/…</c> 路径（mod bank
    ///     没有 strings.bank）。复现 <c>audio_manager_proxy.gd</c> 的 loop/music 队列，并通过与原版相同的
    ///     <see cref="NAudioManager" /> 入口点路由。
    /// </summary>
    internal static class NAudioManagerGuidMappedStudioEventsPatches
    {
        private static readonly Lock MissingStudioPathWarningGate = new();
        private static readonly HashSet<string> MissingStudioPathWarningLoggedKeys = new(StringComparer.Ordinal);

        private static void LogMissingStudioPathOnce(string operation, string path)
        {
            if (!path.StartsWith("event:/", StringComparison.Ordinal))
                return;

            var pathExistsInRuntime = FmodStudioServer.TryCheckEventPath(path);
            if (pathExistsInRuntime != false)
                return;

            var key = operation + "\0" + path;
            lock (MissingStudioPathWarningGate)
            {
                if (!MissingStudioPathWarningLoggedKeys.Add(key))
                    return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[Audio] FMOD event was not found in GUID mappings or loaded Studio events. " +
                $"operation={operation}; " +
                $"path={path}; guidMapEventCount={FmodStudioGuidPathTable.EventMappingCount}; " +
                $"loadedBankCount={FmodStudioServer.TryGetLoadedBankCount()}; " +
                $"loadedEventDescriptionCount={FmodStudioServer.TryGetLoadedEventDescriptionCount()}; " +
                $"banksStillLoading={FmodStudioServer.TryBanksStillLoading()?.ToString() ?? "?"}");
        }

        /// <summary>
        ///     Intercepts mapped <see cref="NAudioManager.PlayOneShot(string, Dictionary{string, float}, float)" /> calls.
        ///     拦截已映射的 <see cref="NAudioManager.PlayOneShot(string, Dictionary{string, float}, float)" /> 调用。
        /// </summary>
        internal sealed class PlayOneShot : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_play_one_shot";
            public static bool IsCritical => false;

            public static string Description =>
                "GUID-backed PlayOneShot when event path is listed in guids.txt";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NAudioManager), "PlayOneShot",
                        [typeof(string), typeof(Dictionary<string, float>), typeof(float)]),
                ];
            }

            public static bool Prefix(NAudioManager __instance, string path, Dictionary<string, float> parameters,
                float volume)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrWhiteSpace(path))
                    return false;

                if (!FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out var mappedGuid))
                {
                    LogMissingStudioPathOnce("PlayOneShot", path);
                    return true;
                }

                if (FmodStudioDirectOneShots.TryFireOneShotForMappedEventPath(path, volume, parameters))
                    return false;

                RitsuLibFramework.Logger.Warn(
                    "[Audio] Mapped FMOD one-shot failed. " +
                    FmodStudioMappedOneShotDiagnostics.BuildMappedOneShotFailureDetail(path, mappedGuid));

                return false;
            }
        }

        /// <summary>
        ///     Intercepts mapped <see cref="NAudioManager.PlayLoop(string, bool)" /> calls.
        ///     拦截已映射的 <see cref="NAudioManager.PlayLoop(string, bool)" /> 调用。
        /// </summary>
        internal sealed class PlayLoop : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_play_loop";
            public static bool IsCritical => false;

            public static string Description =>
                "GUID-backed PlayLoop when event path is listed in guids.txt";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "PlayLoop", [typeof(string), typeof(bool)])];
            }

            public static bool Prefix(NAudioManager __instance, string path, bool usesLoopParam)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrWhiteSpace(path))
                    return false;

                if (!GuidMappedNaudioStudioProxy.IsMappedPath(path))
                {
                    LogMissingStudioPathOnce("PlayLoop", path);
                    return true;
                }

                if (GuidMappedNaudioStudioProxy.TryEnqueueMappedLoop(path, usesLoopParam))
                    return false;

                if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out var g))
                    RitsuLibFramework.Logger.Warn(
                        "[Audio] Mapped PlayLoop failed. " +
                        FmodStudioMappedOneShotDiagnostics.BuildMappedOneShotFailureDetail(path, g));

                return false;
            }
        }

        /// <summary>
        ///     Intercepts <see cref="NAudioManager.StopLoop(string)" /> for paths owned by the mapped loop queue.
        ///     为已映射 loop 队列拥有的路径拦截 <see cref="NAudioManager.StopLoop(string)" />。
        /// </summary>
        internal sealed class StopLoop : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_stop_loop";
            public static bool IsCritical => false;

            public static string Description =>
                "Stop mapped PlayLoop instances keyed by guids.txt paths";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "StopLoop", [typeof(string)])];
            }

            public static bool Prefix(NAudioManager __instance, string path)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrEmpty(path) || !GuidMappedNaudioStudioProxy.IsMappedPath(path))
                    return true;

                return !GuidMappedNaudioStudioProxy.TryStopMappedLoop(path);
            }
        }

        /// <summary>
        ///     Intercepts <see cref="NAudioManager.SetParam(string, string, float)" /> for mapped loop paths.
        ///     为已映射 loop 路径拦截 <see cref="NAudioManager.SetParam(string, string, float)" />。
        /// </summary>
        internal sealed class SetParam : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_set_param";
            public static bool IsCritical => false;

            public static string Description =>
                "SetParam on first mapped loop instance when path is guids.txt-only";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NAudioManager), "SetParam",
                        [typeof(string), typeof(string), typeof(float)]),
                ];
            }

            public static bool Prefix(NAudioManager __instance, string path, string param, float value)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrEmpty(path) || !GuidMappedNaudioStudioProxy.IsMappedPath(path))
                    return true;

                return !GuidMappedNaudioStudioProxy.TrySetParamOnFirstMappedLoop(path, param, value);
            }
        }

        /// <summary>
        ///     Clears mapped loop state when <see cref="NAudioManager.StopAllLoops" /> runs.
        ///     <see cref="NAudioManager.StopAllLoops" /> 运行时清除已映射 loop 状态。
        /// </summary>
        internal sealed class StopAllLoops : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_stop_all_loops";
            public static bool IsCritical => false;

            public static string Description =>
                "Clears mapped loop queues when StopAllLoops runs";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "StopAllLoops")];
            }

            public static void Prefix(NAudioManager __instance)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return;

                GuidMappedNaudioStudioProxy.StopAllMappedLoops();
            }
        }

        /// <summary>
        ///     Intercepts mapped <see cref="NAudioManager.PlayMusic(string)" /> calls.
        ///     拦截已映射的 <see cref="NAudioManager.PlayMusic(string)" /> 调用。
        /// </summary>
        internal sealed class PlayMusic : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_play_music";
            public static bool IsCritical => false;

            public static string Description =>
                "GUID-backed PlayMusic when event path is listed in guids.txt";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "PlayMusic", [typeof(string)])];
            }

            public static bool Prefix(NAudioManager __instance, string music)
            {
                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrWhiteSpace(music))
                    return false;

                if (!GuidMappedNaudioStudioProxy.IsMappedPath(music))
                {
                    LogMissingStudioPathOnce("PlayMusic", music);
                    return true;
                }

                __instance.StopMusic();

                if (GuidMappedNaudioStudioProxy.TryStartMappedMusic(music)) return false;
                if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(music, out var g))
                    RitsuLibFramework.Logger.Warn(
                        "[Audio] Mapped PlayMusic failed. " +
                        FmodStudioMappedOneShotDiagnostics.BuildMappedOneShotFailureDetail(music, g));

                return false;
            }
        }

        /// <summary>
        ///     Releases the mapped music instance in parallel with vanilla <see cref="NAudioManager.StopMusic" />.
        ///     与原版 <see cref="NAudioManager.StopMusic" /> 并行释放已映射音乐实例。
        /// </summary>
        internal sealed class StopMusic : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_stop_music";
            public static bool IsCritical => false;

            public static string Description =>
                "Releases mapped music instance alongside vanilla StopMusic";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "StopMusic")];
            }

            public static void Prefix(NAudioManager __instance)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return;

                GuidMappedNaudioStudioProxy.ReleaseMappedMusic();
            }
        }

        /// <summary>
        ///     Routes <see cref="NAudioManager.UpdateMusicParameter(string, string)" /> to the active mapped music instance.
        ///     将 <see cref="NAudioManager.UpdateMusicParameter(string, string)" /> 路由到活动的已映射音乐实例。
        /// </summary>
        internal sealed class UpdateMusicParameter : IPatchMethod
        {
            public static string PatchId => "naudio_guid_mapped_update_music_parameter";
            public static bool IsCritical => false;

            public static string Description =>
                "Routes UpdateMusicParameter to mapped music instance when active";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NAudioManager), "UpdateMusicParameter",
                        [typeof(string), typeof(string)]),
                ];
            }

            public static bool Prefix(NAudioManager __instance, string parameter, string value)
            {
                _ = __instance;

                if (NonInteractiveMode.IsActive)
                    return true;

                return !GuidMappedNaudioStudioProxy.TryUpdateMappedMusicParameter(parameter, value);
            }
        }
    }
}
