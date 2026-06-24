using System.Reflection;
using System.Reflection.Emit;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace STS2RitsuLib.Networking.ManagedActions
{
    /// <summary>
    ///     Abstract base for RitsuLib-managed vanilla queue action carriers.
    ///     RitsuLib 管理的原版队列动作 carrier 抽象基类。
    /// </summary>
    public abstract class RitsuLibManagedNetAction : INetAction
    {
        /// <summary>
        ///     Stable descriptor opcode that identifies the managed action executor.
        ///     标识 managed action 执行器的稳定 descriptor opcode。
        /// </summary>
        public ulong DescriptorOpcode { get; private set; }

        /// <summary>
        ///     Vanilla queue action type used by the resulting <see cref="GameAction" />.
        ///     生成的 <see cref="GameAction" /> 使用的原版队列动作类型。
        /// </summary>
        public GameActionType ManagedActionType { get; private set; }

        /// <summary>
        ///     Serialized action payload owned by the registered descriptor.
        ///     由已注册 descriptor 管理的序列化 action 载荷。
        /// </summary>
        public byte[] Payload { get; private set; } = [];

        /// <inheritdoc />
        public void Serialize(PacketWriter writer)
        {
            RitsuLibManagedNetActions.WriteManagedActionBody(
                writer,
                DescriptorOpcode,
                ManagedActionType,
                Payload);
        }

        /// <inheritdoc />
        public void Deserialize(PacketReader reader)
        {
            if (!RitsuLibManagedNetActions.TryReadManagedActionBody(
                    reader,
                    out var descriptorOpcode,
                    out var actionType,
                    out var payload))
                throw new InvalidOperationException("Malformed RitsuLib managed net action payload.");

            Initialize(descriptorOpcode, actionType, payload);
        }

        /// <inheritdoc />
        public GameAction ToGameAction(Player player)
        {
            return RitsuLibManagedNetActions.ToGameAction(player, this);
        }

        internal void Initialize(
            ulong descriptorOpcode,
            GameActionType actionType,
            byte[] payload)
        {
            DescriptorOpcode = descriptorOpcode;
            ManagedActionType = actionType;
            Payload = payload;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return
                $"RitsuLibManagedNetAction opcode {DescriptorOpcode} type {ManagedActionType} payload {Payload.Length}";
        }
    }

    internal static class RitsuLibManagedNetActionCarrierFactory
    {
        private static readonly Lock Gate = new();
        private static Type? _carrierType;

        public static RitsuLibManagedNetAction Create(
            ulong descriptorOpcode,
            GameActionType actionType,
            byte[] payload)
        {
            var carrier = (RitsuLibManagedNetAction)Activator.CreateInstance(GetCarrierType())!;
            carrier.Initialize(descriptorOpcode, actionType, payload);
            return carrier;
        }

        private static Type GetCarrierType()
        {
            lock (Gate)
            {
                if (_carrierType != null)
                    return _carrierType;

                var assemblyName = new AssemblyName("STS2RitsuLib.ManagedNetAction.Runtime");
                var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var module = assembly.DefineDynamicModule("STS2RitsuLib.ManagedNetAction.RuntimeModule");
                var typeBuilder = module.DefineType(
                    "STS2RitsuLib.Runtime.ManagedNetActionCarrier",
                    TypeAttributes.Public | TypeAttributes.Sealed,
                    typeof(RitsuLibManagedNetAction));
                typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
                _carrierType = typeBuilder.CreateType() ??
                               throw new InvalidOperationException("Failed to create managed net action carrier type.");
                return _carrierType;
            }
        }
    }
}
