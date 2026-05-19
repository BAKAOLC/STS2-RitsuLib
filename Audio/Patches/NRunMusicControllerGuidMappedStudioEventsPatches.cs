using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Audio.Internal;
using STS2RitsuLib.Patching.Models;
using GdArray = Godot.Collections.Array;

namespace STS2RitsuLib.Audio.Patches
{
    /// <summary>
    ///     Harmony patches for run-scoped music and ambience paths that bypass <see cref="NAudioManager" />.
    ///     处理绕过 <see cref="NAudioManager" /> 的 run 级音乐和 ambience 路径的 Harmony patch。
    /// </summary>
    public static class NRunMusicControllerGuidMappedStudioEventsPatches
    {
        private static readonly StringName StopMusicMethod = new("stop_music");
        private static readonly StringName StopAmbienceMethod = new("stop_ambience");
        private static readonly StringName SetGlobalParameterMethod = new("update_global_parameter");
        private static readonly StringName LoadActBanksMethod = new("load_act_banks");

        private static bool ShouldUseVanilla()
        {
            return NonInteractiveMode.IsActive || TestMode.IsOn;
        }

        private static void StopVanillaRunMusic(Node? proxy)
        {
            TryCall(proxy, StopMusicMethod);
        }

        private static void StopVanillaRunAmbience(Node? proxy)
        {
            TryCall(proxy, StopAmbienceMethod);
        }

        private static void TryCall(Node? proxy, StringName method, params Variant[] args)
        {
            if (proxy is null)
                return;

            try
            {
                proxy.Call(method, args);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] run music proxy {method}: {ex.Message}");
            }
        }

