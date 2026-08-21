namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarEndpointRegistry
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<RitsuLibSidecarEndpointKey, RitsuLibSidecarEndpointRegistration>
            Registrations = [];

        internal static RitsuLibSidecarEndpointHandle Register(
            RitsuLibSidecarEndpointDescriptor descriptor,
            Action<RitsuLibSidecarEndpointMessage> handler)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(handler);
            if (descriptor.DeliveryProfile == RitsuLibSidecarDeliveryProfile.BulkStream)
                throw new ArgumentException(
                    "Bulk-stream endpoints must be registered through RitsuLibSidecarEndpoints.RegisterBulk.",
                    nameof(descriptor));
            RitsuLibSidecarEndpointProtocol.EnsureRegistered();

            RitsuLibSidecarEndpointRegistration registration;
            lock (Gate)
            {
                if (Registrations.Count >= RitsuLibSidecarEndpointPolicy.MaxLocalEndpoints)
                    throw new InvalidOperationException(
                        $"No more than {RitsuLibSidecarEndpointPolicy.MaxLocalEndpoints} routed endpoints may be registered.");
                var key = new RitsuLibSidecarEndpointKey(descriptor.OwnerId, descriptor.Name);
                if (Registrations.ContainsKey(key))
                    throw new InvalidOperationException(
                        $"Endpoint '{descriptor.OwnerId}/{descriptor.Name}' is already registered.");
                registration = new(descriptor, handler);
                Registrations.Add(key, registration);
            }

            RitsuLibSidecarEndpointProtocol.OnLocalCatalogChanged();
            return registration.Handle!;
        }

        internal static RitsuLibSidecarBulkEndpointHandle RegisterBulk(
            RitsuLibSidecarEndpointDescriptor descriptor,
            RitsuLibSidecarBulkStreamOptions options,
            Func<RitsuLibSidecarBulkStreamOffer, RitsuLibSidecarBulkReceiveTarget?> handler)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(handler);
            if (descriptor.DeliveryProfile != RitsuLibSidecarDeliveryProfile.BulkStream)
                throw new ArgumentException(
                    "A bulk-stream registration requires the BulkStream delivery profile.",
                    nameof(descriptor));
            RitsuLibSidecarEndpointProtocol.EnsureRegistered();

            RitsuLibSidecarEndpointRegistration registration;
            lock (Gate)
            {
                if (Registrations.Count >= RitsuLibSidecarEndpointPolicy.MaxLocalEndpoints)
                    throw new InvalidOperationException(
                        $"No more than {RitsuLibSidecarEndpointPolicy.MaxLocalEndpoints} routed endpoints may be registered.");
                var key = new RitsuLibSidecarEndpointKey(descriptor.OwnerId, descriptor.Name);
                if (Registrations.ContainsKey(key))
                    throw new InvalidOperationException(
                        $"Endpoint '{descriptor.OwnerId}/{descriptor.Name}' is already registered.");
                registration = new(descriptor, options, handler);
                Registrations.Add(key, registration);
            }

            RitsuLibSidecarEndpointProtocol.OnLocalCatalogChanged();
            return registration.BulkHandle!;
        }

        internal static void Unregister(RitsuLibSidecarEndpointRegistration registration)
        {
            var removed = false;
            lock (Gate)
            {
                var key = new RitsuLibSidecarEndpointKey(
                    registration.Descriptor.OwnerId,
                    registration.Descriptor.Name);
                if (Registrations.TryGetValue(key, out var current) && ReferenceEquals(current, registration))
                    removed = Registrations.Remove(key);
            }

            if (removed)
                RitsuLibSidecarEndpointProtocol.OnLocalCatalogChanged();
        }

        internal static bool TryGet(
            RitsuLibSidecarEndpointKey key,
            out RitsuLibSidecarEndpointRegistration? registration)
        {
            lock (Gate)
            {
                return Registrations.TryGetValue(key, out registration);
            }
        }

        internal static RitsuLibSidecarEndpointAdvertisement[] GetAdvertisementsSnapshot()
        {
            lock (Gate)
            {
                return
                [
                    .. Registrations.Values
                        .Where(static registration => !registration.IsDisposed)
                        .Select(static registration => new RitsuLibSidecarEndpointAdvertisement(
                            new(
                                registration.Descriptor.OwnerId,
                                registration.Descriptor.Name),
                            registration.Descriptor.ProtocolVersion,
                            registration.Descriptor.MinimumCompatibleProtocolVersion,
                            registration.Descriptor.DeliveryProfile,
                            registration.Descriptor.Topology,
                            registration.Descriptor.MaxPayloadBytes)),
                ];
            }
        }

        internal static RitsuLibSidecarEndpointRegistration[] GetRegistrationsSnapshot()
        {
            lock (Gate)
            {
                return [.. Registrations.Values.Where(static registration => !registration.IsDisposed)];
            }
        }

        internal static void TickBulkTransfers()
        {
            foreach (var registration in GetRegistrationsSnapshot())
                registration.BulkTransfers?.Tick();
        }
    }
}
