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

            foreach (var type in cards)
                if (type == null || !typeof(CardModel).IsAssignableFrom(type))
                    throw new ArgumentException(
                        $"Type '{type?.Name ?? "<null>"}' must derive from CardModel.",
                        nameof(cardTypes));

            foreach (var type in relics)
                if (type == null || !typeof(RelicModel).IsAssignableFrom(type))
                    throw new ArgumentException(
                        $"Type '{type?.Name ?? "<null>"}' must derive from RelicModel.",
                        nameof(relicTypes));

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
                if (t == null || !typeof(PotionModel).IsAssignableFrom(t))
                    throw new ArgumentException(
                        $"Type '{t?.Name ?? "<null>"}' must derive from PotionModel.",
                        nameof(types));

                context.Unlocks.RequireEpoch(t, epochId);
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers an <see cref="EpochModel" /> type with the base game's epoch discovery.</para>
    ///     <para xml:lang="zh-CN">将 <see cref="EpochModel" /> 类型注册到原版游戏的时代发现流程。</para>
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
    ///     <para xml:lang="en">Registers a <see cref="StoryModel" /> type with the base game's story discovery.</para>
    ///     <para xml:lang="zh-CN">将 <see cref="StoryModel" /> 类型注册到原版游戏的故事发现流程。</para>
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
    ///     <para xml:lang="en">Registers an epoch and appends it to a story column.</para>
    ///     <para xml:lang="zh-CN">注册一个时代，并将其追加到故事列。</para>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for <see cref="ModUnlockRegistry.RequireEpoch{TModel, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.RequireEpoch{TModel, TEpoch}" /> 添加内容包条目。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Requires this epoch for every card type returned by
    ///         <see cref="CardUnlockEpochTemplate.EnumerateUnlockCardTypes" />. To declare card lists only in the
    ///         content pack, prefer <see cref="TimelineColumnPackEntry{TStory}" /> with
    ///         <see cref="PackDeclaredCardUnlockEpochTemplate" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         要求 <see cref="CardUnlockEpochTemplate.EnumerateUnlockCardTypes" /> 返回的每种卡牌类型均需解锁此时代。
    ///         若只想在内容包中声明卡牌列表，请优先将 <see cref="TimelineColumnPackEntry{TStory}" /> 与
    ///         <see cref="PackDeclaredCardUnlockEpochTemplate" /> 配合使用。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Requires this epoch for every relic type returned by
    ///         <see cref="RelicUnlockEpochTemplate.EnumerateUnlockRelicTypes" />. To declare relic lists only in the
    ///         content pack, prefer <see cref="TimelineColumnPackEntry{TStory}" /> with
    ///         <see cref="PackDeclaredRelicUnlockEpochTemplate" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         要求 <see cref="RelicUnlockEpochTemplate.EnumerateUnlockRelicTypes" /> 返回的每种遗物类型均需解锁此时代。
    ///         若只想在内容包中声明遗物列表，请优先将 <see cref="TimelineColumnPackEntry{TStory}" /> 与
    ///         <see cref="PackDeclaredRelicUnlockEpochTemplate" /> 配合使用。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
    /// </summary>
    public sealed class UnlockEpochAfterAscensionWinPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _ascensionLevel;

        /// <summary>
        ///     <para xml:lang="en">Creates a rule with the specified minimum Ascension level.</para>
        ///     <para xml:lang="zh-CN">创建一条采用指定最低进阶等级的规则。</para>
        /// </summary>
        /// <param name="ascensionLevel">
        ///     <para xml:lang="en">The minimum Ascension level.</para>
        ///     <para xml:lang="zh-CN">最低进阶等级。</para>
        /// </param>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" /> 添加内容包条目。
    ///     </para>
    /// </summary>
    public sealed class UnlockEpochAfterRunCountPackEntry<TEpoch> : IModContentPackEntry
        where TEpoch : EpochModel, new()
    {
        private readonly bool _requireVictory;
        private readonly int _requiredRuns;

        /// <summary>
        ///     <para xml:lang="en">Creates a rule with the specified run-count threshold.</para>
        ///     <para xml:lang="zh-CN">创建一条采用指定游戏局数阈值的规则。</para>
        /// </summary>
        /// <param name="requiredRuns">
        ///     <para xml:lang="en">The required number of completed runs.</para>
        ///     <para xml:lang="zh-CN">要求完成的游戏局数。</para>
        /// </param>
        /// <param name="requireVictory">
        ///     <para xml:lang="en">Whether only victorious runs count toward the threshold.</para>
        ///     <para xml:lang="zh-CN">是否仅将获胜的一局游戏计入阈值。</para>
        /// </param>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
    /// </summary>
    public sealed class UnlockEpochAfterEliteVictoriesPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredEliteWins;

        /// <summary>
        ///     <para xml:lang="en">Creates a rule with the specified elite-victory threshold.</para>
        ///     <para xml:lang="zh-CN">创建一条采用指定精英战胜次数阈值的规则。</para>
        /// </summary>
        /// <param name="requiredEliteWins">
        ///     <para xml:lang="en">The required number of elite victories. The default is 15.</para>
        ///     <para xml:lang="zh-CN">要求战胜精英的次数，默认为 15。</para>
        /// </param>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
    /// </summary>
    public sealed class UnlockEpochAfterBossVictoriesPackEntry<TCharacter, TEpoch> : IModContentPackEntry
        where TCharacter : CharacterModel
        where TEpoch : EpochModel, new()
    {
        private readonly int _requiredBossWins;

        /// <summary>
        ///     <para xml:lang="en">Creates a rule with the specified boss-victory threshold.</para>
        ///     <para xml:lang="zh-CN">创建一条采用指定首领战胜次数阈值的规则。</para>
        /// </summary>
        /// <param name="requiredBossWins">
        ///     <para xml:lang="en">The required number of boss victories. The default is 15.</para>
        ///     <para xml:lang="zh-CN">要求战胜首领的次数，默认为 15。</para>
        /// </param>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Adds a content-pack entry for
    ///         <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter, TEpoch}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter, TEpoch}" /> 添加内容包条目。
    ///     </para>
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
