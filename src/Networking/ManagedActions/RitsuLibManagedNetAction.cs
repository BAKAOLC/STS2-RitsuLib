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
    ///     <para xml:lang="en">Base class for vanilla queue-action messages that carry RitsuLib-managed actions.</para>
    ///     <para xml:lang="zh-CN">承载 RitsuLib 托管动作的游戏原版队列动作消息基类。</para>
    /// </summary>
    public abstract class RitsuLibManagedNetAction : INetAction
    {
        /// <summary>
        ///     <para xml:lang="en">Stable descriptor opcode that identifies the managed-action executor.</para>
        ///     <para xml:lang="zh-CN">标识托管动作执行器的稳定描述符操作码。</para>
        /// </summary>
        public ulong DescriptorOpcode { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Vanilla queue action type used by the resulting <see cref="GameAction" />.</para>
        ///     <para xml:lang="zh-CN">生成的 <see cref="GameAction" /> 使用的原版队列动作类型。</para>
        /// </summary>
        public GameActionType ManagedActionType { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Serialized action payload owned by the registered descriptor.</para>
        ///     <para xml:lang="zh-CN">由已注册描述符管理的序列化动作载荷。</para>
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
