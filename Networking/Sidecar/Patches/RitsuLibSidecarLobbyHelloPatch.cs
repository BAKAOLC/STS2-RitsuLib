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
    ///     After lobby construction, updates session manager with the active net service so capability providers can
    ///     decide peer reachability before any sidecar payload is sent.
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

        // ReSharper disable once InconsistentNaming
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
    ///     Tracks newly connected host peers so reachability providers can evaluate sidecar support.
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

        // ReSharper disable once InconsistentNaming
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

        // ReSharper disable once InconsistentNaming
        public static void Prefix(ulong playerId)
        {
            RitsuLibSidecarSessionManager.NotePeerDisconnected(playerId);
        }
    }
}
