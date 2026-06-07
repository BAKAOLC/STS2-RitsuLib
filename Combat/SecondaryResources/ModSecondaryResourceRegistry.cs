using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Per-mod registration facade for secondary combat resources.
    ///     每个 mod 的次级战斗资源注册 facade。
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

        private readonly string _modId;

        private ModSecondaryResourceRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     True when at least one secondary resource has been registered.
        ///     至少注册了一个次级资源时为 true。
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
        ///     Returns the registry facade for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的注册 facade。
        /// </summary>
        public static ModSecondaryResourceRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModSecondaryResourceRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Builds the full compound id for a mod-local secondary resource id.
        ///     为 mod 本地次级资源 id 构建完整 compound id。
        /// </summary>
        public static string GetResourceId(string modId, string localId)
        {
            return ModContentRegistry.GetCompoundId(modId, IdTypeStem, localId);
        }

        /// <summary>
        ///     Registers a secondary resource and returns the bound definition.
        ///     注册一个次级资源并返回已绑定的定义。
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
        ///     Attempts to read a registered definition.
        ///     尝试读取已注册的资源定义。
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
        ///     Returns a registered definition or throws when missing.
        ///     返回已注册的定义；不存在时抛出异常。
        /// </summary>
        public static SecondaryResourceDefinition Get(string resourceId)
        {
            return TryGet(resourceId, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Secondary resource is not registered: {resourceId}");
        }

        /// <summary>
        ///     Returns all registered definitions in deterministic order.
        ///     按确定性顺序返回所有已注册定义。
        /// </summary>
        public static SecondaryResourceDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values.OrderBy(static definition => definition.Id, StringComparer.Ordinal).ToArray();
            }
        }

        /// <summary>
        ///     Registers an additional combat UI visibility predicate for one secondary resource.
        ///     为一个次级资源注册额外战斗 UI 可见性谓词。
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

                registrations.Add(new(order, predicate));
                registrations.Sort(static (left, right) => left.Order.CompareTo(right.Order));
            }
        }

        /// <summary>
        ///     Shows a secondary resource in combat UI for <typeparamref name="TCharacter" /> even before the resource
        ///     is obtained.
        ///     让 <typeparamref name="TCharacter" /> 在战斗 UI 中固定显示该次级资源，即使尚未获得该资源。
        /// </summary>
        public void AlwaysShowInCombatUiForCharacter<TCharacter>(string localId, int order = -1000)
            where TCharacter : CharacterModel
        {
            AlwaysShowInCombatUiForCharacter(localId, typeof(TCharacter), order);
        }

        /// <summary>
        ///     Shows a secondary resource in combat UI for a character type even before the resource is obtained.
        ///     让指定角色类型在战斗 UI 中固定显示该次级资源，即使尚未获得该资源。
        /// </summary>
        public void AlwaysShowInCombatUiForCharacter(string localId, Type characterType, int order = -1000)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localId);
            ArgumentNullException.ThrowIfNull(characterType);
            if (!typeof(CharacterModel).IsAssignableFrom(characterType))
                throw new ArgumentException(
                    $"Type '{characterType.FullName}' is not a character model.",
                    nameof(characterType));

            RegisterCombatUiAlwaysVisibleWhen(
                localId,
                context => characterType.IsInstanceOfType(context.Player.Character),
                order);
        }

        /// <summary>
        ///     Shows a secondary resource in combat UI for every character even before the resource is obtained.
        ///     让所有角色在战斗 UI 中固定显示该次级资源，即使尚未获得该资源。
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
                    ? registrations.Select(static registration => registration.Predicate).ToArray()
                    : [];
            }
        }

        private sealed record CombatUiVisibilityPredicateRegistration(
            int Order,
            SecondaryResourceCombatUiVisibilityPredicate Predicate);
    }
}
