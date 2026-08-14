using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Stores one player's mutable secondary-resource amounts for a combat.</para>
    ///     <para xml:lang="zh-CN">存储一名玩家在一场战斗中的可变次级资源数量。</para>
    /// </summary>
    public sealed class SecondaryResourceState
    {
        private readonly Dictionary<string, int> _amounts = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Gets whether at least one amount has been stored explicitly.</para>
        ///     <para xml:lang="zh-CN">获取是否至少显式存储了一个资源数量。</para>
        /// </summary>
        public bool HasValues => _amounts.Count > 0;

        /// <summary>
        ///     <para xml:lang="en">Occurs after a resource's effective amount changes.</para>
        ///     <para xml:lang="zh-CN">在资源的实际数量变化后发生。</para>
        /// </summary>
        public event Action<SecondaryResourceChangedEvent>? Changed;

        /// <summary>
        ///     <para xml:lang="en">Gets the current amount without creating state for an unknown resource.</para>
        ///     <para xml:lang="zh-CN">获取当前数量，且不会为未知资源创建状态。</para>
        /// </summary>
        public int Get(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            if (_amounts.TryGetValue(resourceId.Trim(), out var amount))
                return amount;

            return !ModSecondaryResourceRegistry.TryGet(resourceId, out var definition)
                ? 0
                : Clamp(definition, definition.DefaultAmount);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a key-sorted snapshot of explicitly stored amounts.</para>
        ///     <para xml:lang="zh-CN">返回按键排序的显式存储数量快照。</para>
        /// </summary>
        public IReadOnlyDictionary<string, int> Snapshot()
        {
            return _amounts
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        internal int Set(
            Player player,
            SecondaryResourceDefinition definition,
            int amount,
            SecondaryResourceChangeReason reason,
            AbstractModel? source,
            bool emit = true)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentNullException.ThrowIfNull(definition);

            var oldAmount = Get(definition.Id);
            var newAmount = Clamp(player, definition, amount);
            if (oldAmount == newAmount)
            {
                _amounts.TryAdd(definition.Id, newAmount);
                return newAmount;
            }

            _amounts[definition.Id] = newAmount;
            if (emit)
                Changed?.Invoke(new(player, definition, oldAmount, newAmount, reason, source));

            return newAmount;
        }

        internal bool Remove(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return _amounts.Remove(resourceId.Trim());
        }

        private static int Clamp(
            Player player,
            SecondaryResourceDefinition definition,
            int amount)
        {
            var hardClamped = Clamp(definition, amount);
            if (!definition.ClampToMaxAmount)
                return hardClamped;

            return SecondaryResourceStateStore.GetMaxAmount(player, definition.Id) is { } maxAmount
                ? Math.Min(hardClamped, maxAmount)
                : hardClamped;
        }

        private static int Clamp(SecondaryResourceDefinition definition, int amount)
        {
            return Math.Clamp(amount, definition.MinAmount, definition.HardMaxAmount);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Describes a secondary-resource amount change notification.</para>
    ///     <para xml:lang="zh-CN">描述次级资源数量变化通知。</para>
    /// </summary>
    public sealed record SecondaryResourceChangedEvent(
        Player Player,
        SecondaryResourceDefinition Definition,
        int OldAmount,
        int NewAmount,
        SecondaryResourceChangeReason Reason,
        AbstractModel? Source)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the saturating signed difference from the old amount to the new amount.</para>
        ///     <para xml:lang="zh-CN">获取从旧数量到新数量的饱和带符号差值。</para>
        /// </summary>
        public int Delta => SecondaryResourceAmountMath.SubtractSaturating(NewAmount, OldAmount);
    }

    /// <summary>
    ///     <para xml:lang="en">Provides per-player storage for secondary-resource combat state.</para>
    ///     <para xml:lang="zh-CN">提供按玩家划分的次级资源战斗状态存储。</para>
    /// </summary>
    public static class SecondaryResourceStateStore
    {
        private static readonly AttachedState<PlayerCombatState, SecondaryResourceState> States = new(() => new());

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or creates state for <paramref name="player" /> when at least one resource is registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         至少注册了一个资源时，获取或创建 <paramref name="player" /> 的状态。
        ///     </para>
        /// </summary>
        public static SecondaryResourceState Get(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            return !ModSecondaryResourceRegistry.HasAny
                ? throw new InvalidOperationException("No secondary resources are registered.")
                : States.GetOrCreate(GetPlayerCombatState(player));
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get existing state without creating it.</para>
        ///     <para xml:lang="zh-CN">尝试获取现有状态，且不会创建状态。</para>
        /// </summary>
        public static bool TryGet(Player player, out SecondaryResourceState state)
        {
            ArgumentNullException.ThrowIfNull(player);
            state = null!;

            return ModSecondaryResourceRegistry.HasAny &&
                   player.PlayerCombatState != null &&
                   States.TryGetValue(player.PlayerCombatState, out state!);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a current amount without creating player state.</para>
        ///     <para xml:lang="zh-CN">获取当前数量，且不会创建玩家状态。</para>
        /// </summary>
        public static int GetAmount(Player player, string resourceId)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            if (!ModSecondaryResourceRegistry.HasAny)
                return 0;

            if (TryGet(player, out var state))
                return state.Get(resourceId);

            return ModSecondaryResourceRegistry.TryGet(resourceId, out var definition)
                ? Math.Clamp(definition.DefaultAmount, definition.MinAmount, definition.HardMaxAmount)
                : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Calculates the current maximum for a registered resource.</para>
        ///     <para xml:lang="zh-CN">计算已注册资源的当前最大数量。</para>
        /// </summary>
        public static int? GetMaxAmount(Player player, string resourceId)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            if (!ModSecondaryResourceRegistry.TryGet(resourceId, out var definition) ||
                definition.BaseMaxAmount == null)
                return null;

            var combatState = player.Creature?.CombatState;
            if (combatState == null)
                return Math.Clamp(definition.BaseMaxAmount.Value, definition.MinAmount, definition.HardMaxAmount);

            var context = new SecondaryResourceMaxContext(combatState, player, definition);
            var modified = SecondaryResourceHook.ModifyMaxAmount(context, definition.BaseMaxAmount.Value);
            return SecondaryResourceAmountMath.FloorAndClamp(
                modified,
                definition.MinAmount,
                definition.HardMaxAmount);
        }

        internal static void SetFromPersistence(Player player, string resourceId, int amount)
        {
            if (!ModSecondaryResourceRegistry.TryGet(resourceId, out var definition))
                return;

            Get(player).Set(player, definition, amount, SecondaryResourceChangeReason.Set, null, false);
        }

        private static PlayerCombatState GetPlayerCombatState(Player player)
        {
            return player.PlayerCombatState ??
                   throw new InvalidOperationException("Player does not have a combat state.");
        }
    }

    internal static class SecondaryResourceAmountMath
    {
        public static int AddSaturating(int left, int right)
        {
            return (int)Math.Clamp((long)left + right, int.MinValue, int.MaxValue);
        }

        public static int SubtractSaturating(int left, int right)
        {
            return (int)Math.Clamp((long)left - right, int.MinValue, int.MaxValue);
        }

        public static int FloorAndClamp(decimal value, int min, int max)
        {
            return (int)Math.Clamp(decimal.Floor(value), min, max);
        }

        public static int CeilingAndClamp(decimal value, int min, int max)
        {
            return (int)Math.Clamp(decimal.Ceiling(value), min, max);
        }

        public static int MultiplyNonNegativeSaturating(int left, int right)
        {
            return (int)Math.Min(int.MaxValue, (long)Math.Max(0, left) * Math.Max(0, right));
        }

        public static int RoundAndClamp(decimal value, int min, int max)
        {
            return (int)Math.Clamp(decimal.Round(value, 0, MidpointRounding.ToEven), min, max);
        }

        public static int TruncateAndClamp(decimal value, int min, int max)
        {
            return (int)Math.Clamp(decimal.Truncate(value), min, max);
        }
    }
}
