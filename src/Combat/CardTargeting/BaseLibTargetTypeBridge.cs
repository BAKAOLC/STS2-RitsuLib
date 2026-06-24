using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Optional bridge for BaseLib custom target predicates.
    /// </summary>
    internal static class BaseLibTargetTypeBridge
    {
        private const string BaseLibCustomTargetTypeName = "BaseLib.Patches.Features.CustomTargetType";

        private static readonly Lock Gate = new();

        private static IReadOnlyDictionary<TargetType, TargetPredicate>? _singleTargeting;
        private static IReadOnlyDictionary<TargetType, TargetPredicate>? _multiTargeting;
        private static bool _loggedMissingType;
        private static bool _loggedMissingFields;

        internal static bool IsCustomSingleTargetType(TargetType targetType)
        {
            return TryGetSingleTargeting(out var singleTargeting) && singleTargeting.ContainsKey(targetType);
        }

        internal static bool IsCustomMultiTargetType(TargetType targetType)
        {
            return TryGetMultiTargeting(out var multiTargeting) && multiTargeting.ContainsKey(targetType);
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

        private static bool TryGetSingleTargeting(
            out IReadOnlyDictionary<TargetType, TargetPredicate> singleTargeting)
        {
            EnsureResolved();
            singleTargeting = _singleTargeting!;
            return singleTargeting != null;
        }

        private static bool TryGetMultiTargeting(
            out IReadOnlyDictionary<TargetType, TargetPredicate> multiTargeting)
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
            {
                var assembly = mod.assembly;
                if (assembly == null)
                    continue;

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

        private static IReadOnlyDictionary<TargetType, TargetPredicate>? ReadPredicateMap(
            Type type,
            string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var value = field?.GetValue(null);

            return value switch
            {
                IReadOnlyDictionary<TargetType, Func<Creature, Player, bool>> playerAware => playerAware.ToDictionary(
                    pair => pair.Key, pair => (TargetPredicate)((creature, player) => pair.Value(creature, player))),
                IReadOnlyDictionary<TargetType, Func<Creature, bool>> legacy => legacy.ToDictionary(pair => pair.Key,
                    pair => (TargetPredicate)((creature, _) => pair.Value(creature))),
                _ => null,
            };
        }

        private delegate bool TargetPredicate(Creature creature, Player player);
    }
}
