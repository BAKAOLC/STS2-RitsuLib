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
                    context.Unlocks.RequireEpochIfUnset(t, epochId);
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
                    context.Unlocks.RequireEpochIfUnset(t, epochId);
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
                    context.Unlocks.RequireEpochIfUnset(t, epochId);
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
    ///     Registers an <see cref="EpochModel" /> type into vanilla epoch discovery.
    ///     将一个 <see cref="EpochModel" /> 类型注册到原版 epoch 发现流程。
    /// </summary>
    public sealed class EpochPackEntry<TEpoch> : IModContentPackEntry
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Timeline.RegisterEpoch<TEpoch>();
        }
    }

    /// <summary>
    ///     Registers a <see cref="StoryModel" /> type into vanilla story discovery.
    ///     将一个 <see cref="StoryModel" /> 类型注册到原版 story 发现流程。
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
    ///     Registers an epoch and appends it to a story column.
    ///     注册一个 epoch，并将它追加到 story 列中。
    /// </summary>
    public sealed class StoryEpochPackEntry<TStory, TEpoch> : IModContentPackEntry
        where TStory : StoryModel, new()
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Timeline.RegisterStoryEpoch<TStory, TEpoch>();
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.RequireEpoch{TModel, TEpoch}" />.
    ///     <see cref="ModUnlockRegistry.RequireEpoch{TModel, TEpoch}" /> 的 pack entry。
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
    ///     <see cref="PackDeclaredCardUnlockEpochTemplate" />。
    ///     对 <typeparamref name="TEpoch" /> 的 <see cref="CardUnlockEpochTemplate.EnumerateUnlockCardTypes" /> 中每个 CLR 类型注册
    ///     <see cref="ModUnlockRegistry.RequireEpoch(Type,string)" />。如果你只想把卡牌列表放在 pack manifest 上，优先使用
    ///     <see cref="TimelineColumnPackEntry{TStory}" />（例如 <c>.Epoch&lt;TEpoch&gt;(e =&gt; e.Cards(...))</c>）与
    ///     <see cref="PackDeclaredCardUnlockEpochTemplate" />。
    ///     <see cref="PackDeclaredCardUnlockEpochTemplate" />。
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
    ///     <see cref="PackDeclaredRelicUnlockEpochTemplate" />。
    ///     对 <typeparamref name="TEpoch" /> 的 <see cref="RelicUnlockEpochTemplate.EnumerateUnlockRelicTypes" /> 中每个遗物类型注册
    ///     <see cref="ModUnlockRegistry.RequireEpoch(Type,string)" />。如果你只想把遗物列表放在 pack manifest 上，优先使用
    ///     <see cref="TimelineColumnPackEntry{TStory}" />（例如 <c>.Epoch&lt;TEpoch&gt;(e =&gt; e.Relics(...))</c>）与
    ///     <see cref="PackDeclaredRelicUnlockEpochTemplate" />。
    ///     <see cref="PackDeclaredRelicUnlockEpochTemplate" />。
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
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter, TEpoch}" /> 的 pack entry。
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
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter, TEpoch}" /> 的 pack entry。
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
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter, TEpoch}" />.
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter, TEpoch}" /> 的 pack entry。
    /// </summary>
    public sealed class UnlockEpochAfterAscensionWinPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _ascensionLevel;

        /// <summary>
        ///     Creates a rule with the given minimum ascension level.
        ///     创建一条使用给定最低进阶等级的规则。
        /// </summary>
        public UnlockEpochAfterAscensionWinPackEntry(int ascensionLevel)
        {
            _ascensionLevel = ascensionLevel;
        }

        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(_ascensionLevel);
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" />.
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" /> 的 pack entry。
    /// </summary>
    public sealed class UnlockEpochAfterRunCountPackEntry<TEpoch> : IModContentPackEntry
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredRuns;
        private readonly bool _requireVictory;

        /// <summary>
        ///     Creates a rule with the given run threshold.
        ///     创建一条使用给定 run 次数阈值的规则。
        /// </summary>
        public UnlockEpochAfterRunCountPackEntry(int requiredRuns, bool requireVictory = false)
        {
            _requiredRuns = requiredRuns;
            _requireVictory = requireVictory;
        }

        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockEpochAfterRunCount<TEpoch>(_requiredRuns, _requireVictory);
        }
    }

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter, TEpoch}" />.
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter, TEpoch}" /> 的 pack entry。
    /// </summary>
    public sealed class UnlockEpochAfterEliteVictoriesPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredEliteWins;

        /// <summary>
        ///     Creates a rule with the given elite-win threshold (default 15).
        ///     创建一条使用给定精英胜利阈值的规则（默认 15）。
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
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter, TEpoch}" /> 的 pack entry。
    /// </summary>
    public sealed class UnlockEpochAfterBossVictoriesPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredBossWins;

        /// <summary>
        ///     Creates a rule with the given boss-win threshold (default 15).
        ///     创建一条使用给定 Boss 胜利阈值的规则（默认 15）。
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
    ///     <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter, TEpoch}" /> 的 pack entry。
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
    ///     <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter, TEpoch}" /> 的 pack entry。
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

    /// <summary>
    ///     <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter, TEpoch}" />.
    ///     <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter, TEpoch}" /> 的 pack entry。
    /// </summary>
    public sealed class UnlockCharacterAfterRunAsPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        /// <inheritdoc />
        public void Apply(ModContentPackContext context)
        {
            context.Unlocks.UnlockCharacterAfterRunAs<TCharacter, TEpoch>();
        }
    }
}
