using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Registers secondary combat resources owned by one mod.</para>
    ///     <para xml:lang="zh-CN">注册由一个模组所有的次级战斗资源。</para>
    /// </summary>
    public sealed partial class ModSecondaryResourceRegistry
    {
        private const string IdTypeStem = "SECONDARY_RESOURCE";
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSecondaryResourceRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, SecondaryResourceDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, List<CombatUiVisibilityPredicateRegistration>>
            CombatUiVisibilityPredicates =
                new(StringComparer.OrdinalIgnoreCase);

        private static long _nextVisibilityPredicateSequence;
        private readonly string _modId;

        private ModSecondaryResourceRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether at least one secondary resource is registered.</para>
        ///     <para xml:lang="zh-CN">获取是否至少注册了一个次级资源。</para>
        /// </summary>
        public static bool HasAny
        {
            get
            {
                lock (SyncRoot)
                {
                    return Definitions.Count > 0;
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registry for <paramref name="modId" />. Leading and trailing whitespace is ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 的注册表；忽略首尾空白。
        ///     </para>
        /// </summary>
        public static ModSecondaryResourceRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            var normalizedModId = modId.Trim();

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(normalizedModId, out var existing))
                    return existing;

                var created = new ModSecondaryResourceRegistry(normalizedModId);
                Registries[normalizedModId] = created;
                return created;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a full resource ID from a mod ID and mod-local ID.</para>
        ///     <para xml:lang="zh-CN">根据模组 ID 和模组内 ID 构建完整资源 ID。</para>
        /// </summary>
        public static string GetResourceId(string modId, string localId)
        {
            return ModContentRegistry.GetCompoundId(modId, IdTypeStem, localId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a secondary resource and returns its mod-bound definition. Repeating the same registration
        ///         returns the definition registered first.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册次级资源并返回绑定到模组后的定义；重复注册同一资源时返回最先注册的定义。</para>
        /// </summary>
        public SecondaryResourceDefinition Register(string localId, SecondaryResourceDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            ArgumentNullException.ThrowIfNull(definition);

            var bound = definition.Bind(_modId, localId);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(bound.Id, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, _modId))
                        throw new InvalidOperationException(
                            $"Secondary resource '{bound.Id}' is already registered by mod '{existing.ModId}'.");

                    return existing;
                }

                Definitions[bound.Id] = bound;
            }

            RitsuLibFramework.Logger.Info(
                $"[SecondaryResource] Registered {bound.Id} (mod={bound.ModId}).");
            return bound;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get a registered definition by full resource ID.</para>
        ///     <para xml:lang="zh-CN">尝试按完整资源 ID 获取已注册定义。</para>
        /// </summary>
        public static bool TryGet(string resourceId, out SecondaryResourceDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(resourceId.Trim(), out definition!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a registered definition, or throws when the ID is unknown.</para>
        ///     <para xml:lang="zh-CN">获取已注册定义；ID 未知时抛出异常。</para>
        /// </summary>
        public static SecondaryResourceDefinition Get(string resourceId)
        {
            return TryGet(resourceId, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Secondary resource is not registered: {resourceId}");
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a deterministically ordered snapshot of registered definitions.</para>
        ///     <para xml:lang="zh-CN">返回按确定性顺序排列的已注册定义快照。</para>
        /// </summary>
        public static SecondaryResourceDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return [.. Definitions.Values.OrderBy(static definition => definition.Id, StringComparer.Ordinal)];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a localized <see cref="HoverTip" /> for a registered resource.</para>
        ///     <para xml:lang="zh-CN">为已注册资源创建本地化的 <see cref="HoverTip" />。</para>
        /// </summary>
        /// <param name="resourceId">
        ///     <para xml:lang="en">The full resource ID.</para>
        ///     <para xml:lang="zh-CN">完整资源 ID。</para>
        /// </param>
        /// <param name="amount">
        ///     <para xml:lang="en">The current amount exposed to localization variables.</para>
        ///     <para xml:lang="zh-CN">提供给本地化变量的当前数量。</para>
        /// </param>
        /// <param name="maxAmount">
        ///     <para xml:lang="en">The optional maximum exposed to localization variables.</para>
        ///     <para xml:lang="zh-CN">提供给本地化变量的可选最大数量。</para>
        /// </param>
        public static HoverTip CreateHoverTip(string resourceId, int amount = 0, int? maxAmount = null)
        {
            return !TryGet(resourceId, out var definition)
                ? throw new KeyNotFoundException($"Secondary resource is not registered: {resourceId}")
                : SecondaryResourceHoverTipFactory.Create(definition, amount, maxAmount);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an additional combat UI visibility predicate for one resource. Lower order values run
        ///         first; predicates with the same order retain registration order.
        ///     </para>
        ///     <para xml:lang="zh-CN">为一种资源注册额外的战斗界面可见性谓词；顺序值较小的先运行，相同时保持注册顺序。</para>
        /// </summary>
        public void RegisterCombatUiAlwaysVisibleWhen(
            string localId,
            SecondaryResourceCombatUiVisibilityPredicate predicate,
            int order = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            ArgumentNullException.ThrowIfNull(predicate);

            var resourceId = GetResourceId(_modId, localId);
            lock (SyncRoot)
            {
                if (!CombatUiVisibilityPredicates.TryGetValue(resourceId, out var registrations))
                {
                    registrations = [];
                    CombatUiVisibilityPredicates[resourceId] = registrations;
                }

                registrations.Add(new(order, _nextVisibilityPredicateSequence++, predicate));
                registrations.Sort(static (left, right) =>
                {
                    var orderComparison = left.Order.CompareTo(right.Order);
                    return orderComparison != 0
                        ? orderComparison
                        : left.Sequence.CompareTo(right.Sequence);
                });
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Keeps a resource visible in combat UI for <typeparamref name="TCharacter" />, even at its default
        ///         amount.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         对 <typeparamref name="TCharacter" /> 始终在战斗界面显示资源，即使资源处于默认数量。
        ///     </para>
        /// </summary>
        public void AlwaysShowInCombatUiForCharacter<TCharacter>(string localId, int order = -1000)
            where TCharacter : CharacterModel
        {
            AlwaysShowInCombatUiForCharacter(localId, typeof(TCharacter), order);
        }

        /// <summary>
        ///     <para xml:lang="en">Keeps a resource visible for characters assignable to the specified type.</para>
        ///     <para xml:lang="zh-CN">对可赋值给指定类型的角色始终显示资源。</para>
        /// </summary>
        public void AlwaysShowInCombatUiForCharacter(string localId, Type characterType, int order = -1000)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            ArgumentNullException.ThrowIfNull(characterType);
            if (characterType.IsInterface || characterType.ContainsGenericParameters ||
                !typeof(CharacterModel).IsAssignableFrom(characterType))
                throw new ArgumentException(
                    $"Type '{characterType.FullName}' must be a closed character model subtype.",
                    nameof(characterType));

            RegisterCombatUiAlwaysVisibleWhen(
                localId,
                context => characterType.IsInstanceOfType(context.Player.Character),
                order);
        }

        /// <summary>
        ///     <para xml:lang="en">Keeps a resource visible in combat UI for every character.</para>
        ///     <para xml:lang="zh-CN">对所有角色始终在战斗界面显示资源。</para>
        /// </summary>
        public void AlwaysShowInCombatUi(string localId, int order = -1000)
        {
            RegisterCombatUiAlwaysVisibleWhen(
                localId,
                _ => true,
                order);
        }

        internal static SecondaryResourceCombatUiVisibilityPredicate[] GetCombatUiVisibilityPredicates(
            string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            lock (SyncRoot)
            {
                return CombatUiVisibilityPredicates.TryGetValue(resourceId.Trim(), out var registrations)
                    ? [.. registrations.Select(static registration => registration.Predicate)]
                    : [];
            }
        }

        private sealed record CombatUiVisibilityPredicateRegistration(
            int Order,
            long Sequence,
            SecondaryResourceCombatUiVisibilityPredicate Predicate);
    }
}
