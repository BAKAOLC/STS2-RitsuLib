using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Central registry for custom target types and their selection predicates.
    ///     自定义目标类型及其筛选谓词的中心注册表。
    /// </summary>
    internal static class CustomTargetTypeRegistry
    {
        private static readonly Lock SyncRoot = new();

        /// <summary>
        ///     Predicate map for custom single-target types.
        ///     自定义单体目标类型的谓词映射。
        /// </summary>
        private static readonly Dictionary<TargetType, Func<Creature, bool>> SingleTargetPredicates = [];

        /// <summary>
        ///     Predicate map for custom multi-target types.
        ///     自定义群体目标类型的谓词映射。
        /// </summary>
        private static readonly Dictionary<TargetType, Func<Creature, bool>> MultiTargetPredicates = [];

        private static readonly Dictionary<TargetType, CustomTargetTypeRegistration> Registrations = [];

        /// <summary>
        ///     Returns whether <paramref name="type" /> belongs to any registered custom target category.
        ///     返回 <paramref name="type" /> 是否属于任一已注册的自定义目标类别。
        /// </summary>
        internal static bool IsRitsuCustom(TargetType type)
        {
            lock (SyncRoot)
            {
                return Registrations.ContainsKey(type);
            }
        }

        /// <summary>
        ///     Returns whether <paramref name="type" /> is registered as a custom single-target type.
        ///     返回 <paramref name="type" /> 是否被注册为自定义单体目标类型。
        /// </summary>
        internal static bool IsCustomSingleTargetType(TargetType type)
        {
            lock (SyncRoot)
            {
                return SingleTargetPredicates.ContainsKey(type);
            }
        }

        /// <summary>
        ///     Returns whether <paramref name="type" /> is registered as a custom multi-target type.
        ///     返回 <paramref name="type" /> 是否被注册为自定义群体目标类型。
        /// </summary>
        internal static bool IsCustomMultiTargetType(TargetType type)
        {
            lock (SyncRoot)
            {
                return MultiTargetPredicates.ContainsKey(type);
            }
        }

        /// <summary>
        ///     Resolves and evaluates the predicate for a custom single-target type.
        ///     解析并执行自定义单体目标类型对应的筛选谓词。
        /// </summary>
        internal static bool TryIsAllowedSingleTarget(TargetType type, Creature creature, out bool allowed)
        {
            Func<Creature, bool>? predicate;
            lock (SyncRoot)
            {
                SingleTargetPredicates.TryGetValue(type, out predicate);
            }

            if (predicate == null)
            {
                allowed = false;
                return false;
            }

            allowed = predicate(creature);
            return true;
        }

        /// <summary>
        ///     Resolves and evaluates the predicate for a custom multi-target type.
        ///     解析并执行自定义群体目标类型对应的筛选谓词。
        /// </summary>
        internal static bool TryShouldIncludeMultiTarget(TargetType type, Creature creature, out bool include)
        {
            Func<Creature, bool>? predicate;
            lock (SyncRoot)
            {
                MultiTargetPredicates.TryGetValue(type, out predicate);
            }

            if (predicate == null)
            {
                include = false;
                return false;
            }

            include = predicate(creature);
            return true;
        }

        /// <summary>
        ///     Registers or replaces a custom single-target predicate.
        ///     注册或替换一个自定义单体目标谓词。
        /// </summary>
        internal static void RegisterSingleTargetType(TargetType type, Func<Creature, bool> predicate)
        {
            Register(type, null, CustomTargetTypeKind.Single, predicate);
        }

        /// <summary>
        ///     Registers or replaces a custom single-target predicate with a diagnostic id.
        ///     使用诊断 ID 注册或替换一个自定义单体目标谓词。
        /// </summary>
        internal static void RegisterSingleTargetType(TargetType type, string id, Func<Creature, bool> predicate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            Register(type, id, CustomTargetTypeKind.Single, predicate);
        }

        /// <summary>
        ///     Registers or replaces a custom multi-target predicate.
        ///     注册或替换一个自定义群体目标谓词。
        /// </summary>
        internal static void RegisterMultiTargetType(TargetType type, Func<Creature, bool> predicate)
        {
            Register(type, null, CustomTargetTypeKind.Multi, predicate);
        }

        /// <summary>
        ///     Registers or replaces a custom multi-target predicate with a diagnostic id.
        ///     使用诊断 ID 注册或替换一个自定义群体目标谓词。
        /// </summary>
        internal static void RegisterMultiTargetType(TargetType type, string id, Func<Creature, bool> predicate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            Register(type, id, CustomTargetTypeKind.Multi, predicate);
        }

        /// <summary>
        ///     Registers all built-in custom target definitions while preserving mod-registered predicates.
        ///     注册全部内置自定义目标定义，同时保留 mod 注册的谓词。
        /// </summary>
        internal static void RegisterBuiltIns()
        {
            RegisterSingleTargetType(CustomTargetType.Anyone, target => target is { IsAlive: true, IsPet: false });
            RegisterMultiTargetType(CustomTargetType.Everyone, target => target is { IsAlive: true, IsPet: false });

            RegisterSingleTargetType(
                CustomTargetType.AnyAttackingEnemy,
                target => target is { IsAlive: true, IsEnemy: true, Monster.IntendsToAttack: true });
            RegisterMultiTargetType(
                CustomTargetType.AllAttackingEnemies,
                target => target is { IsAlive: true, IsEnemy: true, Monster.IntendsToAttack: true });

            RegisterSingleTargetType(
                CustomTargetType.AnyBlockingEnemy,
                target => target is { IsAlive: true, IsEnemy: true, Block: > 0 });
            RegisterMultiTargetType(
                CustomTargetType.AllBlockingEnemies,
                target => target is { IsAlive: true, IsEnemy: true, Block: > 0 });

            RegisterSingleTargetType(
                CustomTargetType.AnyNonBlockingEnemy,
                target => target is { IsAlive: true, IsEnemy: true, Block: 0 });
            RegisterMultiTargetType(
                CustomTargetType.AllNonBlockingEnemies,
                target => target is { IsAlive: true, IsEnemy: true, Block: 0 });

            RegisterMultiTargetType(
                CustomTargetType.AllLowestHpEnemies,
                target => target is { IsAlive: true, IsEnemy: true } && IsEnemyHpExtremum(target, true));
            RegisterMultiTargetType(
                CustomTargetType.AllHighestHpEnemies,
                target => target is { IsAlive: true, IsEnemy: true } && IsEnemyHpExtremum(target, false));

            RegisterSingleTargetType(
                CustomTargetType.AnyFullLifeEnemy,
                target => target is { IsAlive: true, IsEnemy: true } && target.CurrentHp == target.MaxHp);
            RegisterMultiTargetType(
                CustomTargetType.AllFullLifeEnemies,
                target => target is { IsAlive: true, IsEnemy: true } && target.CurrentHp == target.MaxHp);
        }

        /// <summary>
        ///     Checks whether <paramref name="target" /> is at the lowest/highest alive enemy HP.
        ///     检查 <paramref name="target" /> 是否处于存活敌人的最低/最高生命值档位。
        /// </summary>
        private static bool IsEnemyHpExtremum(Creature target, bool lowest)
        {
            var enemies = target.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
            if (enemies == null || enemies.Count == 0)
                return false;

            var extremum = lowest ? enemies.Min(e => e.CurrentHp) : enemies.Max(e => e.CurrentHp);
            return target.CurrentHp == extremum;
        }

        private static void Register(
            TargetType type,
            string? id,
            CustomTargetTypeKind kind,
            Func<Creature, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            lock (SyncRoot)
            {
                var diagnosticId = id;
                if (Registrations.TryGetValue(type, out var existing))
                {
                    diagnosticId ??= existing.Id;
                    if (existing.Kind != kind)
                        throw new InvalidOperationException(
                            $"TargetType '{diagnosticId}' is already registered as "
                            + $"{existing.Kind}; it cannot also be registered as {kind}.");
                }

                diagnosticId ??= ((int)type).ToString();
                Registrations[type] = new(diagnosticId, kind);

                switch (kind)
                {
                    case CustomTargetTypeKind.Single:
                        SingleTargetPredicates[type] = predicate;
                        MultiTargetPredicates.Remove(type);
                        break;
                    case CustomTargetTypeKind.Multi:
                        MultiTargetPredicates[type] = predicate;
                        SingleTargetPredicates.Remove(type);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
            }
        }

        private enum CustomTargetTypeKind
        {
            Single,
            Multi,
        }

        private readonly record struct CustomTargetTypeRegistration(string Id, CustomTargetTypeKind Kind);
    }
}
