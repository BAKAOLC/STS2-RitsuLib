using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Relics
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores mod-provided mappings for <see cref="ArchaicTooth" /> transcendence and
    ///         <see cref="TouchOfOrobas" /> refinement.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         保存模组为 <see cref="ArchaicTooth" /> 超越和 <see cref="TouchOfOrobas" /> 精炼提供的映射。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Target models are stored as CLR <see cref="Type" /> values and resolved through
    ///         <see cref="ModelDb.GetByIdOrNull{T}" /> when a patch runs. Registration can therefore occur during a mod's
    ///         <c>Apply()</c> method, before <see cref="ModelDb" /> has added mod content to <c>_contentById</c>. Starter keys
    ///         use the metadata-only <see cref="ModelDb.GetId{T}" /> method.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         目标模型以 CLR <see cref="Type" /> 值保存，并在补丁运行时通过 <see cref="ModelDb.GetByIdOrNull{T}" />
    ///         解析。因此，模组可在 <c>Apply()</c> 方法中、<see cref="ModelDb" /> 尚未将模组内容加入
    ///         <c>_contentById</c> 前完成注册。起始卡牌或遗物的键则通过仅使用元数据的
    ///         <see cref="ModelDb.GetId{T}" /> 方法取得。
    ///     </para>
    /// </remarks>
    internal static class OrobasAncientUpgradeRegistry
    {
        private static readonly Lock Sync = new();

        private static readonly List<OrobasUpgradeMapping> TranscendenceMappings = [];

        private static readonly List<OrobasUpgradeMapping> RefinementMappings = [];
        private static long _nextRegistrationOrder;

        internal static bool TryGetTranscendenceAncient(ModelId starterCardId,
            [NotNullWhen(true)] out CardModel? ancientTemplate)
        {
            OrobasUpgradeMapping? mapping;
            lock (Sync)
            {
                mapping = FindLatestMappingLocked(TranscendenceMappings, starterCardId);
                if (mapping == null)
                {
                    ancientTemplate = null;
                    return false;
                }
            }

            ancientTemplate = ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(mapping.TargetType));
            if (ancientTemplate is { Rarity: CardRarity.Ancient })
                return true;

            ancientTemplate = null;
            return false;
        }

        internal static bool TryGetRefinementUpgrade(ModelId starterRelicId,
            [NotNullWhen(true)] out RelicModel? upgradedTemplate)
        {
            OrobasUpgradeMapping? mapping;
            lock (Sync)
            {
                mapping = FindLatestMappingLocked(RefinementMappings, starterRelicId);
                if (mapping == null)
                {
                    upgradedTemplate = null;
                    return false;
                }
            }

            upgradedTemplate = ModelDb.GetByIdOrNull<RelicModel>(ModelDb.GetId(mapping.TargetType));
            return upgradedTemplate != null;
        }

        internal static bool HasTranscendenceStarter(ModelId starterCardId)
        {
            OrobasUpgradeMapping? mapping;
            lock (Sync)
            {
                mapping = FindLatestMappingLocked(TranscendenceMappings, starterCardId);
            }

            return mapping != null &&
                   ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(mapping.TargetType)) is
                       { Rarity: CardRarity.Ancient };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the distinct Ancient card templates registered by mods for
        ///         <see cref="ArchaicTooth.TranscendenceCards" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回模组为 <see cref="ArchaicTooth.TranscendenceCards" /> 注册的、不重复的先古卡牌模板。
        ///     </para>
        /// </summary>
        internal static IReadOnlyList<CardModel> GetRegisteredTranscendenceAncientTemplates()
        {
            Type[] types;
            lock (Sync)
            {
                types =
                [
                    .. TranscendenceMappings
                        .Select(static mapping => mapping.TargetType)
                        .Distinct()
                        .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal),
                ];
            }

            var seen = new HashSet<ModelId>();
            List<CardModel> list =
            [
                .. types.Select(ancientType => ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(ancientType)))
                    .OfType<CardModel>()
                    .Where(static card => card.Rarity == CardRarity.Ancient)
                    .Where(card => seen.Add(card.Id)),
            ];

            return list;
        }

        internal static void RegisterTranscendence(ModelId starterCardId, Type ancientCardType, string? modIdForLog)
        {
            EnsureModelType(ancientCardType, typeof(CardModel), nameof(ancientCardType));

            lock (Sync)
            {
                var previous = FindLatestExactMappingLocked(TranscendenceMappings, starterCardId, null);
                if (previous != null && previous.TargetType != ancientCardType)
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Transcendence mapping for starter card {starterCardId} " +
                        $"was replaced{(string.IsNullOrEmpty(modIdForLog) ? "" : $" (mod {modIdForLog})")}.");

                RemoveExactMappingsLocked(TranscendenceMappings, starterCardId, null);
                TranscendenceMappings.Add(new(starterCardId, null, ancientCardType, modIdForLog,
                    _nextRegistrationOrder++));
            }
        }

        internal static void RegisterTranscendence(Type starterCardType, Type ancientCardType, string? modIdForLog)
        {
            EnsureModelType(starterCardType, typeof(CardModel), nameof(starterCardType));
            EnsureModelType(ancientCardType, typeof(CardModel), nameof(ancientCardType));

            lock (Sync)
            {
                var previous = FindLatestExactMappingLocked(TranscendenceMappings, null, starterCardType);
                if (previous != null && previous.TargetType != ancientCardType)
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Transcendence mapping for starter card type {starterCardType.FullName} " +
                        $"was replaced{(string.IsNullOrEmpty(modIdForLog) ? "" : $" (mod {modIdForLog})")}.");

                RemoveExactMappingsLocked(TranscendenceMappings, null, starterCardType);
                TranscendenceMappings.Add(new(null, starterCardType, ancientCardType, modIdForLog,
                    _nextRegistrationOrder++));
            }
        }

        internal static void RegisterRefinement(ModelId starterRelicId, Type upgradedRelicType, string? modIdForLog)
        {
            EnsureModelType(upgradedRelicType, typeof(RelicModel), nameof(upgradedRelicType));

            lock (Sync)
            {
                var previous = FindLatestExactMappingLocked(RefinementMappings, starterRelicId, null);
                if (previous != null && previous.TargetType != upgradedRelicType)
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Refinement mapping for starter relic {starterRelicId} " +
                        $"was replaced{(string.IsNullOrEmpty(modIdForLog) ? "" : $" (mod {modIdForLog})")}.");

                RemoveExactMappingsLocked(RefinementMappings, starterRelicId, null);
                RefinementMappings.Add(new(starterRelicId, null, upgradedRelicType, modIdForLog,
                    _nextRegistrationOrder++));
            }
        }

        internal static void RegisterRefinement(Type starterRelicType, Type upgradedRelicType, string? modIdForLog)
        {
            EnsureModelType(starterRelicType, typeof(RelicModel), nameof(starterRelicType));
            EnsureModelType(upgradedRelicType, typeof(RelicModel), nameof(upgradedRelicType));

            lock (Sync)
            {
                var previous = FindLatestExactMappingLocked(RefinementMappings, null, starterRelicType);
                if (previous != null && previous.TargetType != upgradedRelicType)
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Refinement mapping for starter relic type {starterRelicType.FullName} " +
                        $"was replaced{(string.IsNullOrEmpty(modIdForLog) ? "" : $" (mod {modIdForLog})")}.");

                RemoveExactMappingsLocked(RefinementMappings, null, starterRelicType);
                RefinementMappings.Add(new(null, starterRelicType, upgradedRelicType, modIdForLog,
                    _nextRegistrationOrder++));
            }
        }

        internal static void ValidateFrozenRegistrations()
        {
            OrobasUpgradeMapping[] transcendence;
            OrobasUpgradeMapping[] refinement;
            lock (Sync)
            {
                transcendence = [.. TranscendenceMappings];
                refinement = [.. RefinementMappings];
            }

            foreach (var mapping in transcendence)
            {
                ValidateMapping(mapping, "Transcendence", typeof(CardModel), typeof(CardModel));
                var target = ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(mapping.TargetType));
                if (target is { Rarity: not CardRarity.Ancient })
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Ignoring non-Ancient transcendence target {target.Id} for " +
                        $"{mapping.StarterDescription}.");
            }

            foreach (var mapping in refinement)
                ValidateMapping(mapping, "Refinement", typeof(RelicModel), typeof(RelicModel));
        }

        private static void ValidateMapping(OrobasUpgradeMapping mapping, string kind, Type starterBaseType,
            Type targetBaseType)
        {
            if (mapping.StarterType != null)
                RegistrationFreezeDiagnostics.WarnMissingModelType(
                    "OrobasAncientUpgrades",
                    mapping.ModId,
                    $"{kind} starter",
                    mapping.StarterType,
                    starterBaseType);
            else if (mapping.StarterId is { } starterId)
                RegistrationFreezeDiagnostics.WarnMissingModelId(
                    "OrobasAncientUpgrades",
                    mapping.ModId,
                    $"{kind} starter",
                    starterId,
                    starterBaseType);

            RegistrationFreezeDiagnostics.WarnMissingModelType(
                "OrobasAncientUpgrades",
                mapping.ModId,
                $"{kind} target for {mapping.StarterDescription}",
                mapping.TargetType,
                targetBaseType);
        }

        private static OrobasUpgradeMapping? FindLatestMappingLocked(
            IEnumerable<OrobasUpgradeMapping> mappings,
            ModelId starterId)
        {
            return mappings
                .Where(mapping => mapping.ResolveStarterId() == starterId)
                .OrderByDescending(static mapping => mapping.RegistrationOrder)
                .FirstOrDefault();
        }

        private static OrobasUpgradeMapping? FindLatestExactMappingLocked(
            IEnumerable<OrobasUpgradeMapping> mappings,
            ModelId? starterId,
            Type? starterType)
        {
            return mappings
                .Where(mapping => mapping.StarterId == starterId && mapping.StarterType == starterType)
                .OrderByDescending(static mapping => mapping.RegistrationOrder)
                .FirstOrDefault();
        }

        private static void RemoveExactMappingsLocked(
            List<OrobasUpgradeMapping> mappings,
            ModelId? starterId,
            Type? starterType)
        {
            mappings.RemoveAll(mapping => mapping.StarterId == starterId && mapping.StarterType == starterType);
        }

        private static void EnsureModelType(Type modelType, Type requiredBase, string paramName)
        {
            ArgumentNullException.ThrowIfNull(modelType, paramName);
            ArgumentNullException.ThrowIfNull(requiredBase);

            if (modelType.IsAbstract || modelType.IsInterface || modelType.ContainsGenericParameters ||
                !requiredBase.IsAssignableFrom(modelType))
                throw new ArgumentException(
                    $"Type '{modelType.FullName}' must be a closed concrete subtype of '{requiredBase.FullName}'.",
                    paramName);
        }

        private sealed record OrobasUpgradeMapping(
            ModelId? StarterId,
            Type? StarterType,
            Type TargetType,
            string? ModId,
            long RegistrationOrder)
        {
            public string StarterDescription => StarterType?.FullName ?? StarterId?.ToString() ?? "<unknown>";

            public ModelId ResolveStarterId()
            {
                return StarterId ?? ModelDb.GetId(StarterType!);
            }
        }
    }
}
