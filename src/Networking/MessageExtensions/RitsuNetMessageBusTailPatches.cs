using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.MessageExtensions
{
    internal static class RitsuNetMessageBusTailPatches
    {
        private const int ByteBits = 8;

        private static readonly AccessTools.FieldRef<NetMessageBus, PacketReader>? ReaderRef =
            TryCreateFieldRef<PacketReader>("_reader");

        private static readonly AccessTools.FieldRef<NetMessageBus, PacketWriter>? WriterRef =
            TryCreateFieldRef<PacketWriter>("_writer");

        private static readonly Lock RegistrationLock = new();
        private static readonly Dictionary<Type, TailOwnerRegistration> Registrations = [];
        private static Dictionary<Type, TailOwnerRegistration> _readRegistrations = [];

        internal static void Register<TMessage>(
            string patchId,
            string description,
            Action? beforeRead = null,
            bool isCritical = false)
            where TMessage : INetMessage
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            lock (RegistrationLock)
            {
                if (Registrations.ContainsKey(typeof(TMessage)))
                    throw new InvalidOperationException(
                        $"A RitsuLib message-tail owner is already registered for {typeof(TMessage).FullName}.");

                Registrations.Add(typeof(TMessage), new(
                    typeof(TMessage),
                    patchId,
                    description,
                    isCritical,
                    beforeRead,
                    static reader => RitsuNetMessageTailExtensions.Read<TMessage>(reader)));
                Volatile.Write(ref _readRegistrations, new(Registrations));
            }
        }

        internal static bool ApplySerializePatches(ModPatcher patcher)
        {
            ArgumentNullException.ThrowIfNull(patcher);

            if (WriterRef == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[NetMessageTailExtensions] NetMessageBus._writer is unavailable; message tails cannot be serialized.");
                return true;
            }

            var serializeDefinition = AccessTools.DeclaredMethod(
                typeof(NetMessageBus),
                nameof(NetMessageBus.SerializeMessage));
            if (serializeDefinition is not { IsGenericMethodDefinition: true })
            {
                RitsuLibFramework.Logger.Warn(
                    "[NetMessageTailExtensions] NetMessageBus serialization surface is unavailable; " +
                    "message tails cannot be serialized.");
                return true;
            }

            var registrations = Volatile.Read(ref _readRegistrations).Values;

            var patches = registrations.Select(CreatePatch);
            return patcher.ApplyDynamicPatches(patches, true);

            DynamicPatchInfo CreatePatch(TailOwnerRegistration registration)
            {
                var patchType = typeof(SerializePatch<>).MakeGenericType(registration.MessageType);
                const string postfixName = nameof(SerializePatch<INetMessage>.Postfix);
                var postfix = AccessTools.DeclaredMethod(patchType, postfixName)
                              ?? throw new MissingMethodException(patchType.FullName, postfixName);
                return new(
                    registration.PatchId,
                    serializeDefinition.MakeGenericMethod(registration.MessageType),
                    postfix: new(postfix),
                    isCritical: registration.IsCritical,
                    description: registration.Description);
            }
        }

        internal static void ValidateReaderAccess()
        {
            if (ReaderRef == null)
                throw new MissingFieldException(typeof(NetMessageBus).FullName, "_reader");
        }

        internal static void Read(NetMessageBus messageBus, INetMessage message)
        {
            if (!Volatile.Read(ref _readRegistrations).TryGetValue(message.GetType(), out var registration))
                return;

            registration.BeforeRead?.Invoke();
            var reader = ReaderRef?.Invoke(messageBus)
                         ?? throw new InvalidOperationException("NetMessageBus has no active packet reader.");
            registration.ReadTail(reader);
        }

        private static AccessTools.FieldRef<NetMessageBus, TField>? TryCreateFieldRef<TField>(string fieldName)
        {
            try
            {
                return AccessTools.FieldRefAccess<NetMessageBus, TField>(fieldName);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return null;
            }
        }

        private static class SerializePatch<TMessage> where TMessage : INetMessage
        {
            [HarmonyPriority(Priority.Last)]
            internal static void Postfix(
                NetMessageBus __instance,
                TMessage message,
                ref int length,
                ref byte[] __result)
            {
                var writer = WriterRef?.Invoke(__instance)
                             ?? throw new InvalidOperationException("NetMessageBus has no active packet writer.");
                RitsuNetMessageTailExtensions.Write(writer, message);
                length = checked((int)(((long)writer.BitPosition + ByteBits - 1) / ByteBits));
                __result = writer.Buffer;
            }
        }

        private sealed record TailOwnerRegistration(
            Type MessageType,
            string PatchId,
            string Description,
            bool IsCritical,
            Action? BeforeRead,
            Action<PacketReader> ReadTail);
    }

    internal sealed class RitsuNetMessageBusTailDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_message_tail_bus_deserialize";
        public static bool IsCritical => false;

        public static string Description =>
            "Read RitsuLib-owned message tails at the non-inlined message-bus boundary";

        public static ModPatchTarget[] GetTargets()
        {
            RitsuNetMessageBusTailPatches.ValidateReaderAccess();
            return
            [
                new(
                    typeof(NetMessageBus),
                    nameof(NetMessageBus.TryDeserializeMessage),
                    [typeof(byte[]), typeof(INetMessage).MakeByRefType(), typeof(ulong?).MakeByRefType()]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(NetMessageBus __instance, bool __result, INetMessage? message)
        {
            if (__result && message != null)
                RitsuNetMessageBusTailPatches.Read(__instance, message);
        }
    }
}
