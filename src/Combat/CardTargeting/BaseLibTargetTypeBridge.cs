using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     <para xml:lang="en">Bridges BaseLib custom-target predicates when BaseLib is loaded.</para>
    ///     <para xml:lang="zh-CN">在 BaseLib 已加载时桥接其自定义目标谓词。</para>
    /// </summary>
    internal static class BaseLibTargetTypeBridge
    {
        private const string BaseLibCustomTargetTypeName = "BaseLib.Patches.Features.CustomTargetType";

        private static readonly Lock Gate = new();

        private static ITargetPredicateMap? _singleTargeting;
        private static ITargetPredicateMap? _multiTargeting;
        private static bool _loggedMissingType;
        private static bool _loggedMissingFields;

        internal static bool IsCustomSingleTargetType(TargetType targetType)
        {
            return TryGetSingleTargeting(out var singleTargeting) &&
                   singleTargeting.TryGetValue(targetType, out _);
        }

        internal static bool IsCustomMultiTargetType(TargetType targetType)
        {
            return TryGetMultiTargeting(out var multiTargeting) &&
                   multiTargeting.TryGetValue(targetType, out _);
        }

        internal static bool TryIsAllowedSingleTarget(
            TargetType targetType,
            Creature creature,
            Player player,
            out bool allowed)
        {
            allowed = false;
            if (!TryGetSingleTargeting(out var singleTargeting) ||
                !singleTargeting.TryGetValue(targetType, out var predicate))
                return false;

            try
            {
                allowed = predicate(creature, player);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardTargeting] BaseLib single-target predicate failed for {(int)targetType}: {ex.Message}");
                return false;
            }
        }

        internal static bool TryShouldIncludeMultiTarget(
            TargetType targetType,
            Creature creature,
            Player player,
            out bool include)
        {
            include = false;
            if (!TryGetMultiTargeting(out var multiTargeting) ||
                !multiTargeting.TryGetValue(targetType, out var predicate))
                return false;

            try
            {
                include = predicate(creature, player);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardTargeting] BaseLib multi-target predicate failed for {(int)targetType}: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetSingleTargeting(out ITargetPredicateMap singleTargeting)
        {
            EnsureResolved();
            singleTargeting = _singleTargeting!;
            return singleTargeting != null;
        }

        private static bool TryGetMultiTargeting(out ITargetPredicateMap multiTargeting)
        {
            EnsureResolved();
            multiTargeting = _multiTargeting!;
            return multiTargeting != null;
        }

        private static void EnsureResolved()
        {
            if (_singleTargeting != null && _multiTargeting != null)
                return;

            lock (Gate)
            {
                if (_singleTargeting != null && _multiTargeting != null)
                    return;

                var type = ResolveBaseLibCustomTargetType();
                if (type == null)
                    return;

                _singleTargeting = ReadPredicateMap(type, "SingleTargeting");
                _multiTargeting = ReadPredicateMap(type, "MultiTargeting");

                if (_singleTargeting != null && _multiTargeting != null)
                {
                    RitsuLibFramework.Logger.Info("[CardTargeting] BaseLib custom TargetType bridge resolved.");
                    return;
                }

                if (_loggedMissingFields)
                    return;
                _loggedMissingFields = true;
                RitsuLibFramework.Logger.Info(
                    "[CardTargeting] BaseLib custom TargetType registry fields were not found.");
            }
        }

        private static Type? ResolveBaseLibCustomTargetType()
        {
            var byQualifiedName = ExternalFrameworkRegistry.ResolveType(BaseLibCustomTargetTypeName);
            if (byQualifiedName != null)
                return byQualifiedName;

            foreach (var mod in Sts2ModManagerCompat.EnumerateLoadedModsWithAssembly())
            foreach (var assembly in Sts2ModManagerCompat.GetAssemblies(mod))
            {
                var type = assembly.GetType(BaseLibCustomTargetTypeName, false);
                if (type != null)
                    return type;
            }

            var fallback = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(BaseLibCustomTargetTypeName, false))
                .OfType<Type>()
                .FirstOrDefault();
            if (fallback != null)
                return fallback;

            if (_loggedMissingType)
                return null;
            _loggedMissingType = true;
            RitsuLibFramework.Logger.Info("[CardTargeting] BaseLib custom TargetType type not found.");
            return null;
        }

        private static ITargetPredicateMap? ReadPredicateMap(
            Type type,
            string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var value = field?.GetValue(null);

            return value switch
            {
                IReadOnlyDictionary<TargetType, Func<Creature, Player, bool>> playerAware =>
                    new PlayerAwareTargetPredicateMap(playerAware),
                IReadOnlyDictionary<TargetType, Func<Creature, bool>> legacy =>
                    new LegacyTargetPredicateMap(legacy),
                _ => null,
            };
        }

        private interface ITargetPredicateMap
        {
            bool TryGetValue(TargetType targetType, out TargetPredicate predicate);
        }

        private sealed class PlayerAwareTargetPredicateMap(
            IReadOnlyDictionary<TargetType, Func<Creature, Player, bool>> predicates) : ITargetPredicateMap
        {
            public bool TryGetValue(TargetType targetType, out TargetPredicate predicate)
            {
                if (!predicates.TryGetValue(targetType, out var resolved))
                {
                    predicate = null!;
                    return false;
                }

                predicate = resolved.Invoke;
                return true;
            }
        }

        private sealed class LegacyTargetPredicateMap(
            IReadOnlyDictionary<TargetType, Func<Creature, bool>> predicates) : ITargetPredicateMap
        {
            public bool TryGetValue(TargetType targetType, out TargetPredicate predicate)
            {
                if (!predicates.TryGetValue(targetType, out var resolved))
                {
                    predicate = null!;
                    return false;
                }

                predicate = (creature, _) => resolved(creature);
                return true;
            }
        }

        private delegate bool TargetPredicate(Creature creature, Player player);
    }
}
