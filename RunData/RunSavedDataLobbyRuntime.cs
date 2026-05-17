using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace STS2RitsuLib.RunData
{
    internal static class RunSavedDataLobbyRuntime
    {
        private static readonly ConditionalWeakTable<StartRunLobby, RunSavedDataLobbySession> Sessions = [];

        public static RunSavedDataLobbySession GetSession(StartRunLobby lobby)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            return Sessions.GetValue(lobby, _ => new());
        }

        public static bool TryGetSession(StartRunLobby lobby, out RunSavedDataLobbySession session)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            return Sessions.TryGetValue(lobby, out session!);
        }

        public static void ClearSession(StartRunLobby lobby)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            if (Sessions.TryGetValue(lobby, out var session))
                session.Clear();
        }

        public static void RemoveSession(StartRunLobby lobby)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            Sessions.Remove(lobby);
        }
    }
}
