using HarmonyLib;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     After lobby construction, sends sidecar handshake on that lobby’s <see cref="INetGameService" /> so traffic
    ///     can run before <see cref="MegaCrit.Sts2.Core.Runs.RunManager.NetService" /> exists.
    /// </summary>
    internal sealed class RitsuLibSidecarLobbyHelloPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_lobby_hello";

        public static bool IsCritical => false;

        public static string Description => "Sidecar handshake after StartRunLobby / LoadRunLobby construction";

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
                    RitsuLibSidecarConnectionExchange.TrySendHelloForNetService(start.NetService);
                    break;
                case LoadRunLobby load:
                    RitsuLibSidecarConnectionExchange.TrySendHelloForNetService(load.NetService);
                    break;
            }
        }
    }

    /// <summary>
    ///     When the host’s lobby already existed with zero clients, ctor-time handshake had no peers; send again after the
    ///     first (or each) remote client connects.
    /// </summary>
    internal sealed class RitsuLibSidecarStartRunLobbyHostClientConnectedPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_lobby_hello_host_client_connected";

        public static bool IsCritical => false;

        public static string Description => "Sidecar handshake after StartRunLobby host receives a client connection";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(StartRunLobby), "OnConnectedToClientAsHost", [typeof(ulong)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(StartRunLobby __instance, ulong playerId)
        {
            _ = playerId;
            RitsuLibSidecarConnectionExchange.TrySendHelloForNetService(__instance.NetService);
        }
    }
}
