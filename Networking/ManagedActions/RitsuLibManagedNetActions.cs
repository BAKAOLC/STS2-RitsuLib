using System.Buffers.Binary;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Networking.ManagedActions
{
    /// <summary>
    ///     Describes a RitsuLib-managed action carried by vanilla action enqueue messages.
    ///     描述一个由原版 action 入队消息承载的 RitsuLib managed action。
    /// </summary>
    /// <param name="ModuleId">Stable owner id for opcode derivation.</param>
    /// <param name="ActionKey">Stable action key for opcode derivation.</param>
    /// <param name="Serialize">Serializes the typed payload.</param>
    /// <param name="Deserialize">Deserializes the typed payload.</param>
    /// <param name="Execute">Runs when the vanilla queue action executes.</param>
    /// <param name="ActionType">Vanilla queue action type.</param>
    public sealed record RitsuLibManagedNetActionDescriptor<T>(
        string ModuleId,
        string ActionKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        Func<RitsuLibManagedNetActionContext<T>, Task> Execute,
        GameActionType ActionType);

    /// <summary>
    ///     Runtime context delivered to a managed net action executor.
    ///     传递给 managed net action 执行器的运行时上下文。
    /// </summary>
    /// <param name="Message">Typed action payload.</param>
    /// <param name="Player">Player that owns the queued action.</param>
    /// <param name="Action">Underlying vanilla queue action.</param>
    /// <param name="PlayerChoiceContext">Queue-backed choice context for command APIs.</param>
    public readonly record struct RitsuLibManagedNetActionContext<T>(
        T Message,
        Player Player,
        RitsuLibManagedGameAction Action,
        GameActionPlayerChoiceContext PlayerChoiceContext);

    /// <summary>
    ///     Registers and requests RitsuLib-managed actions through vanilla action enqueue messages.
    ///     通过原版 action 入队消息注册和请求 RitsuLib managed action。
    /// </summary>
    public static class RitsuLibManagedNetActions
    {
        private const ulong ManagedActionMagic = 0x4E_41_54_52_32_53_54_52; // RTS2RTAN
        private const byte Version = 1;
        private const int InitialOffset = 0;
        private const int ByteBits = 8;
        private const int ManagedActionMagicBits = 64;

        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];

        /// <summary>
        ///     Registers a managed net action descriptor and returns its stable opcode.
        ///     注册 managed net action descriptor，并返回其稳定 opcode。
        /// </summary>
        public static ulong Register<T>(RitsuLibManagedNetActionDescriptor<T> descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.ModuleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.ActionKey);
            ArgumentNullException.ThrowIfNull(descriptor.Serialize);
            ArgumentNullException.ThrowIfNull(descriptor.Deserialize);
            ArgumentNullException.ThrowIfNull(descriptor.Execute);
            ValidateActionType(descriptor.ActionType);

            var opcode = RitsuLibSidecarOpcodes.For(descriptor.ModuleId, descriptor.ActionKey);
            lock (Gate)
            {
                if (Registrations.TryGetValue(opcode, out var existing))
                {
                    if (existing is Registration<T> typed &&
                        typed.ModuleId == descriptor.ModuleId &&
                        typed.ActionKey == descriptor.ActionKey &&
                        typed.ActionType == descriptor.ActionType)
                        return opcode;

                    throw new InvalidOperationException(
                        $"Managed net action opcode conflict: {descriptor.ModuleId}/{descriptor.ActionKey} -> {opcode}");
                }

                Registrations[opcode] = new Registration<T>(
                    descriptor.ModuleId,
                    descriptor.ActionKey,
                    descriptor.Deserialize,
                    descriptor.Execute,
                    descriptor.ActionType);
            }

            return opcode;
        }

        /// <summary>
        ///     Requests a managed action through the vanilla action queue synchronizer.
        ///     通过原版 action queue synchronizer 请求一个 managed action。
        /// </summary>
        public static bool Request<T>(
            RunManager? runManager,
            RitsuLibManagedNetActionDescriptor<T> descriptor,
            T message,
            ulong? ownerNetId = null)
        {
            var opcode = Register(descriptor);
            var rm = runManager ?? RunManager.Instance;
            var net = rm?.NetService;
            var state = rm?.DebugOnlyGetState();
            if (rm == null || net == null || state == null)
                return false;

            if (!CanSendManagedAction(net))
                return false;

            var owner = ownerNetId ?? net.NetId;
            if (owner != net.NetId)
                return false;

            var player = state.Players.FirstOrDefault(p => p.NetId == owner);
            if (player == null)
                return false;

            var payload = descriptor.Serialize(message);
            var action = new RitsuLibManagedGameAction(
                player,
                opcode,
                descriptor.ActionType,
                payload);
            rm.ActionQueueSynchronizer.RequestEnqueue(action);
            return true;
        }

        internal static bool TryWriteNetAction(PacketWriter writer, INetAction action)
        {
            if (action is not RitsuLibManagedNetAction managed)
                return false;

            managed.Serialize(writer);
            return true;
        }

        internal static INetAction ReadNetAction(PacketReader reader)
        {
            if (NextPayloadIsManagedAction(reader))
                return !TryReadManagedActionBody(
                    reader,
                    out var descriptorOpcode,
                    out var actionType,
                    out var payload)
                    ? throw new InvalidOperationException("Malformed RitsuLib managed net action.")
                    : RitsuLibManagedNetActionCarrierFactory.Create(descriptorOpcode, actionType, payload);

            var actionId = reader.ReadByte();
            if (!ActionTypes.TryGetActionType(actionId, out var type))
                throw new InvalidOperationException(
                    $"Received net action of type {actionId} that does not map to any type!");

            var action = (INetAction)Activator.CreateInstance(type!)!;
            action.Deserialize(reader);
            return action;
        }

        internal static void WriteManagedActionBody(
            PacketWriter writer,
            ulong descriptorOpcode,
            GameActionType actionType,
            ReadOnlySpan<byte> payload)
        {
            writer.WriteULong(ManagedActionMagic);
            writer.WriteByte(Version);
            writer.WriteULong(descriptorOpcode);
            writer.WriteEnum(actionType);
            writer.WriteInt(payload.Length);
            writer.WriteBytes(payload.ToArray(), payload.Length);
        }

        internal static bool TryReadManagedActionBody(
            PacketReader reader,
            out ulong descriptorOpcode,
            out GameActionType actionType,
            out byte[] payload)
        {
            descriptorOpcode = 0;
            actionType = default;
            payload = [];
            if (reader.ReadULong() != ManagedActionMagic ||
                reader.ReadByte() != Version)
                return false;

            descriptorOpcode = reader.ReadULong();
            actionType = reader.ReadEnum<GameActionType>();
            ValidateActionType(actionType);
            var length = reader.ReadInt();
            if (length < 0)
                return false;

            payload = new byte[length];
            reader.ReadBytes(payload, length);
            return true;
        }

        internal static GameAction ToGameAction(Player player, RitsuLibManagedNetAction action)
        {
            return new RitsuLibManagedGameAction(
                player,
                action.DescriptorOpcode,
                action.ManagedActionType,
                action.Payload);
        }

        internal static bool TryGetRegistration(
            ulong opcode,
            GameActionType actionType,
            out RegistrationBase registration)
        {
            lock (Gate)
            {
                return Registrations.TryGetValue(opcode, out registration!) &&
                       registration.ActionType == actionType;
            }
        }

        private static bool CanSendManagedAction(INetGameService net)
        {
            return net switch
            {
                { Type: NetGameType.Singleplayer } => true,
                NetClientGameService client => PeerSupportsManagedActions(client.HostNetId),
                NetHostGameService host => host.ConnectedPeers
                    .Where(peer => peer.readyForBroadcasting)
                    .All(peer => PeerSupportsManagedActions(peer.peerId)),
                _ => false,
            };
        }

        private static bool PeerSupportsManagedActions(ulong peerNetId)
        {
            return RitsuLibSidecarSessionManager.TryGetPeerFeatures(peerNetId, out var features) &&
                   (features & RitsuLibSidecarPeerFeatures.ManagedNetActions) != 0;
        }

        private static void ValidateActionType(GameActionType actionType)
        {
            if (actionType is GameActionType.None)
                throw new InvalidOperationException("Managed net actions do not support GameActionType.None.");
        }

        private static bool NextPayloadIsManagedAction(PacketReader reader)
        {
            return TryPeekULong(reader, InitialOffset, out var magic) &&
                   magic == ManagedActionMagic &&
                   TryPeekByte(reader, ManagedActionMagicBits, out var version) &&
                   version == Version;
        }

        private static bool TryPeekULong(
            PacketReader reader,
            int bitOffset,
            out ulong value)
        {
            value = 0;
            if (!TryReadBits(reader, bitOffset, ManagedActionMagicBits, out var buffer))
                return false;

            value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
            return true;
        }

        private static bool TryPeekByte(
            PacketReader reader,
            int bitOffset,
            out byte value)
        {
            value = 0;
            if (!TryReadBits(reader, bitOffset, ByteBits, out var buffer))
                return false;

            value = buffer[InitialOffset];
            return true;
        }

        private static bool TryReadBits(
            PacketReader reader,
            int bitOffset,
            int bitCount,
            out byte[] destination)
        {
            destination = new byte[(bitCount + ByteBits - 1) / ByteBits];
            var originBitPosition = reader.BitPosition + bitOffset;
            if (originBitPosition < 0 ||
                bitCount < 0 ||
                reader.Buffer.Length * ByteBits - originBitPosition < bitCount)
                return false;

            for (var i = 0; i < bitCount; i++)
                if (GetBit(reader.Buffer, originBitPosition + i))
                    destination[i / ByteBits] |= (byte)(1 << (i % ByteBits));

            return true;
        }

        private static bool GetBit(byte[] buffer, int bitPosition)
        {
            return (buffer[bitPosition / ByteBits] & (1 << (bitPosition % ByteBits))) != 0;
        }

        internal abstract class RegistrationBase(
            string moduleId,
            string actionKey,
            GameActionType actionType)
        {
            public string ModuleId { get; } = moduleId;
            public string ActionKey { get; } = actionKey;
            public GameActionType ActionType { get; } = actionType;
            public abstract Task Execute(RitsuLibManagedGameAction action, GameActionPlayerChoiceContext choiceContext);
        }

        private sealed class Registration<T>(
            string moduleId,
            string actionKey,
            Func<ReadOnlySpan<byte>, T> deserialize,
            Func<RitsuLibManagedNetActionContext<T>, Task> execute,
            GameActionType actionType)
            : RegistrationBase(moduleId, actionKey, actionType)
        {
            public override async Task Execute(
                RitsuLibManagedGameAction action,
                GameActionPlayerChoiceContext choiceContext)
            {
                var message = deserialize(action.Payload);
                var context = new RitsuLibManagedNetActionContext<T>(
                    message,
                    action.Player,
                    action,
                    choiceContext);
                await execute(context);
            }
        }
    }

    /// <summary>
    ///     Vanilla queue action that executes a RitsuLib-managed payload.
    ///     执行 RitsuLib managed payload 的原版队列 action。
    /// </summary>
    public sealed class RitsuLibManagedGameAction(
        Player player,
        ulong descriptorOpcode,
        GameActionType actionType,
        byte[] payload)
        : GameAction
    {
        /// <summary>
        ///     Player that owns this queued action.
        ///     拥有此队列 action 的玩家。
        /// </summary>
        public Player Player { get; } = player;

        /// <summary>
        ///     Stable descriptor opcode that identifies the managed action executor.
        ///     标识 managed action 执行器的稳定 descriptor opcode。
        /// </summary>
        public ulong DescriptorOpcode { get; } = descriptorOpcode;

        /// <summary>
        ///     Serialized payload owned by the descriptor.
        ///     由 descriptor 管理的序列化载荷。
        /// </summary>
        public byte[] Payload { get; } = payload;

        /// <inheritdoc />
        public override ulong OwnerId => Player.NetId;

        /// <inheritdoc />
        public override GameActionType ActionType { get; } = actionType;

        /// <inheritdoc />
        public override bool RecordableToReplay => false;

        /// <inheritdoc />
        protected override async Task ExecuteAction()
        {
            if (!RitsuLibManagedNetActions.TryGetRegistration(DescriptorOpcode, ActionType, out var registration))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[ManagedNetAction] Missing descriptor opcode {DescriptorOpcode} for action type {ActionType}.");
                return;
            }

            var choiceContext = new GameActionPlayerChoiceContext(this);
            try
            {
                await registration.Execute(this, choiceContext);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[ManagedNetAction] Action opcode {DescriptorOpcode} type {ActionType} failed: {ex}");
            }
        }

        /// <inheritdoc />
        public override INetAction ToNetAction()
        {
            return RitsuLibManagedNetActionCarrierFactory.Create(
                DescriptorOpcode,
                ActionType,
                Payload);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"RitsuLibManagedGameAction player {OwnerId} opcode {DescriptorOpcode} type {ActionType}";
        }
    }
}
