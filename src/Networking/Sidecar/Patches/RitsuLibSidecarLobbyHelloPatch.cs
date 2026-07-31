using HarmonyLib;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds the lobby's network service to the sidecar session and sends the client hello when the host is
    ///         reachable.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将大厅的网络服务绑定到 sidecar 会话，并在主机可达时发送客户端握手消息。
    ///     </para>
    /// </summary>
    internal sealed class RitsuLibSidecarLobbyHelloPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_lobby_hello";

        public static bool IsCritical => false;

        public static string Description => "Sidecar session bind after StartRunLobby / LoadRunLobby construction";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(StartRunLobby),
                    ".ctor",
                    [typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int)],
                    MethodType.Constructor),
                new(
                    typeof(StartRunLobby),
                    ".ctor",
                    [
                        typeof(GameMode),
                        typeof(INetGameService),
                        typeof(IStartRunLobbyListener),
                        typeof(TimeServerResult),
                        typeof(int),
                    ],
                    MethodType.Constructor),
                new(
                    typeof(LoadRunLobby),
                    ".ctor",
                    [typeof(INetGameService), typeof(ILoadRunLobbyListener), typeof(SerializableRun)],
                    MethodType.Constructor),
                new(
                    typeof(LoadRunLobby),
                    ".ctor",
                    [typeof(INetGameService), typeof(ILoadRunLobbyListener), typeof(ClientLoadJoinResponseMessage)],
                    MethodType.Constructor),
            ];
        }

        public static void Postfix(object __instance)
        {
            switch (__instance)
            {
                case StartRunLobby start:
                    RitsuLibSidecarSessionManager.ObserveNetService(start.NetService);
                    RitsuLibSidecarConnectionExchange.TrySendClientHelloIfReachable(start.NetService);
                    break;
                case LoadRunLobby load:
                    RitsuLibSidecarSessionManager.ObserveNetService(load.NetService);
                    RitsuLibSidecarConnectionExchange.TrySendClientHelloIfReachable(load.NetService);
                    break;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Tracks clients connected to the host so reachability providers can evaluate sidecar support.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         跟踪连接到主机的客户端，使可达性提供方能够判断其是否支持 sidecar。
    ///     </para>
    /// </summary>
    internal sealed class RitsuLibSidecarStartRunLobbyHostClientConnectedPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_lobby_hello_host_client_connected";

        public static bool IsCritical => false;

        public static string Description => "Sidecar peer connect tracking in StartRunLobby host path";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(StartRunLobby), "OnConnectedToClientAsHost", [typeof(ulong)])];
        }

        public static void Postfix(ulong playerId)
        {
            RitsuLibSidecarSessionManager.NotePeerConnected(playerId);
        }
    }

    internal sealed class RitsuLibSidecarStartRunLobbyHostClientDisconnectedPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_lobby_peer_disconnected";
        public static bool IsCritical => false;
        public static string Description => "Sidecar peer disconnect tracking in StartRunLobby host path";

        public static ModPatchTarget[] GetTargets()
        {
            return
                [new(typeof(StartRunLobby), "OnDisconnectedFromClientAsHost", [typeof(ulong), typeof(NetErrorInfo)])];
        }

        public static void Prefix(ulong playerId)
        {
            RitsuLibSidecarSessionManager.NotePeerDisconnected(playerId);
        }
    }
}
