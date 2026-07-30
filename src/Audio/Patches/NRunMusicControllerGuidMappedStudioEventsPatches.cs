using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Audio.Internal;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Audio.Patches
{
    /// <summary>
    ///     Harmony patches for run-scoped music and ambience paths that bypass <see cref="NAudioManager" />.
    ///     处理绕过 <see cref="NAudioManager" /> 的 run 级音乐和 ambience 路径的 Harmony patch。
    /// </summary>
    internal static class NRunMusicControllerGuidMappedStudioEventsPatches
    {
        private static readonly StringName StopMusicMethod = new("stop_music");
        private static readonly StringName StopAmbienceMethod = new("stop_ambience");
        private static readonly StringName SetGlobalParameterMethod = new("update_global_parameter");

        private static readonly Lock MappedActBankGate = new();
        private static string? _ownedMappedActBankPath;

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
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] run music proxy {method}: {ex.Message}");
            }
        }

        private static void WarnMappedFailure(string operation, string path)
        {
            if (FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out var guid))
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] Mapped {operation} failed. " +
                    FmodStudioMappedOneShotDiagnostics.BuildMappedOneShotFailureDetail(path, guid));
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool TryStartMappedRunMusic(string operation, string path)
        {
            if (GuidMappedNaudioStudioProxy.TryStartMappedRunMusic(path))
                return true;

            WarnMappedFailure(operation, path);
            return false;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool TryStartMappedRunAmbience(string operation, string path)
        {
            if (GuidMappedNaudioStudioProxy.TryStartMappedRunAmbience(path))
                return true;

            WarnMappedFailure(operation, path);
            return false;
        }

        private static bool TryEnsureMappedActBank(string bankPath, string eventPath)
        {
            if (!FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventPath, out var eventGuid))
                return false;

            lock (MappedActBankGate)
            {
                if (FmodStudioServer.TryCheckEventGuid(eventGuid) == true)
                    return true;

                ReleaseOwnedMappedActBankCore();
                if (!FmodStudioServer.TryLoadBank(bankPath))
                    return false;

                FmodStudioServer.TryWaitForAllLoads();
                if (FmodStudioServer.TryCheckEventGuid(eventGuid) != true)
                {
                    FmodStudioServer.TryUnloadBank(bankPath);
                    return false;
                }

                _ownedMappedActBankPath = bankPath;
                return true;
            }
        }

        private static void ReleaseOwnedMappedActBank()
        {
            lock (MappedActBankGate)
                ReleaseOwnedMappedActBankCore();
        }

        private static void ReleaseOwnedMappedActBankCore()
        {
            if (_ownedMappedActBankPath is null)
                return;

            FmodStudioServer.TryUnloadBank(_ownedMappedActBankPath);
            _ownedMappedActBankPath = null;
        }

        /// <summary>
        ///     Handles mapped act BGM before it reaches the vanilla run music proxy.
        ///     在映射的 act BGM 进入原版 run music proxy 前接管它。
        /// </summary>
        internal sealed class UpdateMusic : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_update_music";
            public static bool IsCritical => false;

            public static string Description =>
                "Starts GUID-backed run music after NRunMusicController.UpdateMusic chooses a mapped act track";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))];
            }

            /// <summary>
            ///     Mirrors vanilla track selection and bank loading, then skips the vanilla proxy for mapped tracks.
            ///     复现原版曲目选择和 bank 加载，然后对映射曲目跳过原版 proxy。
            /// </summary>
            [HarmonyPriority(Priority.Last)]
            public static bool Prefix(
                NRunMusicController __instance,
                IRunState ____runState,
                ref string ____currentTrack,
#if STS2_AT_LEAST_0_108_0
                ref string? ____failedTrack,
#endif
                Node ____proxy)
            {
                if (ShouldUseVanilla())
                    return true;

                if (____runState.Act.BgMusicOptions.Length == 0)
                {
                    GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                    ReleaseOwnedMappedActBank();
                    return true;
                }

                var selection = NRunMusicController.ResolveMusic(
                    ____currentTrack,
                    ____runState.Act.BgMusicOptions,
                    ____runState.Act.MusicBankPaths,
                    ____runState.Rng.Seed);
                if (selection is not { } music)
                    return true;

                if (!GuidMappedNaudioStudioProxy.IsMappedPath(music.Track))
                {
                    GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                    ReleaseOwnedMappedActBank();
                    return true;
                }

#if STS2_AT_LEAST_0_108_0
                if (string.Equals(music.Track, ____failedTrack, StringComparison.Ordinal))
                    return false;
#endif

                if (!TryEnsureMappedActBank(music.BankPath, music.Track))
                {
#if STS2_AT_LEAST_0_108_0
                    ____failedTrack = music.Track;
#endif
                    return false;
                }

                StopVanillaRunMusic(____proxy);
                GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                if (!TryStartMappedRunMusic("UpdateMusic", music.Track))
                {
#if STS2_AT_LEAST_0_108_0
                    ____failedTrack = music.Track;
#endif
                    return false;
                }

#if STS2_AT_LEAST_0_108_0
                ____failedTrack = null;
#endif
                ____currentTrack = music.Track;
                TryCall(____proxy, SetGlobalParameterMethod, "Progress", 0);
                __instance.UpdateAmbience();
                return false;
            }
        }

        /// <summary>
        ///     Handles combat encounter CustomBgm, which calls the run music proxy directly.
        ///     处理会直接调用 run music proxy 的战斗遭遇 CustomBgm。
        /// </summary>
        internal sealed class PlayCustomMusic : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_play_custom_music";
            public static bool IsCritical => false;

            public static string Description =>
                "GUID-backed NRunMusicController.PlayCustomMusic for EncounterModel.CustomBgm";

            public static ModPatchTarget[] GetTargets()
            {
                return
                    [new(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic), [typeof(string)])];
            }

            public static bool Prefix(NRunMusicController __instance, string customMusic, Node ____proxy)
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
        internal sealed class StopCustomMusic : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_stop_custom_music";
            public static bool IsCritical => false;

            public static string Description =>
                "Restores GUID-backed act music in NRunMusicController.StopCustomMusic";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.StopCustomMusic))];
            }

            public static bool Prefix(NRunMusicController __instance, string ____currentTrack, Node ____proxy)
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
        internal sealed class StopMusic : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_stop_music";
            public static bool IsCritical => false;

            public static string Description =>
                "Releases mapped run music and ambience in NRunMusicController.StopMusic";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))];
            }

            public static void Prefix(NRunMusicController __instance)
            {
                _ = __instance;

                if (ShouldUseVanilla())
                    return;

                GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                GuidMappedNaudioStudioProxy.ReleaseMappedRunAmbience();
                ReleaseOwnedMappedActBank();
            }
        }

        /// <summary>
        ///     Routes numeric music parameter updates used by boss BGM progression.
        ///     路由 boss BGM 进度使用的数值音乐参数更新。
        /// </summary>
        internal sealed class UpdateMusicParameter : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_update_music_parameter";
            public static bool IsCritical => false;

            public static string Description =>
                "Routes NRunMusicController.UpdateMusicParameter to active mapped run music";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusicParameter),
                        [typeof(string), typeof(float)]),
                ];
            }

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
        internal sealed class UpdateAmbience : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_update_ambience";
            public static bool IsCritical => false;

            public static string Description =>
                "Starts GUID-backed run ambience after NRunMusicController.UpdateAmbience chooses a mapped path";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.UpdateAmbience))];
            }

            public static bool Prefix(
                NRunMusicController __instance,
                IRunState ____runState,
                ref string ____currentAmbience,
                Node ____proxy)
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
        internal sealed class TriggerCampfireGoingOut : IPatchMethod
        {
            public static string PatchId => "nrun_music_guid_mapped_trigger_campfire_going_out";
            public static bool IsCritical => false;

            public static string Description =>
                "Routes NRunMusicController.TriggerCampfireGoingOut to active mapped ambience";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NRunMusicController), nameof(NRunMusicController.TriggerCampfireGoingOut))];
            }

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
