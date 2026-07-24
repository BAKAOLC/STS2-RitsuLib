using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Relics
{
    /// <summary>
    ///     Holds mod-supplied mappings for <see cref="ArchaicTooth" /> transcendence and
    ///     <see cref="TouchOfOrobas" /> refinement, applied via framework Harmony patches.
    ///     保存 mod 提供的 <see cref="ArchaicTooth" /> 超越和
    ///     <see cref="TouchOfOrobas" /> 精炼映射，并通过框架 Harmony patch 应用。
    /// </summary>
    /// <remarks>
    ///     Target models are stored as CLR <see cref="Type" /> and resolved through <see cref="ModelDb.GetByIdOrNull{T}" />
    ///     at patch time so registration can run during mod <c>Apply()</c> before <see cref="ModelDb" /> has injected mod
    ///     content into <c>_contentById</c>. Starter keys use <see cref="ModelDb.GetId{T}" /> (metadata only).
    ///     目标模型以 CLR <see cref="Type" /> 存储，并在 patch 时通过 <see cref="ModelDb.GetByIdOrNull{T}" /> 解析，
    ///     这样注册就能在 mod <c>Apply()</c> 期间、<see cref="ModelDb" /> 将 mod 内容注入
    ///     <c>_contentById</c> 之前运行。初始牌 key 使用 <see cref="ModelDb.GetId{T}" />（仅元数据）。
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
            return ancientTemplate != null;
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
            lock (Sync)
            {
                return FindLatestMappingLocked(TranscendenceMappings, starterCardId) != null;
            }
        }

        /// <summary>
        ///     Distinct ancient card templates registered by mods (for <see cref="ArchaicTooth.TranscendenceCards" />).
        ///     mod 注册的不同古代卡牌模板（用于 <see cref="ArchaicTooth.TranscendenceCards" />）。
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
                    .OfType<CardModel>().Where(card => seen.Add(card.Id)),
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
                ValidateMapping(mapping, "Transcendence", typeof(CardModel), typeof(CardModel));

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
            ArgumentNullException.ThrowIfNull(modelType);
            if (!requiredBase.IsAssignableFrom(modelType))
                throw new ArgumentException($"{modelType.Name} must derive from {requiredBase.Name}.", paramName);
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
