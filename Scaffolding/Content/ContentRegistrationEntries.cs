using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Ancients.Options;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Declarative manifest entry that registers content with a <see cref="ModContentRegistry" /> when applied.
    ///     声明式 manifest 条目，应用时会向 <see cref="ModContentRegistry" /> 注册内容。
    /// </summary>
    public interface IContentRegistrationEntry
    {
        /// <summary>
        ///     Performs the registration for this entry against <paramref name="registry" />.
        ///     针对 <paramref name="registry" /> 执行此条目的注册。
        /// </summary>
        void Register(ModContentRegistry registry);
    }

    /// <summary>
    ///     Registers a mod character model type.
    ///     注册一个 Mod 角色模型类型。
    /// </summary>
    /// <typeparam name="TCharacter">
    ///     Concrete <see cref="CharacterModel" /> to register.
    ///     要注册的具体 <see cref="CharacterModel" />。
    /// </typeparam>
    public sealed class CharacterRegistrationEntry<TCharacter> : IContentRegistrationEntry
        where TCharacter : CharacterModel
    {
        private readonly List<Action<ModContentRegistry>> _starterRegistrations = [];

        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacter<TCharacter>();

            foreach (var registration in _starterRegistrations)
                registration(registry);
        }

        /// <summary>
        ///     Appends starter-deck copies of <typeparamref name="TCard" /> when this character entry is registered.
        ///     注册此角色条目时，向初始牌组追加 <typeparamref name="TCard" /> 的若干复制。
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingCard<TCard>(int count = 1)
            where TCard : CardModel
        {
            return AddStartingCard<TCard>(count, 0);
        }

        /// <summary>
        ///     Appends starter-deck copies of <typeparamref name="TCard" /> when this character entry is registered.
        ///     注册此角色条目时，向初始牌组追加 <typeparamref name="TCard" /> 的若干复制。
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingCard<TCard>(int count, int order)
            where TCard : CardModel
        {
            _starterRegistrations.Add(registry =>
                registry.RegisterCharacterStarterCard<TCharacter, TCard>(count, order));
            return this;
        }

        /// <summary>
        ///     Appends starting relic copies of <typeparamref name="TRelic" /> when this character entry is registered.
        ///     注册此角色条目时，追加 <typeparamref name="TRelic" /> 的若干初始遗物复制。
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingRelic<TRelic>(int count = 1)
            where TRelic : RelicModel
        {
            return AddStartingRelic<TRelic>(count, 0);
        }

        /// <summary>
        ///     Appends starting relic copies of <typeparamref name="TRelic" /> when this character entry is registered.
        ///     注册此角色条目时，追加 <typeparamref name="TRelic" /> 的若干初始遗物复制。
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingRelic<TRelic>(int count, int order)
            where TRelic : RelicModel
        {
            _starterRegistrations.Add(registry =>
                registry.RegisterCharacterStarterRelic<TCharacter, TRelic>(count, order));
            return this;
        }

        /// <summary>
        ///     Appends starting potion copies of <typeparamref name="TPotion" /> when this character entry is registered.
        ///     注册此角色条目时，追加 <typeparamref name="TPotion" /> 的若干初始药水复制。
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingPotion<TPotion>(int count = 1)
            where TPotion : PotionModel
        {
            return AddStartingPotion<TPotion>(count, 0);
        }

        /// <summary>
        ///     Appends starting potion copies of <typeparamref name="TPotion" /> when this character entry is registered.
        ///     注册此角色条目时，追加 <typeparamref name="TPotion" /> 的若干初始药水复制。
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingPotion<TPotion>(int count, int order)
            where TPotion : PotionModel
        {
            _starterRegistrations.Add(registry =>
                registry.RegisterCharacterStarterPotion<TCharacter, TPotion>(count, order));
            return this;
        }
    }

    /// <summary>
    ///     Registers additional starter-deck copies of a card for an already-known character type.
    ///     为已知角色类型注册额外的初始牌组卡牌复制。
    /// </summary>
    public sealed class CharacterStarterCardRegistrationEntry<TCharacter, TCard>(int count, int order)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TCard : CardModel
    {
        /// <summary>
        ///     Legacy overload for binary compatibility; forwards to the primary constructor with default order <c>0</c>.
        ///     用于二进制兼容的旧重载；转发到主构造函数并使用默认排序 <c>0</c>。
        /// </summary>
        public CharacterStarterCardRegistrationEntry(int count = 1) : this(count, 0)
        {
        }

        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterStarterCard<TCharacter, TCard>(count, order);
        }
    }

    /// <summary>
    ///     Registers additional starting relic copies for an already-known character type.
    ///     为已知角色类型注册额外的初始遗物复制。
    /// </summary>
    public sealed class CharacterStarterRelicRegistrationEntry<TCharacter, TRelic>(int count, int order)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TRelic : RelicModel
    {
        /// <summary>
        ///     Legacy overload for binary compatibility; forwards to the primary constructor with default order <c>0</c>.
        ///     用于二进制兼容的旧重载；转发到主构造函数并使用默认排序 <c>0</c>。
        /// </summary>
        public CharacterStarterRelicRegistrationEntry(int count = 1) : this(count, 0)
        {
        }

        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterStarterRelic<TCharacter, TRelic>(count, order);
        }
    }

    /// <summary>
    ///     Registers additional starting potion copies for an already-known character type.
    ///     为已知角色类型注册额外的初始药水复制。
    /// </summary>
    public sealed class CharacterStarterPotionRegistrationEntry<TCharacter, TPotion>(int count, int order)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TPotion : PotionModel
    {
        /// <summary>
        ///     Legacy overload for binary compatibility; forwards to the primary constructor with default order <c>0</c>.
        ///     用于二进制兼容的旧重载；转发到主构造函数并使用默认排序 <c>0</c>。
        /// </summary>
        public CharacterStarterPotionRegistrationEntry(int count = 1) : this(count, 0)
        {
        }

        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterStarterPotion<TCharacter, TPotion>(count, order);
        }
    }

    /// <summary>
    ///     Registers direct asset replacement for a target character id (vanilla or mod).
    ///     为目标角色 id（原版或 Mod）注册直接资源替换。
    /// </summary>
    public sealed class CharacterAssetReplacementRegistrationEntry(
        string characterEntry,
        CharacterAssetProfile assetProfile) : IContentRegistrationEntry
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterAssetReplacement(characterEntry, assetProfile);
        }
    }

    /// <summary>
    ///     Registers a mod act model type.
    ///     注册一个 Mod Act 模型类型。
    /// </summary>
    /// <typeparam name="TAct">
    ///     Concrete <see cref="ActModel" /> to register.
    ///     要注册的具体 <see cref="ActModel" />。
    /// </typeparam>
    public sealed class ActRegistrationEntry<TAct> : IContentRegistrationEntry
        where TAct : ActModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAct<TAct>();
        }
    }

    /// <summary>
    ///     Registers a card type with its pool and optional public entry options.
    ///     将卡牌类型及其池和可选公开条目选项一起注册。
    /// </summary>
    /// <typeparam name="TPool">
    ///     Card pool model type.
    ///     卡牌池模型类型。
    /// </typeparam>
    /// <typeparam name="TCard">
    ///     Card model type.
    ///     CardModel类型。
    /// </typeparam>
    /// <param name="publicEntry">
    ///     Optional stable entry / visibility options.
    ///     可选的稳定条目/可见性选项。
    /// </param>
    public sealed class CardRegistrationEntry<TPool, TCard>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : CardPoolModel
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCard<TPool, TCard>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers <see cref="ModCardHandGlowRegistry" /> rules for a card type (gold/red hand highlights).
    ///     为卡牌类型注册 <see cref="ModCardHandGlowRegistry" /> 规则（金色/红色手牌高亮）。
    /// </summary>
    /// <typeparam name="TCard">
    ///     <see cref="CardModel" /> subtype.
    ///     <see cref="CardModel" /> 子类型。
    /// </typeparam>
    /// <param name="rules">
    ///     Predicate rules; merged with <see cref="ModCardHandGlowRules.Or" /> if registered twice.
    ///     谓词规则；重复注册时会通过 <see cref="ModCardHandGlowRules.Or" /> 合并。
    /// </param>
    public sealed class CardHandGlowRegistrationEntry<TCard>(ModCardHandGlowRules rules) : IContentRegistrationEntry
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCardHandGlow<TCard>(rules);
        }
    }

    /// <summary>
    ///     Registers <see cref="ModCardHandOutlineRegistry" /> tint rules for a card type (arbitrary hand-highlight colors).
    ///     为卡牌类型注册 <see cref="ModCardHandOutlineRegistry" /> 染色规则（任意手牌高亮颜色）。
    /// </summary>
    public sealed class CardHandOutlineRegistrationEntry<TCard> : IContentRegistrationEntry where TCard : CardModel
    {
        private readonly ModCardHandOutlineRules _rules;

        /// <summary>
        ///     Registers one or more custom hand-outline rules.
        ///     注册一条或多条自定义手牌描边规则。
        /// </summary>
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineRules<TCard> rules)
            : this(rules.ToUntyped(), true)
        {
        }

        /// <summary>
        ///     Registers one custom hand-outline rule.
        ///     注册一条自定义手牌描边规则。
        /// </summary>
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineSwitchRule<TCard> rule)
            : this(ModCardHandOutlineRules<TCard>.Of(rule))
        {
        }

        /// <summary>
        ///     Registers one or more type-erased custom hand-outline rules.
        ///     注册一条或多条类型擦除自定义手牌描边规则。
        /// </summary>
        [Obsolete("Use CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineRules<TCard>).")]
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineRules rules)
        {
            _rules = rules;
        }

        /// <summary>
        ///     Registers one type-erased custom hand-outline rule.
        ///     注册一条类型擦除自定义手牌描边规则。
        /// </summary>
        [Obsolete("Use CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineSwitchRule<TCard>).")]
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineSwitchRule rule)
            : this(ModCardHandOutlineRules.Of(rule), true)
        {
        }

        /// <summary>
        ///     Registers one legacy custom hand-outline rule.
        ///     注册一条旧版自定义手牌描边规则。
        /// </summary>
        [Obsolete(
            "Use CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineRules<TCard>) or CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineSwitchRule<TCard>).")]
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineRule rule)
        {
            _rules = ModCardHandOutlineRules.Of(rule.ToSwitchRule());
        }

        private CardHandOutlineRegistrationEntry(ModCardHandOutlineRules rules, bool _)
        {
            _rules = rules;
        }

        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            ModCardHandOutlineRegistry.Register<TCard>(_rules);
        }
    }

    /// <summary>
    ///     Registers a relic type with its pool and optional public entry options.
    ///     将遗物类型及其池和可选公开条目选项一起注册。
    /// </summary>
    /// <typeparam name="TPool">
    ///     Relic pool model type.
    ///     遗物池模型类型。
    /// </typeparam>
    /// <typeparam name="TRelic">
    ///     Relic model type.
    ///     RelicModel类型。
    /// </typeparam>
    /// <param name="publicEntry">
    ///     Optional stable entry / visibility options.
    ///     可选的稳定条目/可见性选项。
    /// </param>
    public sealed class RelicRegistrationEntry<TPool, TRelic>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : RelicPoolModel
        where TRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterRelic<TPool, TRelic>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a potion type with its pool and optional public entry options.
    ///     将药水类型及其池和可选公开条目选项一起注册。
    /// </summary>
    /// <typeparam name="TPool">
    ///     Potion pool model type.
    ///     药水池模型类型。
    /// </typeparam>
    /// <typeparam name="TPotion">
    ///     Potion model type.
    ///     药水模型类型。
    /// </typeparam>
    /// <param name="publicEntry">
    ///     Optional stable entry / visibility options.
    ///     可选的稳定条目/可见性选项。
    /// </param>
    public sealed class PotionRegistrationEntry<TPool, TPotion>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : PotionPoolModel
        where TPotion : PotionModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPotion<TPool, TPotion>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a standalone power model type.
    ///     注册一个独立能力模型类型。
    /// </summary>
    /// <typeparam name="TPower">
    ///     Concrete <see cref="PowerModel" />.
    ///     具体 <see cref="PowerModel" />。
    /// </typeparam>
    public sealed class PowerRegistrationEntry<TPower> : IContentRegistrationEntry
        where TPower : PowerModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPower<TPower>();
        }
    }

    /// <summary>
    ///     Registers a non-power health bar forecast source type.
    ///     注册一个非能力的生命条预测来源类型。
    /// </summary>
    /// <typeparam name="TSource">
    ///     Concrete forecast source type.
    ///     具体预测来源类型。
    /// </typeparam>
    /// <param name="sourceId">
    ///     Optional stable id; defaults to the source type name.
    ///     可选稳定 id；默认使用来源类型名。
    /// </param>
    public sealed class HealthBarForecastRegistrationEntry<TSource>(string? sourceId = null)
        : IContentRegistrationEntry
        where TSource : IHealthBarForecastSource, new()
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterHealthBarForecast<TSource>(registry.ModId, sourceId);
        }
    }

    /// <summary>
    ///     Registers a shared card pool type (not tied to a single character registration here).
    ///     注册一个共享卡牌池类型（此处不绑定到单个角色注册）。
    /// </summary>
    /// <typeparam name="TPool">
    ///     Concrete <see cref="CardPoolModel" />.
    ///     具体 <see cref="CardPoolModel" />。
    /// </typeparam>
    public sealed class SharedCardPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedCardPool<TPool>();
        }
    }

    /// <summary>
    ///     Registers a mod orb model type.
    ///     注册一个 mod 充能球模型类型。
    /// </summary>
    /// <typeparam name="TOrb">
    ///     Concrete <see cref="OrbModel" />.
    ///     具体 <see cref="OrbModel" />。
    /// </typeparam>
    public sealed class OrbRegistrationEntry<TOrb> : IContentRegistrationEntry
        where TOrb : OrbModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterOrb<TOrb>();
        }
    }

    /// <summary>
    ///     Registers a mod enchantment model type.
    ///     注册一个 mod 附魔模型类型。
    /// </summary>
    /// <typeparam name="TEnchantment">
    ///     Concrete <see cref="EnchantmentModel" />.
    ///     具体 <see cref="EnchantmentModel" />。
    /// </typeparam>
    public sealed class EnchantmentRegistrationEntry<TEnchantment> : IContentRegistrationEntry
        where TEnchantment : EnchantmentModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterEnchantment<TEnchantment>();
        }
    }

    /// <summary>
    ///     Registers a mod affliction model type.
    ///     注册一个 mod 苦痛模型类型。
    /// </summary>
    /// <typeparam name="TAffliction">
    ///     Concrete <see cref="AfflictionModel" />.
    ///     具体 <see cref="AfflictionModel" />。
    /// </typeparam>
    public sealed class AfflictionRegistrationEntry<TAffliction> : IContentRegistrationEntry
        where TAffliction : AfflictionModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAffliction<TAffliction>();
        }
    }

    /// <summary>
    ///     Registers a mod achievement model type.
    ///     注册一个 mod 成就模型类型。
    /// </summary>
    /// <typeparam name="TAchievement">
    ///     Concrete <see cref="AchievementModel" />.
    ///     具体 <see cref="AchievementModel" />。
    /// </typeparam>
    public sealed class AchievementRegistrationEntry<TAchievement> : IContentRegistrationEntry
        where TAchievement : AchievementModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAchievement<TAchievement>();
        }
    }

    /// <summary>
    ///     Registers a mod singleton model type.
    ///     注册一个 mod singleton 模型类型。
    /// </summary>
    /// <typeparam name="TSingleton">
    ///     Concrete <see cref="SingletonModel" />.
    ///     具体 <see cref="SingletonModel" />。
    /// </typeparam>
    public sealed class SingletonRegistrationEntry<TSingleton> : IContentRegistrationEntry
        where TSingleton : SingletonModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSingleton<TSingleton>();
        }
    }

    /// <summary>
    ///     Registers a mod modifier as a good daily modifier.
    ///     将一个 mod modifier 注册为正面每日 modifier。
    /// </summary>
    /// <typeparam name="TModifier">
    ///     Concrete <see cref="ModifierModel" />.
    ///     具体 <see cref="ModifierModel" />。
    /// </typeparam>
    public sealed class GoodModifierRegistrationEntry<TModifier> : IContentRegistrationEntry
        where TModifier : ModifierModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterGoodModifier<TModifier>();
        }
    }

    /// <summary>
    ///     Registers a mod modifier as a bad daily modifier.
    ///     将一个 mod modifier 注册为负面每日 modifier。
    /// </summary>
    /// <typeparam name="TModifier">
    ///     Concrete <see cref="ModifierModel" />.
    ///     具体 <see cref="ModifierModel" />。
    /// </typeparam>
    public sealed class BadModifierRegistrationEntry<TModifier> : IContentRegistrationEntry
        where TModifier : ModifierModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterBadModifier<TModifier>();
        }
    }

    /// <summary>
    ///     Registers a shared relic pool model type.
    ///     注册一个共享遗物池模型类型。
    /// </summary>
    /// <typeparam name="TPool">
    ///     Concrete <see cref="RelicPoolModel" />.
    ///     具体 <see cref="RelicPoolModel" />。
    /// </typeparam>
    public sealed class SharedRelicPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : RelicPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedRelicPool<TPool>();
        }
    }

    /// <summary>
    ///     Registers a shared potion pool model type.
    ///     注册一个共享药水池模型类型。
    /// </summary>
    /// <typeparam name="TPool">
    ///     Concrete <see cref="PotionPoolModel" />.
    ///     具体 <see cref="PotionPoolModel" />。
    /// </typeparam>
    public sealed class SharedPotionPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : PotionPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedPotionPool<TPool>();
        }
    }

    /// <summary>
    ///     Registers a mod monster model type.
    ///     注册一个 mod 怪物模型类型。
    /// </summary>
    /// <typeparam name="TMonster">
    ///     Concrete <see cref="MonsterModel" />.
    ///     具体 <see cref="MonsterModel" />。
    /// </typeparam>
    public sealed class MonsterRegistrationEntry<TMonster> : IContentRegistrationEntry
        where TMonster : MonsterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterMonster<TMonster>();
        }
    }

    /// <summary>
    ///     Registers a shared event model type.
    ///     注册一个共享EventModel类型。
    /// </summary>
    /// <typeparam name="TEvent">
    ///     Concrete <see cref="EventModel" />.
    ///     具体 <see cref="EventModel" />。
    /// </typeparam>
    public sealed class SharedEventRegistrationEntry<TEvent> : IContentRegistrationEntry
        where TEvent : EventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedEvent<TEvent>();
        }
    }

    /// <summary>
    ///     Registers an encounter model scoped to <typeparamref name="TAct" />.
    ///     注册一个限定在 <typeparamref name="TAct" /> 范围内的遭遇模型。
    /// </summary>
    public sealed class ActEncounterRegistrationEntry<TAct, TEncounter> : IContentRegistrationEntry
        where TAct : ActModel
        where TEncounter : EncounterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterActEncounter<TAct, TEncounter>();
        }
    }

    /// <summary>
    ///     Registers an encounter model merged into every act’s encounter list (see
    ///     <c>ModContentRegistry.RegisterGlobalEncounter&lt;TEncounter&gt;()</c>).
    ///     注册一个会合并到每个 act 遭遇列表中的遭遇模型（见
    /// </summary>
    /// <typeparam name="TEncounter">
    ///     Concrete <see cref="EncounterModel" />.
    ///     具体 <see cref="EncounterModel" />。
    /// </typeparam>
    public sealed class GlobalEncounterRegistrationEntry<TEncounter> : IContentRegistrationEntry
        where TEncounter : EncounterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterGlobalEncounter<TEncounter>();
        }
    }

    /// <summary>
    ///     Registers an event model scoped to <typeparamref name="TAct" />.
    ///     注册一个限定在 <typeparamref name="TAct" /> 范围内的EventModel。
    /// </summary>
    public sealed class ActEventRegistrationEntry<TAct, TEvent> : IContentRegistrationEntry
        where TAct : ActModel
        where TEvent : EventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterActEvent<TAct, TEvent>();
        }
    }

    /// <summary>
    ///     Registers a shared ancient event model type.
    ///     注册一个共享 ancient EventModel类型。
    /// </summary>
    /// <typeparam name="TAncient">
    ///     Concrete <see cref="AncientEventModel" />.
    ///     具体 <see cref="AncientEventModel" />。
    /// </typeparam>
    public sealed class SharedAncientRegistrationEntry<TAncient> : IContentRegistrationEntry
        where TAncient : AncientEventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedAncient<TAncient>();
        }
    }

    /// <summary>
    ///     Registers an ancient event model scoped to <typeparamref name="TAct" />.
    ///     注册一个限定在 <typeparamref name="TAct" /> 范围内的 ancient EventModel。
    /// </summary>
    public sealed class ActAncientRegistrationEntry<TAct, TAncient> : IContentRegistrationEntry
        where TAct : ActModel
        where TAncient : AncientEventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterActAncient<TAct, TAncient>();
        }
    }

    /// <summary>
    ///     Registers extra initial-option injection rules for a specific ancient model type.
    ///     为特定 ancient 模型类型注册额外初始选项注入规则。
    /// </summary>
    public sealed class AncientOptionRegistrationEntry<TAncient>(ModAncientOptionRule rule) : IContentRegistrationEntry
        where TAncient : AncientEventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAncientOption<TAncient>(rule);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder card from a stable entry stem.
    ///     通过稳定条目词干注册一张生成式占位卡牌。
    /// </summary>
    public sealed class PlaceholderCardRegistrationEntry<TPool>(
        string stableEntryStem,
        PlaceholderCardDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderCard<TPool>(stableEntryStem, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder card with explicit public entry options.
    ///     使用显式公开条目选项注册一张生成式占位卡牌。
    /// </summary>
    public sealed class PlaceholderCardFromOptionsRegistrationEntry<TPool>(
        ModelPublicEntryOptions publicEntry,
        PlaceholderCardDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderCard<TPool>(publicEntry, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder relic from a stable entry stem.
    ///     通过稳定条目词干注册一个生成式占位遗物。
    /// </summary>
    public sealed class PlaceholderRelicRegistrationEntry<TPool>(
        string stableEntryStem,
        PlaceholderRelicDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : RelicPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderRelic<TPool>(stableEntryStem, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder relic with explicit public entry options.
    ///     使用显式公开条目选项注册一个生成式占位遗物。
    /// </summary>
    public sealed class PlaceholderRelicFromOptionsRegistrationEntry<TPool>(
        ModelPublicEntryOptions publicEntry,
        PlaceholderRelicDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : RelicPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderRelic<TPool>(publicEntry, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder potion from a stable entry stem.
    ///     通过稳定条目词干注册一瓶生成式占位药水。
    /// </summary>
    public sealed class PlaceholderPotionRegistrationEntry<TPool>(
        string stableEntryStem,
        PlaceholderPotionDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : PotionPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderPotion<TPool>(stableEntryStem, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder potion with explicit public entry options.
    ///     使用显式公开条目选项注册一瓶生成式占位药水。
    /// </summary>
    public sealed class PlaceholderPotionFromOptionsRegistrationEntry<TPool>(
        ModelPublicEntryOptions publicEntry,
        PlaceholderPotionDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : PotionPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderPotion<TPool>(publicEntry, descriptor);
        }
    }

    /// <summary>
    ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping (starter deck card → ancient transform target).
    ///     注册一个 <see cref="ArchaicTooth" /> 超越映射（初始牌组卡牌 → ancient 转化目标）。
    /// </summary>
    /// <typeparam name="TStarterCard">
    ///     Deck card id to match.
    ///     要匹配的牌组卡牌 id。
    /// </typeparam>
    /// <typeparam name="TAncientCard">
    ///     Transform target prototype from <see cref="ModelDb.Card{T}" />.
    ///     来自 <see cref="ModelDb.Card{T}" /> 的转化目标原型。
    /// </typeparam>
    public sealed class
        ArchaicToothTranscendenceRegistrationEntry<TStarterCard, TAncientCard> : IContentRegistrationEntry
        where TStarterCard : CardModel
        where TAncientCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<TStarterCard, TAncientCard>(registry.ModId);
        }
    }

    /// <summary>
    ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping with explicit starter id and ancient card type.
    ///     使用显式初始卡牌 id 和 ancient 卡牌类型注册一个 <see cref="ArchaicTooth" /> 超越映射。
    /// </summary>
    /// <param name="StarterCardId">
    ///     Deck card model id to match.
    ///     要匹配的牌组CardModel id。
    /// </param>
    /// <param name="AncientCardType">
    ///     Concrete ancient card type (resolved via <see cref="ModelDb" /> at runtime).
    ///     具体 ancient 卡牌类型（运行时通过 <see cref="ModelDb" /> 解析）。
    /// </param>
    public sealed record ArchaicToothTranscendenceByIdRegistrationEntry(
        ModelId StarterCardId,
        Type AncientCardType) : IContentRegistrationEntry
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(
                StarterCardId,
                AncientCardType,
                registry.ModId);
        }
    }

    /// <summary>
    ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping (starter relic → upgraded relic).
    ///     注册一个 <see cref="TouchOfOrobas" /> 精炼映射（初始遗物 → 升级遗物）。
    /// </summary>
    /// <typeparam name="TStarterRelic">
    ///     Starter relic id to match.
    ///     要匹配的初始遗物 id。
    /// </typeparam>
    /// <typeparam name="TUpgradedRelic">
    ///     Replacement relic prototype from <see cref="ModelDb.Relic{T}" />.
    ///     来自 <see cref="ModelDb.Relic{T}" /> 的替换遗物原型。
    /// </typeparam>
    public sealed class
        TouchOfOrobasRefinementRegistrationEntry<TStarterRelic, TUpgradedRelic> : IContentRegistrationEntry
        where TStarterRelic : RelicModel
        where TUpgradedRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<TStarterRelic, TUpgradedRelic>(registry.ModId);
        }
    }

    /// <summary>
    ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping with explicit starter id and upgraded relic type.
    ///     使用显式初始遗物 id 和升级遗物类型注册一个 <see cref="TouchOfOrobas" /> 精炼映射。
    /// </summary>
    /// <param name="StarterRelicId">
    ///     Starter relic id to match.
    ///     要匹配的初始遗物 id。
    /// </param>
    /// <param name="UpgradedRelicType">
    ///     Concrete upgraded relic type (resolved via <see cref="ModelDb" /> at runtime).
    ///     具体升级遗物类型（运行时通过 <see cref="ModelDb" /> 解析）。
    /// </param>
    public sealed record TouchOfOrobasRefinementByIdRegistrationEntry(
        ModelId StarterRelicId,
        Type UpgradedRelicType) : IContentRegistrationEntry
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(
                StarterRelicId,
                UpgradedRelicType,
                registry.ModId);
        }
    }
}
