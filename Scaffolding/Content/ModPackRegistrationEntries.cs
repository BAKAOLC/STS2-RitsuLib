using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Content;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Scaffolding.Content
{
    internal static class ModEpochGatedContentPackHelper
    {
        internal static void ApplyExplicitTypes<TEpoch>(ModContentPackContext context, IReadOnlyList<Type> cardTypes,
            IReadOnlyList<Type> relicTypes) where TEpoch : EpochModel, new()
        {
            ApplyExplicitTypes(typeof(TEpoch), context, cardTypes, relicTypes);
        }

        internal static void ApplyExplicitTypes(Type epochType, ModContentPackContext context,
            IReadOnlyList<Type> cardTypes, IReadOnlyList<Type> relicTypes)
        {
            var cards = cardTypes ?? [];
            var relics = relicTypes ?? [];
            if (cards.Count == 0 && relics.Count == 0)
                throw new ArgumentException(
                    $"Epoch gated content for '{epochType.Name}' needs at least one card or relic type.");

            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            ModEpochGatedContentRegistry.Register(context.ModId, epochId, cards, relics);
            foreach (var t in cards)
                context.Unlocks.RequireEpoch(t, epochId);
            foreach (var t in relics)
                context.Unlocks.RequireEpoch(t, epochId);
        }

        internal static void ApplyRelicsFromPool<TEpoch, TRelicPool>(ModContentPackContext context)
            where TEpoch : EpochModel, new()
            where TRelicPool : RelicPoolModel
        {
            ApplyRelicsFromPool(typeof(TEpoch), typeof(TRelicPool), context);
        }

        internal static void ApplyRelicsFromPool(Type epochType, Type relicPoolType, ModContentPackContext context)
        {
            var types = ModContentRegistry.GetRegisteredModelsInPool(context.ModId, relicPoolType)
                .Where(static t => typeof(RelicModel).IsAssignableFrom(t))
                .ToArray();
            if (types.Length == 0)
                throw new InvalidOperationException(
                    $"Epoch gated relics: no relic types in pool '{relicPoolType.Name}' for mod '{context.ModId}'.");

            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            ModEpochGatedContentRegistry.Register(context.ModId, epochId, null, types);
            foreach (var t in types)
                context.Unlocks.RequireEpoch(t, epochId);
        }

        internal static void ApplyCardsFromPool<TEpoch, TCardPool>(ModContentPackContext context)
            where TEpoch : EpochModel, new()
            where TCardPool : CardPoolModel
        {
            ApplyCardsFromPool(typeof(TEpoch), typeof(TCardPool), context);
        }

        internal static void ApplyCardsFromPool(Type epochType, Type cardPoolType, ModContentPackContext context)
        {
            var types = ModContentRegistry.GetRegisteredModelsInPool(context.ModId, cardPoolType)
                .Where(static t => typeof(CardModel).IsAssignableFrom(t))
                .ToArray();
            if (types.Length == 0)
                throw new InvalidOperationException(
                    $"Epoch gated cards: no card types in pool '{cardPoolType.Name}' for mod '{context.ModId}'.");

            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            ModEpochGatedContentRegistry.Register(context.ModId, epochId, types, null);
            foreach (var t in types)
                context.Unlocks.RequireEpoch(t, epochId);
        }

        internal static void ApplyRequireAllPoolCards<TEpoch, TPool>(ModContentPackContext context)
            where TEpoch : EpochModel, new()
            where TPool : CardPoolModel
        {
            ApplyRequireAllPoolCards(typeof(TEpoch), typeof(TPool), context);
        }

        internal static void ApplyRequireAllPoolCards(Type epochType, Type poolType, ModContentPackContext context)
        {
            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            foreach (var t in ModContentRegistry.GetRegisteredModelsInPool(context.ModId, poolType))
                if (typeof(CardModel).IsAssignableFrom(t))
                    context.Unlocks.RequireEpoch(t, epochId);
        }

        internal static void ApplyRequireAllPoolRelics<TEpoch, TPool>(ModContentPackContext context)
            where TEpoch : EpochModel, new()
            where TPool : RelicPoolModel
        {
            ApplyRequireAllPoolRelics(typeof(TEpoch), typeof(TPool), context);
        }

        internal static void ApplyRequireAllPoolRelics(Type epochType, Type poolType, ModContentPackContext context)
        {
            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            foreach (var t in ModContentRegistry.GetRegisteredModelsInPool(context.ModId, poolType))
                if (typeof(RelicModel).IsAssignableFrom(t))
                    context.Unlocks.RequireEpoch(t, epochId);
        }

        internal static void ApplyRequireAllPoolPotions<TEpoch, TPool>(ModContentPackContext context)
            where TEpoch : EpochModel, new()
            where TPool : PotionPoolModel
        {
            ApplyRequireAllPoolPotions(typeof(TEpoch), typeof(TPool), context);
        }

        internal static void ApplyRequireAllPoolPotions(Type epochType, Type poolType, ModContentPackContext context)
        {
            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            foreach (var t in ModContentRegistry.GetRegisteredModelsInPool(context.ModId, poolType))
                if (typeof(PotionModel).IsAssignableFrom(t))
                    context.Unlocks.RequireEpoch(t, epochId);
        }

        internal static void ApplyExplicitPotions<TEpoch>(ModContentPackContext context, IReadOnlyList<Type> types)
            where TEpoch : EpochModel, new()
        {
            ApplyExplicitPotions(typeof(TEpoch), context, types);
        }

        internal static void ApplyExplicitPotions(Type epochType, ModContentPackContext context,
            IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            if (types.Count == 0)
                throw new ArgumentException(
                    $"Epoch potion gating for '{epochType.Name}' needs at least one potion type.");

            var epochId = ModTimelineRegistry.GetEpochId(epochType);
            foreach (var t in types)
            {
                if (!typeof(PotionModel).IsAssignableFrom(t))
                    throw new ArgumentException($"Type '{t.Name}' must derive from PotionModel.", nameof(types));

                context.Unlocks.RequireEpoch(t, epochId);
            }
        }
    }

    /// <summary>
    ///     Registers a <see cref="StoryModel" /> type into vanilla story discovery.
    /// </summary>
    public sealed class StoryPackEntry<TStory> : IModContentPackEntry
        where TStory : StoryModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Timeline.RegisterStory<TStory>();
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.RequireEpoch{TModel, TEpoch}" />.
    /// </summary>
    public sealed class RequireEpochPackEntry<TModel, TEpoch> : IModContentPackEntry
        where TModel : AbstractModel
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.RequireEpoch<TModel, TEpoch>();
        }
    }

    /// <summary>
    ///     For each CLR type in <typeparamref name="TEpoch" />’s
    ///     <see cref="CardUnlockEpochTemplate.EnumerateUnlockCardTypes" />,
    ///     registers <see cref="ModUnlockRegistry.RequireEpoch(Type,string)" />. Prefer
    ///     <see cref="TimelineColumnPackEntry{TStory}" /> (e.g. <c>.Epoch&lt;TEpoch&gt;(e =&gt; e.Cards(...))</c>) with
    ///     <see cref="PackDeclaredCardUnlockEpochTemplate" /> when you want card lists on the pack manifest only.
    /// </summary>
    public sealed class BindCardUnlockEpochPackEntry<TEpoch> : IModContentPackEntry
        where TEpoch : CardUnlockEpochTemplate, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            var epoch = new TEpoch();
            var id = epoch.Id;
            foreach (var t in epoch.EnumerateUnlockCardTypes())
                context.Unlocks.RequireEpoch(t, id);
        }
    }

    /// <summary>
    ///     For each relic type in <typeparamref name="TEpoch" />’s
    ///     <see cref="RelicUnlockEpochTemplate.EnumerateUnlockRelicTypes" />,
    ///     registers <see cref="ModUnlockRegistry.RequireEpoch(Type,string)" />. Prefer
    ///     <see cref="TimelineColumnPackEntry{TStory}" /> (e.g. <c>.Epoch&lt;TEpoch&gt;(e =&gt; e.Relics(...))</c>) with
    ///     <see cref="PackDeclaredRelicUnlockEpochTemplate" /> when you want relic lists on the pack manifest only.
    /// </summary>
    public sealed class BindRelicUnlockEpochPackEntry<TEpoch> : IModContentPackEntry
        where TEpoch : RelicUnlockEpochTemplate, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            var epoch = new TEpoch();
            var id = epoch.Id;
            foreach (var t in epoch.EnumerateUnlockRelicTypes())
                context.Unlocks.RequireEpoch(t, id);
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter, TEpoch}" />.
    /// </summary>
    public sealed class UnlockEpochAfterRunAsPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterRunAs<TCharacter, TEpoch>();
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter, TEpoch}" />.
    /// </summary>
    public sealed class UnlockEpochAfterWinAsPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterWinAs<TCharacter, TEpoch>();
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter, TEpoch}" />.
    /// </summary>
    public sealed class UnlockEpochAfterEliteVictoriesPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredEliteWins;

        /// <summary>
        ///     Creates a rule with the given elite-win threshold (default 15).
        /// </summary>
        public UnlockEpochAfterEliteVictoriesPackEntry(int requiredEliteWins = 15)
        {
            _requiredEliteWins = requiredEliteWins;
        }

        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(_requiredEliteWins);
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter, TEpoch}" />.
    /// </summary>
    public sealed class UnlockEpochAfterBossVictoriesPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredBossWins;

        /// <summary>
        ///     Creates a rule with the given boss-win threshold (default 15).
        /// </summary>
        public UnlockEpochAfterBossVictoriesPackEntry(int requiredBossWins = 15)
        {
            _requiredBossWins = requiredBossWins;
        }

        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(_requiredBossWins);
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter, TEpoch}" />.
    /// </summary>
    public sealed class UnlockEpochAfterAscensionOneWinPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>();
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter, TEpoch}" />.
    /// </summary>
    public sealed class RevealAscensionAfterEpochPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.RevealAscensionAfterEpoch<TCharacter, TEpoch>();
        }
    }
}
