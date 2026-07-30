using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     <para xml:lang="en">Centralizes guarded <c>FmodServer</c> lookup and dynamic Godot method invocation.</para>
    ///     <para xml:lang="zh-CN">集中执行受保护的 <c>FmodServer</c> 查找和 Godot 动态方法调用。</para>
    /// </summary>
    internal static class FmodStudioGateway
    {
        internal static readonly StringName ServerName = new("FmodServer");

        public static GodotObject? TryGetServer()
        {
            try
            {
                if (!Engine.HasSingleton(ServerName))
                    return null;

                var server = Engine.GetSingleton(ServerName);
                return server is not null && GodotObject.IsInstanceValid(server) ? server : null;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FmodServer singleton: {ex}");
                return null;
            }
        }

        public static bool TryCall(out Variant result, StringName method, params Variant[] args)
        {
            result = default;
            var server = TryGetServer();
            if (server is null || !server.HasMethod(method))
                return false;

            try
            {
                result = args.Length == 0 ? server.Call(method) : server.Call(method, args);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD {method}: {ex}");
                return false;
            }
        }

        public static bool TryCall(StringName method, params Variant[] args)
        {
            return TryCall(out _, method, args);
        }
    }
}
