using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     Centralized FmodServer lookup and guarded <see cref="GodotObject.Call(StringName, Variant[])" /> calls.
    /// </summary>
    internal static class FmodStudioGateway
    {
        internal static readonly StringName ServerName = new("FmodServer");

        public static GodotObject? TryGetServer()
        {
            try
            {
                return !Engine.HasSingleton(ServerName) ? null : Engine.GetSingleton(ServerName);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FmodServer singleton: {ex.Message}");
                return null;
            }
        }

        public static bool TryCall(out Variant result, StringName method, params Variant[] args)
        {
            result = default;
            var server = TryGetServer();
            if (server is null)
                return false;

            try
            {
                result = args.Length == 0 ? server.Call(method) : server.Call(method, args);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD {method}: {ex.Message}");
                return false;
            }
        }

        public static bool TryCall(StringName method, params Variant[] args)
        {
            return TryCall(out _, method, args);
        }
    }
}
