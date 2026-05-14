using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class RitsuLibSteamworks
    {
        private static readonly Lazy<Bindings?> LazyBindings = new(CreateBindings);

        internal static bool IsAvailable => LazyBindings.Value != null;

        internal static bool TryGetRemoteFileCount(out int count)
        {
            count = 0;
            var bindings = LazyBindings.Value;
            return bindings != null && bindings.TryGetRemoteFileCount(out count);
        }

        internal static bool TryGetRemoteFileNameAndSize(int index, out string path)
        {
            path = string.Empty;
            var bindings = LazyBindings.Value;
            return bindings != null && bindings.TryGetRemoteFileNameAndSize(index, out path);
        }

        internal static bool TryGetNumLobbyMembers(ulong lobbyId, out int count)
        {
            count = 0;
            var bindings = LazyBindings.Value;
            return bindings != null && bindings.TryGetNumLobbyMembers(lobbyId, out count);
        }

        internal static bool TryLobbyContainsMember(ulong lobbyId, ulong memberId, out bool contains)
        {
            contains = false;
            var bindings = LazyBindings.Value;
            return bindings != null && bindings.TryLobbyContainsMember(lobbyId, memberId, out contains);
        }

        internal static bool TrySetLobbyMemberData(ulong lobbyId, string key, string value)
        {
            var bindings = LazyBindings.Value;
            return bindings != null && bindings.TrySetLobbyMemberData(lobbyId, key, value);
        }

        internal static bool TryGetLobbyMemberData(ulong lobbyId, ulong memberId, string key, out string value)
        {
            value = string.Empty;
            var bindings = LazyBindings.Value;
            return bindings != null && bindings.TryGetLobbyMemberData(lobbyId, memberId, key, out value);
        }

        private static Bindings? CreateBindings()
        {
            if (RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration)
                return null;

            try
            {
                var cSteamId = Type.GetType("Steamworks.CSteamID, Steamworks.NET", false);
                var remoteStorage = Type.GetType("Steamworks.SteamRemoteStorage, Steamworks.NET", false);
                var matchmaking = Type.GetType("Steamworks.SteamMatchmaking, Steamworks.NET", false);
                if (cSteamId == null || remoteStorage == null || matchmaking == null)
                    return null;

                var cSteamIdCtor = cSteamId.GetConstructor([typeof(ulong)]);
                var getRemoteFileCount = remoteStorage.GetMethod(
                    "GetFileCount",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);
                var getRemoteFileNameAndSize = remoteStorage.GetMethod(
                    "GetFileNameAndSize",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(int), typeof(int).MakeByRefType()],
                    null);
                var getNumLobbyMembers = matchmaking.GetMethod(
                    "GetNumLobbyMembers",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [cSteamId],
                    null);
                var getLobbyMemberByIndex = matchmaking.GetMethod(
                    "GetLobbyMemberByIndex",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [cSteamId, typeof(int)],
                    null);
                var setLobbyMemberData = matchmaking.GetMethod(
                    "SetLobbyMemberData",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [cSteamId, typeof(string), typeof(string)],
                    null);
                var getLobbyMemberData = matchmaking.GetMethod(
                    "GetLobbyMemberData",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [cSteamId, cSteamId, typeof(string)],
                    null);

                if (cSteamIdCtor == null ||
                    getRemoteFileCount == null ||
                    getRemoteFileNameAndSize == null ||
                    getNumLobbyMembers == null ||
                    getLobbyMemberByIndex == null ||
                    setLobbyMemberData == null ||
                    getLobbyMemberData == null)
                    return null;

                return new(
                    value => cSteamIdCtor.Invoke([value]),
                    () => (int)getRemoteFileCount.Invoke(null, null)!,
                    index =>
                    {
                        object?[] args = [index, 0];
                        return getRemoteFileNameAndSize.Invoke(null, args) as string;
                    },
                    lobby => (int)getNumLobbyMembers.Invoke(null, [lobby])!,
                    (lobby, index) => getLobbyMemberByIndex.Invoke(null, [lobby, index]),
                    (lobby, key, value) => (bool)setLobbyMemberData.Invoke(null, [lobby, key, value])!,
                    (lobby, member, key) => getLobbyMemberData.Invoke(null, [lobby, member, key]) as string);
            }
            catch
            {
                return null;
            }
        }

        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
        private sealed class Bindings(
            Func<ulong, object> createSteamId,
            Func<int> getRemoteFileCount,
            Func<int, string?> getRemoteFileNameAndSize,
            Func<object, int> getNumLobbyMembers,
            Func<object, int, object?> getLobbyMemberByIndex,
            Func<object, string, string, bool> setLobbyMemberData,
            Func<object, object, string, string?> getLobbyMemberData)
        {
            internal bool TryGetRemoteFileCount(out int count)
            {
                count = 0;
                try
                {
                    count = getRemoteFileCount();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            internal bool TryGetRemoteFileNameAndSize(int index, out string path)
            {
                path = string.Empty;
                try
                {
                    path = getRemoteFileNameAndSize(index) ?? string.Empty;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            internal bool TryGetNumLobbyMembers(ulong lobbyId, out int count)
            {
                count = 0;
                try
                {
                    count = getNumLobbyMembers(createSteamId(lobbyId));
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            internal bool TryLobbyContainsMember(ulong lobbyId, ulong memberId, out bool contains)
            {
                contains = false;
                try
                {
                    var lobby = createSteamId(lobbyId);
                    var member = createSteamId(memberId);
                    var count = getNumLobbyMembers(lobby);
                    for (var i = 0; i < count; i++)
                    {
                        var current = getLobbyMemberByIndex(lobby, i);
                        if (!Equals(current, member)) continue;
                        contains = true;
                        return true;
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            internal bool TrySetLobbyMemberData(ulong lobbyId, string key, string value)
            {
                try
                {
                    return setLobbyMemberData(createSteamId(lobbyId), key, value);
                }
                catch
                {
                    return false;
                }
            }

            internal bool TryGetLobbyMemberData(ulong lobbyId, ulong memberId, string key, out string value)
            {
                value = string.Empty;
                try
                {
                    value = getLobbyMemberData(createSteamId(lobbyId), createSteamId(memberId), key) ?? string.Empty;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
