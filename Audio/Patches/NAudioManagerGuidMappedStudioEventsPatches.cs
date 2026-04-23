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
    /// </summary>
    public static class NAudioManagerGuidMappedStudioEventsPatches
    {
        /// <summary>
        ///     Intercepts mapped <see cref="NAudioManager.PlayOneShot(string, Dictionary{string, float}, float)" /> calls.
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
