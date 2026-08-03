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
    ///     <para xml:lang="en">Describes a RitsuLib-managed action carried by a vanilla action-enqueue message.</para>
    ///     <para xml:lang="zh-CN">描述由原版动作入队消息承载的 RitsuLib 托管动作。</para>
    /// </summary>
    /// <param name="ModuleId">
    ///     <para xml:lang="en">Stable owner ID used to derive the opcode.</para>
    ///     <para xml:lang="zh-CN">用于派生操作码的稳定所有者 ID。</para>
    /// </param>
    /// <param name="ActionKey">
    ///     <para xml:lang="en">Stable action key used to derive the opcode.</para>
    ///     <para xml:lang="zh-CN">用于派生操作码的稳定动作键。</para>
    /// </param>
    /// <param name="Serialize">
    ///     <para xml:lang="en">Serializes the typed payload.</para>
    ///     <para xml:lang="zh-CN">序列化强类型载荷。</para>
    /// </param>
    /// <param name="Deserialize">
    ///     <para xml:lang="en">Deserializes the typed payload.</para>
    ///     <para xml:lang="zh-CN">反序列化强类型载荷。</para>
    /// </param>
    /// <param name="Execute">
    ///     <para xml:lang="en">Runs when the vanilla queue action executes.</para>
    ///     <para xml:lang="zh-CN">在原版队列动作执行时运行。</para>
    /// </param>
    /// <param name="ActionType">
    ///     <para xml:lang="en">Vanilla queue action type.</para>
    ///     <para xml:lang="zh-CN">原版队列动作类型。</para>
    /// </param>
    public sealed record RitsuLibManagedNetActionDescriptor<T>(
        string ModuleId,
        string ActionKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        Func<RitsuLibManagedNetActionContext<T>, Task> Execute,
        GameActionType ActionType);

    /// <summary>
    ///     <para xml:lang="en">Provides runtime context to a managed net-action executor.</para>
    ///     <para xml:lang="zh-CN">向托管网络动作执行器提供运行时上下文。</para>
    /// </summary>
    /// <param name="Message">
    ///     <para xml:lang="en">Typed action payload.</para>
    ///     <para xml:lang="zh-CN">强类型动作载荷。</para>
    /// </param>
    /// <param name="Player">
    ///     <para xml:lang="en">Player that owns the queued action.</para>
    ///     <para xml:lang="zh-CN">拥有该队列动作的玩家。</para>
    /// </param>
    /// <param name="Action">
    ///     <para xml:lang="en">Underlying vanilla queue action.</para>
    ///     <para xml:lang="zh-CN">底层原版队列动作。</para>
    /// </param>
    /// <param name="PlayerChoiceContext">
    ///     <para xml:lang="en">Queue-backed choice context for command APIs.</para>
    ///     <para xml:lang="zh-CN">供命令 API 使用的队列支持选择上下文。</para>
    /// </param>
    public readonly record struct RitsuLibManagedNetActionContext<T>(
        T Message,
        Player Player,
        RitsuLibManagedGameAction Action,
        GameActionPlayerChoiceContext PlayerChoiceContext);

    /// <summary>
    ///     <para xml:lang="en">Registers and requests RitsuLib-managed actions through vanilla action-enqueue messages.</para>
    ///     <para xml:lang="zh-CN">通过原版动作入队消息注册和请求 RitsuLib 托管动作。</para>
    /// </summary>
    public static class RitsuLibManagedNetActions
    {
        /// <summary>
        ///     <para xml:lang="en">Maximum serialized payload size for one managed queue action.</para>
        ///     <para xml:lang="zh-CN">单个托管队列动作的最大序列化载荷大小。</para>
        /// </summary>
        public const int MaxPayloadBytes = 64 * 1024;

        private const ulong ManagedActionMagic = 0x4E_41_54_52_32_53_54_52; // RTS2RTAN
        private const byte Version = 1;
        private const int InitialOffset = 0;
        private const int ByteBits = 8;
        private const int ManagedActionMagicBits = 64;
        private static readonly int GameActionTypeBits = GetEnumBitCount<GameActionType>();

        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a managed net-action descriptor and returns its stable opcode. Registering the same
        ///         module, action key, type, and action type is idempotent; an opcode conflict throws.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册托管网络动作描述符并返回其稳定操作码。以相同模块、动作键、类型和动作类型重复注册是幂等的；操作码冲突会抛出异常。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Serializes and requests a managed action through the vanilla action-queue synchronizer. A
        ///         <see langword="true" /> result means the enqueue request was issued, not that its executor ran
        ///         successfully.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         序列化并通过原版动作队列同步器请求托管动作。返回 <see langword="true" /> 表示已发出入队请求，不表示其执行器已经成功运行。
        ///     </para>
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
            if (payload.Length > MaxPayloadBytes)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ManagedAction] Refusing oversized payload. ModuleId='{descriptor.ModuleId}' " +
                    $"ActionKey='{descriptor.ActionKey}' Bytes={payload.Length} Limit={MaxPayloadBytes}.");
                return false;
            }

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
            if (payload.Length > MaxPayloadBytes)
                throw new InvalidOperationException(
                    $"Managed action payload is {payload.Length} bytes; maximum is {MaxPayloadBytes}.");

            writer.WriteULong(ManagedActionMagic);
            writer.WriteByte(Version);
            writer.WriteULong(descriptorOpcode);
            writer.WriteEnum(actionType);
            writer.WriteInt(payload.Length);
            writer.WriteBytes([.. payload], payload.Length);
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
            if (!HasRemainingBits(reader, ManagedActionMagicBits + ByteBits) ||
                reader.ReadULong() != ManagedActionMagic ||
                reader.ReadByte() != Version ||
                !HasRemainingBits(reader, 64 + GameActionTypeBits + 32))
                return false;

            descriptorOpcode = reader.ReadULong();
            actionType = reader.ReadEnum<GameActionType>();
            if (!Enum.IsDefined(actionType) || actionType is GameActionType.None)
                return false;

            var length = reader.ReadInt();
            if (length < 0 ||
                length > MaxPayloadBytes ||
                !HasRemainingBits(reader, (long)length * ByteBits))
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

        internal static bool NextPayloadIsManagedAction(PacketReader reader, int bitOffset = InitialOffset)
        {
            return TryPeekULong(reader, bitOffset, out var magic) &&
                   magic == ManagedActionMagic &&
                   TryPeekByte(reader, bitOffset + ManagedActionMagicBits, out var version) &&
                   version == Version;
        }

        internal static bool TryPeekInt(
            PacketReader reader,
            int bitOffset,
            int bits,
            out int value)
        {
            value = 0;
            if (!TryReadBits(reader, bitOffset, bits, out var buffer))
                return false;

            Span<byte> scratch = stackalloc byte[sizeof(int)];
            buffer.AsSpan().CopyTo(scratch);
            value = BinaryPrimitives.ReadInt32LittleEndian(scratch);
            return true;
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

        private static bool HasRemainingBits(PacketReader reader, long bitCount)
        {
            return bitCount >= 0 &&
                   reader.BitPosition >= 0 &&
                   (long)reader.Buffer.Length * ByteBits - reader.BitPosition >= bitCount;
        }

        private static int GetEnumBitCount<T>() where T : struct, Enum
        {
            var maxValue = Enum.GetValues<T>().Max(static value => Convert.ToInt32(value));
            return (int)Math.Ceiling(Math.Log2(maxValue) + 1d);
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
    ///     <para xml:lang="en">
    ///         Applies a registered RitsuLib multiplayer action in game-action order. Missing registrations and
    ///         executor failures are surfaced to the game's action executor; recoverable executor failures are also
    ///         logged with their RitsuLib descriptor context before propagation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按游戏动作顺序应用已注册的 RitsuLib 多人操作。缺失注册与执行器失败会交由游戏动作执行器处理；
    ///         可恢复的执行器失败还会在继续抛出前连同 RitsuLib 描述符上下文写入日志。
    ///     </para>
    /// </summary>
    public sealed class RitsuLibManagedGameAction(
        Player player,
        ulong descriptorOpcode,
        GameActionType actionType,
        byte[] payload)
        : GameAction
    {
        /// <summary>
        ///     <para xml:lang="en">Player that owns this queued action.</para>
        ///     <para xml:lang="zh-CN">拥有该队列动作的玩家。</para>
        /// </summary>
        public Player Player { get; } = player;

        /// <summary>
        ///     <para xml:lang="en">Stable descriptor opcode that identifies the managed-action executor.</para>
        ///     <para xml:lang="zh-CN">标识托管动作执行器的稳定描述符操作码。</para>
        /// </summary>
        public ulong DescriptorOpcode { get; } = descriptorOpcode;

        /// <summary>
        ///     <para xml:lang="en">Serialized payload owned by the descriptor.</para>
        ///     <para xml:lang="zh-CN">由描述符管理的序列化载荷。</para>
        /// </summary>
        public byte[] Payload { get; } = payload;

        /// <inheritdoc />
        public override ulong OwnerId => Player.NetId;

        /// <inheritdoc />
        public override GameActionType ActionType { get; } = actionType;

        /// <inheritdoc />
        public override bool RecordableToReplay => true;

        /// <inheritdoc />
        protected override async Task ExecuteAction()
        {
            if (!RitsuLibManagedNetActions.TryGetRegistration(DescriptorOpcode, ActionType, out var registration))
                throw new InvalidOperationException(
                    $"Missing managed-action descriptor opcode {DescriptorOpcode} for action type {ActionType}.");

            var choiceContext = new GameActionPlayerChoiceContext(this);
            try
            {
                await registration.Execute(this, choiceContext);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[ManagedNetAction] Action opcode {DescriptorOpcode} type {ActionType} failed: {ex}");
                throw;
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
