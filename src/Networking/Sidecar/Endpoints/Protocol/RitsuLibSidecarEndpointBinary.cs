using System.Buffers.Binary;
using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarEndpointBinary
    {
        internal const byte Version = 1;
        internal const int IngressHeaderSize = 26;
        internal const int DeliveryHeaderSize = 25;
        internal const int RouteSnapshotAckSize = 5;

        private const int CatalogHeaderSize = 4;
        private const int RouteSnapshotHeaderSize = 7;
        private const int AdvertisementFixedSize = 12;
        private const int RouteFixedSize = 23;

        internal static byte[] WriteCatalog(RitsuLibSidecarEndpointCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog.Endpoints);
            if (catalog.Endpoints.Count > RitsuLibSidecarEndpointPolicy.MaxCatalogEndpointsPerPeer)
                throw new ArgumentOutOfRangeException(nameof(catalog), "Endpoint catalog contains too many entries.");
            if (catalog.Endpoints.Select(static endpoint => endpoint.Key).Distinct().Count() !=
                catalog.Endpoints.Count)
                throw new ArgumentException("Endpoint catalog contains duplicate keys.", nameof(catalog));

            var encoded = catalog.Endpoints.Select(EncodeAdvertisement).ToArray();
            var length = CatalogHeaderSize + encoded.Sum(static item => item.Length);
            if (length > RitsuLibSidecarEndpointPolicy.MaxCatalogPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(catalog), "Endpoint catalog exceeds the payload limit.");

            var output = new byte[length];
            var span = output.AsSpan();
            span[0] = Version;
            span[1] = (byte)catalog.SupportedProfiles;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), (ushort)encoded.Length);
            var offset = CatalogHeaderSize;
            foreach (var item in encoded)
            {
                item.CopyTo(span[offset..]);
                offset += item.Length;
            }

            return output;
        }

        internal static bool TryReadCatalog(ReadOnlySpan<byte> source, out RitsuLibSidecarEndpointCatalog catalog)
        {
            catalog = default;
            if (source.Length is < CatalogHeaderSize or > RitsuLibSidecarEndpointPolicy.MaxCatalogPayloadBytes ||
                source[0] != Version)
                return false;

            var supportedProfiles = (RitsuLibSidecarTransportProfileMask)source[1];
            if ((supportedProfiles & ~(RitsuLibSidecarTransportProfileMask.Control |
                                       RitsuLibSidecarTransportProfileMask.RealtimeDatagram |
                                       RitsuLibSidecarTransportProfileMask.BulkStream)) != 0)
                return false;

            var count = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(2, 2));
            if (count > RitsuLibSidecarEndpointPolicy.MaxCatalogEndpointsPerPeer)
                return false;

            var endpoints = new RitsuLibSidecarEndpointAdvertisement[count];
            var offset = CatalogHeaderSize;
            var keys = new HashSet<RitsuLibSidecarEndpointKey>();
            for (var i = 0; i < count; i++)
            {
                if (!TryReadAdvertisement(source, ref offset, out var advertisement) ||
                    !keys.Add(advertisement.Key))
                    return false;
                endpoints[i] = advertisement;
            }

            if (offset != source.Length)
                return false;

            catalog = new(supportedProfiles, endpoints);
            return true;
        }

        internal static byte[] WriteRouteSnapshot(RitsuLibSidecarEndpointRouteSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot.Routes);
            ArgumentOutOfRangeException.ThrowIfZero(snapshot.Revision);
            if (snapshot.Routes.Count > RitsuLibSidecarEndpointPolicy.MaxHostRoutes)
                throw new ArgumentOutOfRangeException(nameof(snapshot), "Route snapshot contains too many routes.");
            if (snapshot.Routes.Select(static route => route.RouteId).Distinct().Count() != snapshot.Routes.Count ||
                snapshot.Routes.Select(static route => route.Key).Distinct().Count() != snapshot.Routes.Count)
                throw new ArgumentException("Route snapshot contains duplicate route IDs or keys.", nameof(snapshot));

            var encoded = snapshot.Routes.Select(EncodeRoute).ToArray();
            var length = RouteSnapshotHeaderSize + encoded.Sum(static item => item.Length);
            if (length > RitsuLibSidecarEndpointPolicy.MaxRouteSnapshotPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(snapshot), "Route snapshot exceeds the payload limit.");

            var output = new byte[length];
            var span = output.AsSpan();
            span[0] = Version;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1, 4), snapshot.Revision);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(5, 2), (ushort)encoded.Length);
            var offset = RouteSnapshotHeaderSize;
            foreach (var item in encoded)
            {
                item.CopyTo(span[offset..]);
                offset += item.Length;
            }

            return output;
        }

        internal static bool TryReadRouteSnapshot(
            ReadOnlySpan<byte> source,
            out RitsuLibSidecarEndpointRouteSnapshot snapshot)
        {
            snapshot = default;
            if (source.Length is < RouteSnapshotHeaderSize
                    or > RitsuLibSidecarEndpointPolicy.MaxRouteSnapshotPayloadBytes ||
                source[0] != Version)
                return false;

            var revision = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1, 4));
            var count = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(5, 2));
            if (revision == 0 || count > RitsuLibSidecarEndpointPolicy.MaxHostRoutes)
                return false;

            var routes = new RitsuLibSidecarEndpointRouteDefinition[count];
            var routeIds = new HashSet<uint>();
            var keys = new HashSet<RitsuLibSidecarEndpointKey>();
            var offset = RouteSnapshotHeaderSize;
            for (var i = 0; i < count; i++)
            {
                if (!TryReadRoute(source, ref offset, out var route) ||
                    !routeIds.Add(route.RouteId) ||
                    !keys.Add(route.Key))
                    return false;
                routes[i] = route;
            }

            if (offset != source.Length)
                return false;

            snapshot = new(revision, routes);
            return true;
        }

        internal static byte[] WriteRouteSnapshotAck(uint revision)
        {
            ArgumentOutOfRangeException.ThrowIfZero(revision);
            var output = new byte[RouteSnapshotAckSize];
            output[0] = Version;
            BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(1, 4), revision);
            return output;
        }

        internal static bool TryReadRouteSnapshotAck(ReadOnlySpan<byte> source, out uint revision)
        {
            revision = 0;
            if (source.Length != RouteSnapshotAckSize || source[0] != Version)
                return false;
            revision = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1, 4));
            return revision != 0;
        }

        internal static byte[] WriteIngressFrame(
            uint routeId,
            ulong nonce,
            uint sequence,
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId,
            ReadOnlySpan<byte> payload)
        {
            ArgumentOutOfRangeException.ThrowIfZero(routeId);
            ArgumentOutOfRangeException.ThrowIfZero(nonce);
            if (!Enum.IsDefined(destination))
                throw new ArgumentOutOfRangeException(nameof(destination), destination,
                    "Invalid endpoint destination.");
            if (destination == RitsuLibSidecarEndpointDestination.Peer && targetNetId == 0 ||
                destination != RitsuLibSidecarEndpointDestination.Peer && targetNetId != 0)
                throw new ArgumentException("Target peer does not match the endpoint destination.",
                    nameof(targetNetId));
            if (payload.Length > RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(payload), "Endpoint payload exceeds the absolute limit.");

            var output = new byte[IngressHeaderSize + payload.Length];
            var span = output.AsSpan();
            span[0] = Version;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1, 4), routeId);
            BinaryPrimitives.WriteUInt64BigEndian(span.Slice(5, 8), nonce);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(13, 4), sequence);
            span[17] = (byte)destination;
            BinaryPrimitives.WriteUInt64BigEndian(span.Slice(18, 8), targetNetId);
            payload.CopyTo(span[IngressHeaderSize..]);
            return output;
        }

        internal static bool TryReadIngressFrame(
            ReadOnlyMemory<byte> source,
            out RitsuLibSidecarEndpointIngressFrame frame)
        {
            frame = default;
            if (source.Length < IngressHeaderSize ||
                source.Length - IngressHeaderSize > RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes ||
                source.Span[0] != Version)
                return false;

            var span = source.Span;
            var routeId = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(1, 4));
            var nonce = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(5, 8));
            if (routeId == 0 || nonce == 0)
                return false;
            var destination = (RitsuLibSidecarEndpointDestination)span[17];
            if (!Enum.IsDefined(destination))
                return false;
            var targetNetId = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(18, 8));
            if (destination == RitsuLibSidecarEndpointDestination.Peer && targetNetId == 0 ||
                destination != RitsuLibSidecarEndpointDestination.Peer && targetNetId != 0)
                return false;

            frame = new(
                routeId,
                nonce,
                BinaryPrimitives.ReadUInt32BigEndian(span.Slice(13, 4)),
                destination,
                targetNetId,
                source[IngressHeaderSize..]);
            return true;
        }

        internal static byte[] WriteDeliveryFrame(
            uint routeId,
            ulong nonce,
            uint sequence,
            ulong originalSenderNetId,
            ReadOnlySpan<byte> payload)
        {
            ArgumentOutOfRangeException.ThrowIfZero(routeId);
            ArgumentOutOfRangeException.ThrowIfZero(nonce);
            ArgumentOutOfRangeException.ThrowIfZero(originalSenderNetId);
            if (payload.Length > RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(payload), "Endpoint payload exceeds the absolute limit.");

            var output = new byte[DeliveryHeaderSize + payload.Length];
            var span = output.AsSpan();
            span[0] = Version;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1, 4), routeId);
            BinaryPrimitives.WriteUInt64BigEndian(span.Slice(5, 8), nonce);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(13, 4), sequence);
            BinaryPrimitives.WriteUInt64BigEndian(span.Slice(17, 8), originalSenderNetId);
            payload.CopyTo(span[DeliveryHeaderSize..]);
            return output;
        }

        internal static bool TryReadDeliveryFrame(
            ReadOnlyMemory<byte> source,
            out RitsuLibSidecarEndpointDeliveryFrame frame)
        {
            frame = default;
            if (source.Length < DeliveryHeaderSize ||
                source.Length - DeliveryHeaderSize > RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes ||
                source.Span[0] != Version)
                return false;

            var span = source.Span;
            var routeId = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(1, 4));
            var nonce = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(5, 8));
            if (routeId == 0 || nonce == 0)
                return false;
            var originalSenderNetId = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(17, 8));
            if (originalSenderNetId == 0)
                return false;

            frame = new(
                routeId,
                nonce,
                BinaryPrimitives.ReadUInt32BigEndian(span.Slice(13, 4)),
                originalSenderNetId,
                source[DeliveryHeaderSize..]);
            return true;
        }

        private static byte[] EncodeAdvertisement(RitsuLibSidecarEndpointAdvertisement advertisement)
        {
            ValidateAdvertisement(advertisement);
            var owner = Encoding.UTF8.GetBytes(advertisement.Key.OwnerId);
            var name = Encoding.UTF8.GetBytes(advertisement.Key.Name);
            var output = new byte[AdvertisementFixedSize + owner.Length + name.Length];
            var span = output.AsSpan();
            span[0] = (byte)owner.Length;
            owner.CopyTo(span[1..]);
            var offset = 1 + owner.Length;
            span[offset++] = (byte)name.Length;
            name.CopyTo(span[offset..]);
            offset += name.Length;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset, 2), advertisement.ProtocolVersion);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(
                span.Slice(offset, 2),
                advertisement.MinimumCompatibleProtocolVersion);
            offset += 2;
            span[offset++] = (byte)advertisement.DeliveryProfile;
            span[offset++] = (byte)advertisement.Topology;
            BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, 4), advertisement.MaxPayloadBytes);
            return output;
        }

        private static bool TryReadAdvertisement(
            ReadOnlySpan<byte> source,
            ref int offset,
            out RitsuLibSidecarEndpointAdvertisement advertisement)
        {
            advertisement = default;
            if (!TryReadIdentifier(source, ref offset, false, out var owner) ||
                !TryReadIdentifier(source, ref offset, true, out var name) ||
                source.Length - offset < AdvertisementFixedSize - 2)
                return false;

            var protocolVersion = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(offset, 2));
            offset += 2;
            var minimumCompatibleProtocolVersion = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(offset, 2));
            offset += 2;
            var deliveryProfile = (RitsuLibSidecarDeliveryProfile)source[offset++];
            var topology = (RitsuLibSidecarEndpointTopology)source[offset++];
            var maxPayloadBytes = BinaryPrimitives.ReadInt32BigEndian(source.Slice(offset, 4));
            offset += 4;
            advertisement = new(
                new(owner, name),
                protocolVersion,
                minimumCompatibleProtocolVersion,
                deliveryProfile,
                topology,
                maxPayloadBytes);
            return IsValidAdvertisement(advertisement);
        }

        private static byte[] EncodeRoute(RitsuLibSidecarEndpointRouteDefinition route)
        {
            ValidateRoute(route);
            var owner = Encoding.UTF8.GetBytes(route.Key.OwnerId);
            var name = Encoding.UTF8.GetBytes(route.Key.Name);
            var output = new byte[RouteFixedSize + owner.Length + name.Length +
                                  route.ParticipantNetIds.Count * RitsuLibSidecarBinaryLayout.U64Size];
            var span = output.AsSpan();
            BinaryPrimitives.WriteUInt32BigEndian(span[..4], route.RouteId);
            BinaryPrimitives.WriteUInt64BigEndian(span.Slice(4, 8), route.Nonce);
            span[12] = (byte)owner.Length;
            owner.CopyTo(span[13..]);
            var offset = 13 + owner.Length;
            span[offset++] = (byte)name.Length;
            name.CopyTo(span[offset..]);
            offset += name.Length;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset, 2), route.ProtocolVersion);
            offset += 2;
            span[offset++] = (byte)route.DeliveryProfile;
            span[offset++] = (byte)route.Topology;
            BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, 4), route.MaxPayloadBytes);
            offset += 4;
            span[offset++] = (byte)route.ParticipantNetIds.Count;
            foreach (var participant in route.ParticipantNetIds)
            {
                BinaryPrimitives.WriteUInt64BigEndian(span.Slice(offset, 8), participant);
                offset += 8;
            }

            return output;
        }

        private static bool TryReadRoute(
            ReadOnlySpan<byte> source,
            ref int offset,
            out RitsuLibSidecarEndpointRouteDefinition route)
        {
            route = default;
            if (source.Length - offset < 13)
                return false;
            var routeId = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(offset, 4));
            offset += 4;
            var nonce = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(offset, 8));
            offset += 8;
            if (!TryReadIdentifier(source, ref offset, false, out var owner) ||
                !TryReadIdentifier(source, ref offset, true, out var name) ||
                source.Length - offset < RouteFixedSize - 14)
                return false;

            var protocolVersion = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(offset, 2));
            offset += 2;
            var deliveryProfile = (RitsuLibSidecarDeliveryProfile)source[offset++];
            var topology = (RitsuLibSidecarEndpointTopology)source[offset++];
            var maxPayloadBytes = BinaryPrimitives.ReadInt32BigEndian(source.Slice(offset, 4));
            offset += 4;
            var participantCount = source[offset++];
            if (participantCount is 0 or > RitsuLibSidecarEndpointPolicy.MaxRouteParticipants ||
                source.Length - offset < participantCount * RitsuLibSidecarBinaryLayout.U64Size)
                return false;

            var participants = new ulong[participantCount];
            var uniqueParticipants = new HashSet<ulong>();
            for (var i = 0; i < participantCount; i++)
            {
                var participant = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(offset, 8));
                offset += 8;
                if (participant == 0 || !uniqueParticipants.Add(participant))
                    return false;
                participants[i] = participant;
            }

            route = new(
                routeId,
                nonce,
                new(owner, name),
                protocolVersion,
                deliveryProfile,
                topology,
                maxPayloadBytes,
                participants);
            return IsValidRoute(route);
        }

        private static bool TryReadIdentifier(
            ReadOnlySpan<byte> source,
            ref int offset,
            bool allowSlash,
            out string value)
        {
            value = string.Empty;
            if (offset >= source.Length)
                return false;
            var length = source[offset++];
            if (length == 0 || length > RitsuLibSidecarEndpointPolicy.MaxIdentifierUtf8Bytes ||
                source.Length - offset < length)
                return false;

            try
            {
                value = new UTF8Encoding(false, true).GetString(source.Slice(offset, length));
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            offset += length;
            return IsValidIdentifier(value, allowSlash);
        }

        private static void ValidateAdvertisement(RitsuLibSidecarEndpointAdvertisement advertisement)
        {
            if (!IsValidAdvertisement(advertisement))
                throw new ArgumentException("Invalid endpoint advertisement.", nameof(advertisement));
        }

        private static bool IsValidAdvertisement(RitsuLibSidecarEndpointAdvertisement advertisement)
        {
            return IsValidIdentifier(advertisement.Key.OwnerId, false) &&
                   IsValidIdentifier(advertisement.Key.Name, true) &&
                   advertisement.ProtocolVersion != 0 &&
                   advertisement.MinimumCompatibleProtocolVersion != 0 &&
                   advertisement.MinimumCompatibleProtocolVersion <= advertisement.ProtocolVersion &&
                   Enum.IsDefined(advertisement.DeliveryProfile) &&
                   Enum.IsDefined(advertisement.Topology) &&
                   advertisement.MaxPayloadBytes > 0 &&
                   advertisement.MaxPayloadBytes <= MaximumPayload(advertisement.DeliveryProfile);
        }

        private static void ValidateRoute(RitsuLibSidecarEndpointRouteDefinition route)
        {
            if (!IsValidRoute(route))
                throw new ArgumentException("Invalid endpoint route.", nameof(route));
        }

        private static bool IsValidRoute(RitsuLibSidecarEndpointRouteDefinition route)
        {
            return route.RouteId != 0 &&
                   route.Nonce != 0 &&
                   IsValidIdentifier(route.Key.OwnerId, false) &&
                   IsValidIdentifier(route.Key.Name, true) &&
                   route.ProtocolVersion != 0 &&
                   Enum.IsDefined(route.DeliveryProfile) &&
                   Enum.IsDefined(route.Topology) &&
                   route.MaxPayloadBytes > 0 &&
                   route.MaxPayloadBytes <= MaximumPayload(route.DeliveryProfile) &&
                   route.ParticipantNetIds is
                       { Count: > 0 and <= RitsuLibSidecarEndpointPolicy.MaxRouteParticipants } &&
                   route.ParticipantNetIds.All(static participant => participant != 0) &&
                   route.ParticipantNetIds.Distinct().Count() == route.ParticipantNetIds.Count;
        }

        private static int MaximumPayload(RitsuLibSidecarDeliveryProfile profile)
        {
            return profile switch
            {
                RitsuLibSidecarDeliveryProfile.Control => RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes,
                RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                    RitsuLibSidecarEndpointPolicy.MaxRealtimePayloadBytes,
                RitsuLibSidecarDeliveryProfile.BulkStream =>
                    RitsuLibSidecarEndpointPolicy.MaxBulkPayloadBytes,
                _ => 0,
            };
        }

        private static bool IsValidIdentifier(string value, bool allowSlash)
        {
            if (string.IsNullOrEmpty(value) ||
                Encoding.UTF8.GetByteCount(value) > RitsuLibSidecarEndpointPolicy.MaxIdentifierUtf8Bytes)
                return false;
            return value.All(character =>
                character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-' ||
                allowSlash && character == '/');
        }
    }
}
