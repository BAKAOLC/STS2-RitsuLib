using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Long-lived Studio event instances (manual start/stop/release).
    /// </summary>
    public static class FmodStudioEventInstances
    {
        /// <summary>
        ///     Creates a typed event handle for a Studio event source.
        /// </summary>
        public static AudioEventHandle? TryCreateHandle(AudioSource source, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = source switch
            {
                StudioEventSource path => TryCreate(path.Path),
                StudioGuidSource guid => TryCreateFromGuid(guid.Value),
                _ => null,
            };

            return instance is null
                ? null
                : new AudioEventHandle(source, options.ScopeToken?.Scope ?? options.Scope, instance);
        }

        /// <summary>
        ///     Creates a Studio event or snapshot instance; null when creation fails.
        /// </summary>
        public static GodotObject? TryCreate(string eventOrSnapshotPath)
        {
            if (!FmodStudioGuidPathTable.TryGetStudioGuidForEventPath(eventOrSnapshotPath, out var mappedGuid))
                return TryCreateByPathOnly(eventOrSnapshotPath);

            var guidInCache = FmodStudioServer.TryCheckEventGuid(mappedGuid) == true;
            var pathInCache = ProbeStudioHasEventPath(eventOrSnapshotPath) == true;

            if (!guidInCache) return pathInCache ? TryCreateByPathOnly(eventOrSnapshotPath) : null;
            var byGuid = TryCreateFromGuid(mappedGuid);
            if (byGuid is not null)
                return byGuid;

            return pathInCache ? TryCreateByPathOnly(eventOrSnapshotPath) : null;
        }

        /// <summary>
        ///     Raw <c>FmodServer.check_event_path</c>; does not use <c>FmodStudioServer.TryCheckEventPath</c> guids-table
        ///     shortcut.
        /// </summary>
        private static bool? ProbeStudioHasEventPath(string eventPath)
        {
            if (!FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CheckEventPath, eventPath))
                return null;

            return v.AsBool();
        }

        /// <summary>
        ///     Creates a Studio instance from an event or snapshot GUID string (same underlying call as the editor tools).
        /// </summary>
        public static GodotObject? TryCreateFromGuid(string eventGuid)
        {
            if (string.IsNullOrWhiteSpace(eventGuid))
                return null;

            if (!FmodStudioGuidInterop.TryNormalizeForAddon(eventGuid, out var normalized))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] FMOD create_event_instance_with_guid: invalid GUID string '{eventGuid}' " +
                    $"(GDExtension expects braced format, see fmod-gdextension helpers/common.h string_to_fmod_guid).");
                return null;
            }

            if (FmodStudioServer.TryCheckEventGuid(normalized) == false)
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateEventInstanceWithGuid, normalized)
                ? null
                : v.AsGodotObject();
        }

        private static GodotObject? TryCreateByPathOnly(string eventOrSnapshotPath)
        {
            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateEventInstance, eventOrSnapshotPath)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Calls <c>start</c> on the instance when non-null.
        /// </summary>
        public static bool TryStart(GodotObject? instance)
        {
            if (instance is null)
                return false;

            try
            {
                instance.Call("start");
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD event start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Stops the instance; <paramref name="allowFadeOut" /> maps to FMOD stop mode.
        /// </summary>
        public static bool TryStop(GodotObject? instance, bool allowFadeOut = true)
        {
            if (instance is null)
                return false;

            try
            {
                instance.Call("stop", allowFadeOut ? 0 : 1);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD event stop: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Releases native resources for the instance; errors are logged only.
        /// </summary>
        public static void TryRelease(GodotObject? instance)
        {
            if (instance is null)
                return;

            try
            {
                instance.Call("release");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD event release: {ex.Message}");
            }
        }
    }
}
