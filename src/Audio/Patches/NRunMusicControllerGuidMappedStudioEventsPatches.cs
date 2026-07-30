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
    ///     <para xml:lang="en">
    ///         Patches run-scoped music and ambience paths that call the run proxy instead of
    ///         <see cref="NAudioManager" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">修补不经由 <see cref="NAudioManager" />、而是直接调用局内代理的音乐和环境音路径。</para>
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

        private static bool StopVanillaRunMusic(Node? proxy)
        {
            return TryCall(proxy, StopMusicMethod);
        }

        private static bool StopVanillaRunAmbience(Node? proxy)
        {
            return TryCall(proxy, StopAmbienceMethod);
        }

        private static bool TryCall(Node? proxy, StringName method, params Variant[] args)
        {
            if (proxy is null)
                return false;

            try
            {
                proxy.Call(method, args);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] run music proxy {method}: {ex}");
                return false;
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

                if (!ReleaseOwnedMappedActBankCore())
                    return false;

                if (!FmodStudioServer.TryLoadBank(bankPath))
                    return false;

                FmodStudioServer.TryWaitForAllLoads();
                if (FmodStudioServer.TryCheckEventGuid(eventGuid) != true)
                {
                    if (!FmodStudioServer.TryUnloadBank(bankPath))
                        _ownedMappedActBankPath = bankPath;
                    return false;
                }

                _ownedMappedActBankPath = bankPath;
                return true;
            }
        }

        private static bool ReleaseOwnedMappedActBank()
        {
            lock (MappedActBankGate)
            {
                return ReleaseOwnedMappedActBankCore();
            }
        }

        private static bool ReleaseOwnedMappedActBankCore()
        {
            if (_ownedMappedActBankPath is null)
                return true;

            if (!FmodStudioServer.TryUnloadBank(_ownedMappedActBankPath))
                return false;

            _ownedMappedActBankPath = null;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Loads and starts mapped act music before it reaches the native run-music proxy.</para>
        ///     <para xml:lang="zh-CN">在映射的章节音乐进入原生局内音乐代理前加载并启动它。</para>
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
            ///     <para xml:lang="en">
            ///         Mirrors native deterministic track selection and act-bank loading, then handles mapped tracks
            ///         by GUID.
            ///     </para>
            ///     <para xml:lang="zh-CN">复现原生的确定性曲目选择和章节音频库加载，并按 GUID 处理已映射曲目。</para>
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
                    var musicReleased = GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                    var bankReleased = ReleaseOwnedMappedActBank();
                    return musicReleased && bankReleased;
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
                    var musicReleased = GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                    var bankReleased = ReleaseOwnedMappedActBank();
                    return musicReleased && bankReleased;
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

                var vanillaStopped = StopVanillaRunMusic(____proxy);
                var mappedReleased = GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic();
                if (!vanillaStopped || !mappedReleased)
                    return false;

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
        ///     <para xml:lang="en">Handles mapped encounter music passed directly to the run-music proxy.</para>
        ///     <para xml:lang="zh-CN">处理直接传给局内音乐代理的已映射遭遇音乐。</para>
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

                if (!GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic())
                    return false;

                if (!GuidMappedNaudioStudioProxy.IsMappedPath(customMusic))
                    return true;

                if (!StopVanillaRunMusic(____proxy))
                    return false;

                TryStartMappedRunMusic("PlayCustomMusic", customMusic);
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Restores mapped act music when custom encounter music ends.</para>
        ///     <para xml:lang="zh-CN">自定义遭遇音乐结束时恢复已映射的章节音乐。</para>
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

                if (!GuidMappedNaudioStudioProxy.ReleaseMappedRunMusic())
                    return false;

                if (string.IsNullOrEmpty(____currentTrack) ||
                    !GuidMappedNaudioStudioProxy.IsMappedPath(____currentTrack))
                    return true;

                if (!StopVanillaRunMusic(____proxy))
                    return false;

                TryStartMappedRunMusic("StopCustomMusic", ____currentTrack);
                TryCall(____proxy, SetGlobalParameterMethod, "Progress", 7f);
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Stops mapped run music and ambience and releases the retained mapped act bank.</para>
        ///     <para xml:lang="zh-CN">停止已映射的局内音乐和环境音，并释放保留的映射章节音频库。</para>
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
        ///     <para xml:lang="en">Routes numeric progression parameters to active mapped run music.</para>
        ///     <para xml:lang="zh-CN">将数值型进度参数路由到活动的已映射局内音乐。</para>
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
        ///     <para xml:lang="en">Handles mapped act or encounter ambience before it reaches the native run-music proxy.</para>
        ///     <para xml:lang="zh-CN">在映射的章节或遭遇环境音进入原生局内音乐代理前接管它。</para>
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
                    return GuidMappedNaudioStudioProxy.ReleaseMappedRunAmbience();

                if (GuidMappedNaudioStudioProxy.HasActiveMappedRunAmbience(ambience))
                    return false;

                var vanillaStopped = StopVanillaRunAmbience(____proxy);
                var mappedReleased = GuidMappedNaudioStudioProxy.ReleaseMappedRunAmbience();
                if (!vanillaStopped || !mappedReleased)
                    return false;
                if (!TryStartMappedRunAmbience("UpdateAmbience", ambience))
                    return false;

                ____currentAmbience = ambience;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the native campfire parameter transition to active mapped ambience.</para>
        ///     <para xml:lang="zh-CN">将原生营火参数转换应用到活动的已映射环境音。</para>
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
