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
    public static class NAudioManagerGuidMappedStudioEventsPatches
    {
        /// <summary>
        ///     Intercepts mapped <see cref="NAudioManager.PlayOneShot(string, Dictionary{string, float}, float)" /> calls.
        ///     拦截已映射的 <see cref="NAudioManager.PlayOneShot(string, Dictionary{string, float}, float)" /> 调用。
        /// </summary>
        public sealed class PlayOneShot : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_play_one_shot";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "GUID-backed PlayOneShot when event path is listed in guids.txt";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NAudioManager), "PlayOneShot",
                        [typeof(string), typeof(Dictionary<string, float>), typeof(float)]),
                ];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; returns false to skip vanilla after handling mapped paths (or skip with failure diagnostics).
            ///     Harmony prefix；处理已映射路径后返回 false 跳过原版逻辑（或在失败诊断后跳过）。
            /// </summary>
            public static bool Prefix(NAudioManager __instance, string path, Dictionary<string, float> parameters,
                float volume)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrEmpty(path) ||
                    !FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out var mappedGuid))
                    return true;

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
        public sealed class PlayLoop : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_play_loop";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "GUID-backed PlayLoop when event path is listed in guids.txt";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "PlayLoop", [typeof(string), typeof(bool)])];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; skips vanilla when a mapped loop queue entry was created.
            ///     Harmony prefix；创建已映射 loop 队列条目时跳过原版逻辑。
            /// </summary>
            public static bool Prefix(NAudioManager __instance, string path, bool usesLoopParam)
            {
                _ = __instance;

                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrEmpty(path) || !GuidMappedNaudioStudioProxy.IsMappedPath(path))
                    return true;

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
        public sealed class StopLoop : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_stop_loop";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Stop mapped PlayLoop instances keyed by guids.txt paths";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "StopLoop", [typeof(string)])];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; returns false when the mapped queue handled the stop (vanilla proxy had no entry).
            ///     Harmony prefix；当已映射队列处理了停止（原版 proxy 没有条目）时返回 false。
            /// </summary>
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
        public sealed class SetParam : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_set_param";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "SetParam on first mapped loop instance when path is guids.txt-only";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NAudioManager), "SetParam",
                        [typeof(string), typeof(string), typeof(float)]),
                ];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; returns false when the mapped first loop instance received the parameter.
            ///     Harmony prefix；当已映射的第一个 loop 实例接收了参数时返回 false。
            /// </summary>
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
        public sealed class StopAllLoops : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_stop_all_loops";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Clears mapped loop queues when StopAllLoops runs";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "StopAllLoops")];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; runs before vanilla and clears parallel mapped queues.
            ///     Harmony prefix；在原版逻辑之前运行，并清除并行的已映射队列。
            /// </summary>
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
        public sealed class PlayMusic : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_play_music";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "GUID-backed PlayMusic when event path is listed in guids.txt";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "PlayMusic", [typeof(string)])];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; stops previous music then starts a mapped Studio instance (mirrors proxy ordering).
            ///     Harmony prefix；停止先前音乐，然后启动已映射的 Studio 实例（复现 proxy 顺序）。
            /// </summary>
            public static bool Prefix(NAudioManager __instance, string music)
            {
                if (TestMode.IsOn)
                    return true;

                if (string.IsNullOrEmpty(music) || !GuidMappedNaudioStudioProxy.IsMappedPath(music))
                    return true;

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
        public sealed class StopMusic : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_stop_music";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Releases mapped music instance alongside vanilla StopMusic";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NAudioManager), "StopMusic")];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; tears down mapped music before the proxy stops vanilla music.
            ///     Harmony prefix；在 proxy 停止原版音乐之前拆除已映射音乐。
            /// </summary>
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
        public sealed class UpdateMusicParameter : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "naudio_guid_mapped_update_music_parameter";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Routes UpdateMusicParameter to mapped music instance when active";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NAudioManager), "UpdateMusicParameter",
                        [typeof(string), typeof(string)]),
                ];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Harmony prefix; returns false when parameters were applied to mapped music (skip vanilla proxy call).
            ///     Harmony prefix；当参数已应用到已映射音乐时返回 false（跳过原版 proxy 调用）。
            /// </summary>
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
