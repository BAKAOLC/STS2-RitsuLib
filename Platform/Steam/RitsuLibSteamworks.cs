using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

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

        internal static bool CanPublishLobbyMemberData(ulong lobbyId, ulong memberId)
        {
            if (lobbyId == 0 || memberId == 0)
                return false;

            var bindings = LazyBindings.Value;
            return bindings != null && bindings.CanPublishLobbyMemberData(lobbyId, memberId);
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
                var cSteamId = ResolveGameSteamIdType();
                if (cSteamId == null)
                    return null;

                var steamworksAssembly = cSteamId.Assembly;
                var remoteStorage = steamworksAssembly.GetType("Steamworks.SteamRemoteStorage", false);
                var matchmaking = steamworksAssembly.GetType("Steamworks.SteamMatchmaking", false);
                if (remoteStorage == null || matchmaking == null)
                    return null;

                var cSteamIdCtor = cSteamId.GetConstructor([typeof(ulong)]);
                var steamIdValue = cSteamId.GetField("m_SteamID", BindingFlags.Public | BindingFlags.Instance);
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
                    steamIdValue == null ||
                    getRemoteFileCount == null ||
                    getRemoteFileNameAndSize == null ||
                    getNumLobbyMembers == null ||
                    getLobbyMemberByIndex == null ||
                    setLobbyMemberData == null ||
                    getLobbyMemberData == null)
                    return null;

                return new(
                    CreateGetNumLobbyMembersDelegate(cSteamIdCtor, getNumLobbyMembers),
                    () => (int)getRemoteFileCount.Invoke(null, null)!,
                    index =>
                    {
                        object?[] args = [index, 0];
                        return getRemoteFileNameAndSize.Invoke(null, args) as string;
                    },
                    CreateGetLobbyMemberByIndexDelegate(cSteamIdCtor, steamIdValue, getLobbyMemberByIndex),
                    CreateSetLobbyMemberDataDelegate(cSteamIdCtor, setLobbyMemberData),
                    CreateGetLobbyMemberDataDelegate(cSteamIdCtor, getLobbyMemberData));
            }
            catch
            {
                return null;
            }
        }

        private static Type? ResolveGameSteamIdType()
        {
            var transportAssembly = typeof(NetTransferMode).Assembly;
            var steamHost = transportAssembly.GetType(
                "MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamHost",
                false);
            var steamClient = transportAssembly.GetType(
                "MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamClient",
                false);
            if (steamHost == null || steamClient == null)
                return null;

            var hostSteamId = ResolveLobbyIdType(steamHost);
            var clientSteamId = ResolveLobbyIdType(steamClient);
            if (hostSteamId == null || clientSteamId == null || hostSteamId != clientSteamId)
                return null;

            return hostSteamId.FullName == "Steamworks.CSteamID" ? hostSteamId : null;
        }

        private static Type? ResolveLobbyIdType(Type transportType)
        {
            var property = transportType.GetProperty(
                "LobbyId",
                BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return null;

            return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        }

        private static Func<ulong, int> CreateGetNumLobbyMembersDelegate(
            ConstructorInfo cSteamIdCtor,
            MethodInfo getNumLobbyMembers)
        {
            var lobbyId = Expression.Parameter(typeof(ulong), "lobbyId");
            var lobby = Expression.New(cSteamIdCtor, lobbyId);
            var call = Expression.Call(getNumLobbyMembers, lobby);
            return Expression.Lambda<Func<ulong, int>>(call, lobbyId).Compile();
        }

        private static Func<ulong, int, ulong> CreateGetLobbyMemberByIndexDelegate(
            ConstructorInfo cSteamIdCtor,
            FieldInfo steamIdValue,
            MethodInfo getLobbyMemberByIndex)
        {
            var lobbyId = Expression.Parameter(typeof(ulong), "lobbyId");
            var index = Expression.Parameter(typeof(int), "index");
            var lobby = Expression.New(cSteamIdCtor, lobbyId);
            var call = Expression.Call(getLobbyMemberByIndex, lobby, index);
            var value = Expression.Field(call, steamIdValue);
            return Expression.Lambda<Func<ulong, int, ulong>>(value, lobbyId, index).Compile();
        }

        private static Func<ulong, string, string, bool> CreateSetLobbyMemberDataDelegate(
            ConstructorInfo cSteamIdCtor,
            MethodInfo setLobbyMemberData)
        {
            var lobbyId = Expression.Parameter(typeof(ulong), "lobbyId");
            var key = Expression.Parameter(typeof(string), "key");
            var value = Expression.Parameter(typeof(string), "value");
            var lobby = Expression.New(cSteamIdCtor, lobbyId);
            var call = Expression.Call(setLobbyMemberData, lobby, key, value);
            return Expression.Lambda<Func<ulong, string, string, bool>>(call, lobbyId, key, value).Compile();
        }

        private static Func<ulong, ulong, string, string?> CreateGetLobbyMemberDataDelegate(
            ConstructorInfo cSteamIdCtor,
            MethodInfo getLobbyMemberData)
        {
            var lobbyId = Expression.Parameter(typeof(ulong), "lobbyId");
            var memberId = Expression.Parameter(typeof(ulong), "memberId");
            var key = Expression.Parameter(typeof(string), "key");
            var lobby = Expression.New(cSteamIdCtor, lobbyId);
            var member = Expression.New(cSteamIdCtor, memberId);
            var call = Expression.Call(getLobbyMemberData, lobby, member, key);
            return Expression.Lambda<Func<ulong, ulong, string, string?>>(call, lobbyId, memberId, key).Compile();
        }

        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
        private sealed class Bindings(
            Func<ulong, int> getNumLobbyMembers,
            Func<int> getRemoteFileCount,
            Func<int, string?> getRemoteFileNameAndSize,
            Func<ulong, int, ulong> getLobbyMemberByIndex,
            Func<ulong, string, string, bool> setLobbyMemberData,
            Func<ulong, ulong, string, string?> getLobbyMemberData)
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
                    count = getNumLobbyMembers(lobbyId);
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
                    var count = getNumLobbyMembers(lobbyId);
                    for (var i = 0; i < count; i++)
                    {
                        var current = getLobbyMemberByIndex(lobbyId, i);
                        if (current != memberId) continue;
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
                    return setLobbyMemberData(lobbyId, key, value);
                }
                catch
                {
                    return false;
                }
            }

            internal bool CanPublishLobbyMemberData(ulong lobbyId, ulong memberId)
            {
                try
                {
                    var count = getNumLobbyMembers(lobbyId);
                    if (count <= 0)
                        return false;

                    for (var i = 0; i < count; i++)
                        if (getLobbyMemberByIndex(lobbyId, i) == memberId)
                            return true;

                    return false;
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
                    value = getLobbyMemberData(lobbyId, memberId, key) ?? string.Empty;
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
