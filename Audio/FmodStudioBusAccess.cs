using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Direct bus objects from FMOD Studio (parallel to strings in <see cref="FmodStudioRouting" />).
    /// </summary>
    public static class FmodStudioBusAccess
    {
        private static readonly StringName GetVolume = new("get_volume");
        private static readonly StringName SetVolume = new("set_volume");
        private static readonly StringName SetMute = new("set_mute");
        private static readonly StringName SetPaused = new("set_paused");
        private static readonly StringName BusGetPath = new("get_path");
        private static readonly StringName BusGetStudioGuid = new("get_guid");
        private static readonly StringName BusGetNumericId = new("get_id");

        /// <summary>
        ///     Resolves a Studio bus object for <paramref name="busPath" />; null when the addon call fails.
        /// </summary>
        public static GodotObject? TryGetBus(string busPath)
        {
            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.GetBus, busPath)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Reads linear volume for <paramref name="busPath" />; 0 when missing or on error.
        /// </summary>
        public static float TryGetVolume(string busPath)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return 0f;

            try
            {
                return bus.Call(GetVolume).AsSingle();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] bus get_volume: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        ///     Sets linear volume on the resolved bus.
        /// </summary>
        public static bool TrySetVolume(string busPath, float linearVolume)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return false;

            try
            {
                bus.Call(SetVolume, linearVolume);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] bus set_volume: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Mutes or unmutes the bus.
        /// </summary>
        public static bool TrySetMute(string busPath, bool muted)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return false;

            try
            {
                bus.Call(SetMute, muted);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] bus set_mute: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Pauses or resumes the bus.
        /// </summary>
        public static bool TrySetPaused(string busPath, bool paused)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return false;

            try
            {
                bus.Call(SetPaused, paused);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] bus set_paused: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Reads the Studio bus GUID for <paramref name="busPath" /> (stable across renames); null when unavailable.
        /// </summary>
        public static string? TryGetStudioGuid(string busPath)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return null;

            try
            {
                return bus.Call(BusGetStudioGuid).AsString();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] bus get_guid: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     Reads FMOD Studio's numeric bus id when the addon exposes <c>get_id</c>; null when missing or unsupported.
        /// </summary>
        public static long? TryGetNumericId(string busPath)
        {
            var bus = TryGetBus(busPath);
            if (bus is null)
                return null;

            if (!bus.HasMethod(BusGetNumericId))
                return null;

            try
            {
                var v = bus.Call(BusGetNumericId);
                return v.VariantType switch
                {
                    Variant.Type.Int => v.AsInt32(),
                    Variant.Type.Float => (long)v.AsDouble(),
                    _ => v.AsInt64(),
                };
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] bus get_id: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     Finds a bus path whose Studio GUID matches <paramref name="studioBusGuid" />.
        /// </summary>
        public static string? TryFindBusPathByStudioGuid(string studioBusGuid)
        {
            if (string.IsNullOrWhiteSpace(studioBusGuid))
                return null;

            foreach (var item in FmodStudioServer.TryGetAllBuses())
            {
                if (item.VariantType != Variant.Type.Object)
                    continue;

                var bus = item.AsGodotObject();
                if (bus is null || !GodotObject.IsInstanceValid(bus))
                    continue;

                try
                {
                    if (!string.Equals(bus.Call(BusGetStudioGuid).AsString(), studioBusGuid,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    return bus.Call(BusGetPath).AsString();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error($"[Audio] bus enumerate match: {ex.Message}");
                }
            }

            return null;
        }
    }
}