        private static void WarnMappedFailure(string operation, string path)
        {
            if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out var guid))
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] Mapped {operation} failed. " +
                    FmodStudioMappedOneShotDiagnostics.BuildMappedOneShotFailureDetail(path, guid));
        }

        private static bool TryStartMappedRunMusic(string operation, string path)
        {
            if (GuidMappedNaudioStudioProxy.TryStartMappedRunMusic(path))
                return true;

            WarnMappedFailure(operation, path);
            return false;
        }

        private static bool TryStartMappedRunAmbience(string operation, string path)
        {
            if (GuidMappedNaudioStudioProxy.TryStartMappedRunAmbience(path))
                return true;

            WarnMappedFailure(operation, path);
            return false;
        }

        /// <summary>
        ///     Handles mapped act BGM before it reaches the vanilla run music proxy.
        ///     在映射的 act BGM 进入原版 run music proxy 前接管它。
        /// </summary>
        public sealed class UpdateMusic : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_update_music";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Starts GUID-backed run music after NRunMusicController.UpdateMusic chooses a mapped act track";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))];
            }

            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Mirrors vanilla track selection and bank loading, then skips the vanilla proxy for mapped tracks.
            ///     复现原版曲目选择和 bank 加载，然后对映射曲目跳过原版 proxy。
            /// </summary>
            public static bool Prefix(
                    NRunMusicController __instance,
                    IRunState ____runState,
                    ref string ____currentTrack,
                    Node ____proxy)
                // ReSharper restore InconsistentNaming
            {
                if (ShouldUseVanilla())
                    return true;

                var bgMusicOptions = ____runState.Act.BgMusicOptions;
                var musicBankPaths = ____runState.Act.MusicBankPaths;
                var index = new Rng(____runState.Rng.Seed).NextInt(0, bgMusicOptions.Length);
                var track = bgMusicOptions[index];
                if (!GuidMappedNaudioStudioProxy.IsMappedPath(track))
                {
                    GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                    return true;
                }

                var bankPaths = new GdArray { musicBankPaths[index] };
                TryCall(____proxy, LoadActBanksMethod, bankPaths);
                ____currentTrack = track;
                StopVanillaRunMusic(____proxy);
                GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                TryStartMappedRunMusic("UpdateMusic", track);
                TryCall(____proxy, SetGlobalParameterMethod, "Progress", 0);
                __instance.UpdateAmbience();
                return false;
            }
        }

        /// <summary>
        ///     Handles combat encounter CustomBgm, which calls the run music proxy directly.
        ///     处理会直接调用 run music proxy 的战斗遭遇 CustomBgm。
        /// </summary>
        public sealed class PlayCustomMusic : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_play_custom_music";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "GUID-backed NRunMusicController.PlayCustomMusic for EncounterModel.CustomBgm";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                    [new(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic), [typeof(string)])];
            }

            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Starts mapped custom BGM and skips the vanilla proxy for guids.txt-only paths.
            ///     播放映射的自定义 BGM，并对仅存在于 guids.txt 的路径跳过原版 proxy。
            /// </summary>
            public static bool Prefix(NRunMusicController __instance, string customMusic, Node ____proxy)
                // ReSharper restore InconsistentNaming
            {
                _ = __instance;

                if (ShouldUseVanilla() || string.IsNullOrEmpty(customMusic))
                    return true;

                GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                if (!GuidMappedNaudioStudioProxy.IsMappedPath(customMusic))
                    return true;

                StopVanillaRunMusic(____proxy);
                TryStartMappedRunMusic("PlayCustomMusic", customMusic);
                return false;
            }
        }

        /// <summary>
        ///     Restores mapped act music when leaving a custom-BGM combat.
        ///     离开自定义 BGM 战斗时恢复映射的 act 音乐。
        /// </summary>
        public sealed class StopCustomMusic : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_stop_custom_music";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Restores GUID-backed act music in NRunMusicController.StopCustomMusic";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.StopCustomMusic))];
            }

            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Restores the selected act track when it only exists in GUID mappings.
            ///     当选中的 act 曲目只存在于 GUID 映射时恢复该曲目。
            /// </summary>
            public static bool Prefix(NRunMusicController __instance, string ____currentTrack, Node ____proxy)
                // ReSharper restore InconsistentNaming
            {
                _ = __instance;

                if (ShouldUseVanilla())
                    return true;

                GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                if (string.IsNullOrEmpty(____currentTrack) ||
                    !GuidMappedNaudioStudioProxy.IsMappedPath(____currentTrack))
                    return true;

                StopVanillaRunMusic(____proxy);
                TryStartMappedRunMusic("StopCustomMusic", ____currentTrack);
                TryCall(____proxy, SetGlobalParameterMethod, "Progress", 7f);
                return false;
            }
        }

        /// <summary>
        ///     Releases mapped run music and ambience alongside the vanilla proxy.
        ///     随原版 proxy 一起释放映射的 run music 和 ambience。
        /// </summary>
        public sealed class StopMusic : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_stop_music";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Releases mapped run music and ambience in NRunMusicController.StopMusic";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Releases mapped run-owned instances before vanilla unloads run audio.
            ///     在原版卸载 run 音频前释放 run 拥有的映射实例。
            /// </summary>
            public static void Prefix(NRunMusicController __instance)
            {
                _ = __instance;

                if (ShouldUseVanilla())
                    return;

                GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                GuidMappedNaudioStudioProxy.ReleaseMappedRunAmbience();
            }
        }

        /// <summary>
        ///     Routes numeric music parameter updates used by boss BGM progression.
        ///     路由 boss BGM 进度使用的数值音乐参数更新。
        /// </summary>
        public sealed class UpdateMusicParameter : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_update_music_parameter";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Routes NRunMusicController.UpdateMusicParameter to active mapped run music";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusicParameter),
                        [typeof(string), typeof(float)]),
                ];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Applies boss-progress parameters to the mapped run music instance.
            ///     将 boss 进度参数应用到映射的 run music 实例。
            /// </summary>
            public static bool Prefix(NRunMusicController __instance, string label, float trackIndex)
            {
                _ = __instance;

                if (ShouldUseVanilla())
                    return true;

                return !GuidMappedNaudioStudioProxy.TrySetParameterOnMappedRunMusic(label, trackIndex);
            }
        }

        /// <summary>
        ///     Handles mapped act or encounter ambience before it reaches the vanilla run music proxy.
        ///     在映射的 act 或遭遇 ambience 进入原版 run music proxy 前接管它。
        /// </summary>
        public sealed class UpdateAmbience : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_update_ambience";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Starts GUID-backed run ambience after NRunMusicController.UpdateAmbience chooses a mapped path";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.UpdateAmbience))];
            }

            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Mirrors vanilla ambience selection, then skips the vanilla proxy for mapped ambience.
            ///     复现原版 ambience 选择，然后对映射 ambience 跳过原版 proxy。
            /// </summary>
            public static bool Prefix(
                    NRunMusicController __instance,
                    IRunState ____runState,
                    ref string ____currentAmbience,
                    Node ____proxy)
                // ReSharper restore InconsistentNaming
            {
                _ = __instance;

                if (ShouldUseVanilla())
                    return true;

                var ambience = ____runState.Act.AmbientSfx;
                if (____runState.CurrentRoom is CombatRoom { Encounter: { HasAmbientSfx: true } encounter })
                    ambience = encounter.AmbientSfx;

                if (!GuidMappedNaudioStudioProxy.IsMappedPath(ambience))
                {
                    GuidMappedNaudioStudioProxy.ReleaseMappedRunAmbience();
                    return true;
                }

                if (GuidMappedNaudioStudioProxy.HasActiveMappedRunAmbience(ambience))
                    return false;

                ____currentAmbience = ambience;
                StopVanillaRunAmbience(____proxy);
                GuidMappedNaudioStudioProxy.ReleaseMappedRunAmbience();
                TryStartMappedRunAmbience("UpdateAmbience", ambience);
                return false;
            }
        }

        /// <summary>
        ///     Mirrors campfire ambience parameter updates for mapped ambience events.
        ///     为映射的 ambience 事件复现营火 ambience 参数更新。
        /// </summary>
        public sealed class TriggerCampfireGoingOut : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "nrun_music_guid_mapped_trigger_campfire_going_out";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static string Description =>
                "Routes NRunMusicController.TriggerCampfireGoingOut to active mapped ambience";

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.TriggerCampfireGoingOut))];
            }

            // ReSharper disable once InconsistentNaming
            /// <summary>
            ///     Applies the campfire transition parameter to mapped ambience.
            ///     将营火转场参数应用到映射的 ambience。
            /// </summary>
            public static void Postfix(NRunMusicController __instance)
            {
                _ = __instance;

                if (ShouldUseVanilla())
                    return;

                GuidMappedNaudioStudioProxy.TrySetParameterOnMappedRunAmbience("Campfire", 1f);
            }
        }
    }
}
