using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.RunData
{
    internal static class RunSavedDataLobbyRuntime
    {
        private static readonly AttachedState<StartRunLobby, RunSavedDataLobbySession> Sessions = new();

        public static RunSavedDataLobbySession GetSession(StartRunLobby lobby)
        {
            ArgumentNullException.ThrowIfNull(lobby);
            return Sessions.GetOrAdd(lobby, _ => new());
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
