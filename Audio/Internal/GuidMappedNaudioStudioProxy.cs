using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     Mirrors <c>audio_manager_proxy.gd</c> bookkeeping for <c>event:/…</c> paths that exist only via guids.txt + mod
    ///     banks (no strings.bank path table).
    ///     复现 <c>audio_manager_proxy.gd</c> 对仅通过 guids.txt + mod
    ///     bank 存在的 <c>event:/…</c> 路径的簿记（没有 strings.bank 路径表）。
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
            LoopSlot[] slots;
            lock (Gate)
            {
                slots = LoopQueues.Values.SelectMany(static list => list).ToArray();
                LoopQueues.Clear();
            }

            foreach (var slot in slots)
                StopSlot(slot);
        }

        internal static bool TryEnqueueMappedLoop(string path, bool usesLoopParam)
        {
            var inst = FmodStudioEventInstances.TryCreate(path);
            if (inst is null)
                return false;

            try
            {
                inst.Call("start");
                inst.Call("release");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] mapped PlayLoop start/release: {ex.Message}");
                return false;
            }

            lock (Gate)
            {
                if (!LoopQueues.TryGetValue(path, out var list))
                {
                    list = [];
                    LoopQueues[path] = list;
                }

                list.Add(new(inst, usesLoopParam));
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
            list.RemoveAt(0);
            if (list.Count == 0)
                LoopQueues.Remove(path);

            StopSlot(slot);

            return true;
        }

        private static void StopSlot(LoopSlot slot)
        {
            try
            {
                if (slot.UsesLoopParam)
                    slot.Instance.Call("set_parameter_by_name", new StringName("loop"), 1f);
                else
                    slot.Instance.Call("stop", 1);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] mapped StopLoop: {ex.Message}");
            }
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
                    RitsuLibFramework.Logger.Error($"[Audio] mapped SetParam: {ex.Message}");
                    return false;
                }

                return true;
            }
        }

        internal static void ReleaseMappedMusic()
        {
            lock (Gate)
            {
                ReleaseMappedInstance(ref _musicInstance, "StopMusic");
            }
        }

        internal static bool TryStartMappedMusic(string path)
        {
            var inst = FmodStudioEventInstances.TryCreate(path);
            if (inst is null)
                return false;

            try
            {
                inst.Call("start");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] mapped PlayMusic start: {ex.Message}");
                return false;
            }

            lock (Gate)
            {
                _musicInstance = inst;
            }

            return true;
        }

        internal static void ReleaseMappedRunMusic()
        {
            lock (Gate)
            {
                ReleaseMappedInstance(ref _runMusicInstance, "StopRunMusic");
                _runMusicPath = null;
            }
        }

        internal static bool TryStartMappedRunMusic(string path)
        {
            return TryStartMappedSingleInstance(path, ref _runMusicInstance, ref _runMusicPath, "PlayRunMusic");
        }

        internal static void ReleaseMappedRunAmbience()
        {
            lock (Gate)
            {
                ReleaseMappedInstance(ref _runAmbienceInstance, "StopRunAmbience");
                _runAmbiencePath = null;
            }
        }

        internal static bool TryStartMappedRunAmbience(string path)
        {
            return TryStartMappedSingleInstance(path, ref _runAmbienceInstance, ref _runAmbiencePath,
                "PlayRunAmbience");
        }

        internal static bool TrySetParameterOnMappedRunMusic(string parameter, float value)
        {
            return TrySetParameterOnMappedInstance(_runMusicInstance, parameter, value, "UpdateRunMusicParameter");
        }

        internal static bool TrySetParameterOnMappedRunAmbience(string parameter, float value)
        {
            return TrySetParameterOnMappedInstance(_runAmbienceInstance, parameter, value,
                "UpdateRunAmbienceParameter");
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
                    RitsuLibFramework.Logger.Error($"[Audio] mapped UpdateMusicParameter: {ex.Message}");
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

            try
            {
                inst.Call("start");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] mapped {operation} start: {ex.Message}");
                return false;
            }

            lock (Gate)
            {
                slot = inst;
                slotPath = path;
            }

            return true;
        }

        private static bool TrySetParameterOnMappedInstance(GodotObject? instance, string parameter, float value,
            string operation)
        {
            lock (Gate)
            {
                if (instance is null || !GodotObject.IsInstanceValid(instance))
                    return false;

                try
                {
                    instance.Call("set_parameter_by_name", parameter, value);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error($"[Audio] mapped {operation}: {ex.Message}");
                    return false;
                }

                return true;
            }
        }

        private static void ReleaseMappedInstance(ref GodotObject? instance, string operation)
        {
            if (instance is null)
                return;

            try
            {
                instance.Call("stop", 0);
                instance.Call("release");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] mapped {operation}: {ex.Message}");
            }
            finally
            {
                instance = null;
            }
        }

        private sealed record LoopSlot(GodotObject Instance, bool UsesLoopParam);
    }
}
