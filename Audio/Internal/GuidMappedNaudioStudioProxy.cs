using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     Mirrors <c>audio_manager_proxy.gd</c> bookkeeping for <c>event:/…</c> paths that exist only via guids.txt + mod
    ///     banks (no strings.bank path table).
    /// </summary>
    internal static class GuidMappedNaudioStudioProxy
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, List<LoopSlot>> LoopQueues = new(StringComparer.Ordinal);

        private static GodotObject? _musicInstance;

        internal static bool IsMappedPath(string? path)
        {
            return !string.IsNullOrEmpty(path) &&
                   FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(path, out _);
        }

        internal static void StopAllMappedLoops()
        {
            lock (Gate)
            {
                foreach (var path in LoopQueues.Keys)
                    StopMappedLoopCore(path);

                LoopQueues.Clear();
            }
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

            return true;
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
                if (_musicInstance is null)
                    return;

                try
                {
                    _musicInstance.Call("stop", 0);
                    _musicInstance.Call("release");
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error($"[Audio] mapped StopMusic: {ex.Message}");
                }
                finally
                {
                    _musicInstance = null;
                }
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

        private sealed record LoopSlot(GodotObject Instance, bool UsesLoopParam);
    }
}
