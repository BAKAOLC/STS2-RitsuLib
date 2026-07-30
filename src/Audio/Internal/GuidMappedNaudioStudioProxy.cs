using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Mirrors the loop, music, and run-audio ownership used by <c>audio_manager_proxy.gd</c> for
    ///         <c>event:/…</c> paths available through <c>guids.txt</c> and mod banks but absent from a strings bank.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对可通过 <c>guids.txt</c> 和模组音频库访问、但不存在于 strings bank 中的 <c>event:/…</c>
    ///         路径，复现 <c>audio_manager_proxy.gd</c> 使用的循环、音乐和局内音频归属管理。
    ///     </para>
    /// </summary>
    internal static class GuidMappedNaudioStudioProxy
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, List<LoopSlot>> LoopQueues = new(StringComparer.Ordinal);

        private static GodotObject? _musicInstance;
        private static GodotObject? _runMusicInstance;
        private static GodotObject? _runAmbienceInstance;
        private static string? _runMusicPath;
        private static string? _runAmbiencePath;

        internal static bool IsMappedPath(string? path)
        {
            return !string.IsNullOrEmpty(path) &&
                   FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out _);
        }

        internal static void StopAllMappedLoops()
        {
            lock (Gate)
            {
                foreach (var path in LoopQueues.Keys.ToArray())
                {
                    var slots = LoopQueues[path];
                    slots.RemoveAll(static slot => StopSlot(slot));
                    if (slots.Count == 0)
                        LoopQueues.Remove(path);
                }
            }
        }

        internal static bool TryEnqueueMappedLoop(string path, bool usesLoopParam)
        {
            var inst = FmodStudioEventInstances.TryCreate(path);
            if (inst is null)
                return false;

            if (!FmodStudioEventInstances.TryStart(inst))
            {
                DisposeUnownedInstance(path, inst);
                return false;
            }

            var releaseScheduled = FmodStudioEventInstances.TryScheduleRelease(inst);
            lock (Gate)
            {
                if (!LoopQueues.TryGetValue(path, out var list))
                {
                    list = [];
                    LoopQueues[path] = list;
                }

                list.Add(new(inst, usesLoopParam, releaseScheduled));
            }

            return true;
        }

        internal static bool TryStopMappedLoop(string path)
        {
            lock (Gate)
            {
                return StopMappedLoopCore(path);
            }
        }

        private static bool StopMappedLoopCore(string path)
        {
            if (!LoopQueues.TryGetValue(path, out var list) || list.Count == 0)
                return false;

            var slot = list[0];
            if (!StopSlot(slot))
                return false;

            list.RemoveAt(0);
            if (list.Count == 0)
                LoopQueues.Remove(path);

            return true;
        }

        private static bool StopSlot(LoopSlot slot)
        {
            if (!GodotObject.IsInstanceValid(slot.Instance))
                return true;

            try
            {
                if (slot.UsesLoopParam)
                    slot.Instance.Call("set_parameter_by_name", new StringName("loop"), 1f);
                else
                    slot.Instance.Call("stop", 1);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] mapped StopLoop: {ex}");
                return false;
            }

            return slot.ReleaseScheduled || FmodStudioEventInstances.TryScheduleRelease(slot.Instance);
        }

        internal static bool TrySetParamOnFirstMappedLoop(string path, string param, float value)
        {
            lock (Gate)
            {
                if (!LoopQueues.TryGetValue(path, out var list) || list.Count == 0)
                    return false;

                try
                {
                    list[0].Instance.Call("set_parameter_by_name", new StringName(param), value);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] mapped SetParam: {ex}");
                    return false;
                }

                return true;
            }
        }

        internal static bool ReleaseMappedMusic()
        {
            lock (Gate)
            {
                return ReleaseMappedInstance(ref _musicInstance, "StopMusic");
            }
        }

        internal static bool TryStartMappedMusic(string path)
        {
            var inst = FmodStudioEventInstances.TryCreate(path);
            if (inst is null)
                return false;

            lock (Gate)
            {
                if (!ReleaseMappedInstance(ref _musicInstance, "ReplaceMusic"))
                {
                    DisposeUnownedInstance(path, inst);
                    return false;
                }

                if (!FmodStudioEventInstances.TryStart(inst))
                {
                    DisposeUnownedInstance(path, inst);
                    return false;
                }

                _musicInstance = inst;
            }

            return true;
        }

        internal static bool ReleaseMappedRunMusic()
        {
            lock (Gate)
            {
                if (!ReleaseMappedInstance(ref _runMusicInstance, "StopRunMusic"))
                    return false;

                _runMusicPath = null;
                return true;
            }
        }

        internal static bool TryStartMappedRunMusic(string path)
        {
            return TryStartMappedSingleInstance(path, ref _runMusicInstance, ref _runMusicPath, "PlayRunMusic");
        }

        internal static bool ReleaseMappedRunAmbience()
        {
            lock (Gate)
            {
                if (!ReleaseMappedInstance(ref _runAmbienceInstance, "StopRunAmbience"))
                    return false;

                _runAmbiencePath = null;
                return true;
            }
        }

        internal static bool TryStartMappedRunAmbience(string path)
        {
            return TryStartMappedSingleInstance(path, ref _runAmbienceInstance, ref _runAmbiencePath,
                "PlayRunAmbience");
        }

        internal static bool TrySetParameterOnMappedRunMusic(string parameter, float value)
        {
            lock (Gate)
            {
                return TrySetParameterOnMappedInstance(_runMusicInstance, parameter, value,
                    "UpdateRunMusicParameter");
            }
        }

        internal static bool TrySetParameterOnMappedRunAmbience(string parameter, float value)
        {
            lock (Gate)
            {
                return TrySetParameterOnMappedInstance(_runAmbienceInstance, parameter, value,
                    "UpdateRunAmbienceParameter");
            }
        }

        internal static bool TryUpdateMappedMusicParameter(string parameter, string labelValue)
        {
            lock (Gate)
            {
                if (_musicInstance is null || !GodotObject.IsInstanceValid(_musicInstance))
                    return false;

                try
                {
                    _musicInstance.Call("set_parameter_by_name_with_label", parameter, labelValue, false);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] mapped UpdateMusicParameter: {ex}");
                    return false;
                }

                return true;
            }
        }

        internal static bool HasActiveMappedMusic()
        {
            lock (Gate)
            {
                return _musicInstance is not null && GodotObject.IsInstanceValid(_musicInstance);
            }
        }

        internal static bool HasActiveMappedRunMusic(string path)
        {
            lock (Gate)
            {
                return string.Equals(_runMusicPath, path, StringComparison.Ordinal) &&
                       _runMusicInstance is not null &&
                       GodotObject.IsInstanceValid(_runMusicInstance);
            }
        }

        internal static bool HasActiveMappedRunAmbience(string path)
        {
            lock (Gate)
            {
                return string.Equals(_runAmbiencePath, path, StringComparison.Ordinal) &&
                       _runAmbienceInstance is not null &&
                       GodotObject.IsInstanceValid(_runAmbienceInstance);
            }
        }

        private static bool TryStartMappedSingleInstance(string path, ref GodotObject? slot, ref string? slotPath,
            string operation)
        {
            var inst = FmodStudioEventInstances.TryCreate(path);
            if (inst is null)
                return false;

            lock (Gate)
            {
                if (!ReleaseMappedInstance(ref slot, $"Replace{operation}"))
                {
                    DisposeUnownedInstance(path, inst);
                    return false;
                }

                if (!FmodStudioEventInstances.TryStart(inst))
                {
                    DisposeUnownedInstance(path, inst);
                    return false;
                }

                slot = inst;
                slotPath = path;
            }

            return true;
        }

        private static bool TrySetParameterOnMappedInstance(GodotObject? instance, string parameter, float value,
            string operation)
        {
            if (instance is null || !GodotObject.IsInstanceValid(instance))
                return false;

            try
            {
                instance.Call("set_parameter_by_name", parameter, value);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] mapped {operation}: {ex}");
                return false;
            }
        }

        private static bool ReleaseMappedInstance(ref GodotObject? instance, string operation)
        {
            if (instance is null)
                return true;

            if (!GodotObject.IsInstanceValid(instance))
            {
                instance = null;
                return true;
            }

            if (!FmodStudioEventInstances.TryStop(instance))
            {
                RitsuLibFramework.Logger.Warn($"[Audio] mapped {operation}: stop failed.");
                return false;
            }

            if (!FmodStudioEventInstances.TryScheduleRelease(instance))
                return false;

            instance = null;
            return true;
        }

        private static void DisposeUnownedInstance(string path, GodotObject instance)
        {
            var handle = new AudioEventHandle(AudioSource.Event(path), AudioLifecycleScope.Manual, instance);
            AudioLifecycleRegistry.Shared.Attach(handle, null);
            if (handle.TryStop(false))
                handle.TryRelease();
        }

        private sealed record LoopSlot(GodotObject Instance, bool UsesLoopParam, bool ReleaseScheduled);
    }
}
