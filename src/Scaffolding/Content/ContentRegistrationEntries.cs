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
    ///     <para xml:lang="en">
    ///         Represents a declarative manifest entry that registers content with a
    ///         <see cref="ModContentRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示向 <see cref="ModContentRegistry" /> 注册内容的声明式清单条目。
    ///     </para>
    /// </summary>
    public interface IContentRegistrationEntry
    {
        /// <summary>
        ///     <para xml:lang="en">Registers this entry with <paramref name="registry" />.</para>
        ///     <para xml:lang="zh-CN">向 <paramref name="registry" /> 注册此条目。</para>
        /// </summary>
        void Register(ModContentRegistry registry);
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a mod character model type.</para>
    ///     <para xml:lang="zh-CN">注册模组角色模型类型。</para>
    /// </summary>
    /// <typeparam name="TCharacter">
    ///     <para xml:lang="en">The concrete <see cref="CharacterModel" /> type to register.</para>
    ///     <para xml:lang="zh-CN">要注册的具体 <see cref="CharacterModel" /> 类型。</para>
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
        ///     <para xml:lang="en">
        ///         Appends <paramref name="count" /> copies of <typeparamref name="TCard" /> to the starting deck when
        ///         this character entry is registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册此角色条目时，向初始牌组追加 <paramref name="count" /> 张
        ///         <typeparamref name="TCard" />。
        ///     </para>
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingCard<TCard>(int count = 1)
            where TCard : CardModel
        {
            return AddStartingCard<TCard>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <paramref name="count" /> copies of <typeparamref name="TCard" /> to the starting deck at
        ///         the specified registration <paramref name="order" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按指定注册顺序 <paramref name="order" />，向初始牌组追加 <paramref name="count" /> 张
        ///         <typeparamref name="TCard" />。
        ///     </para>
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingCard<TCard>(int count, int order)
            where TCard : CardModel
        {
            _starterRegistrations.Add(registry =>
                registry.RegisterCharacterStarterCard<TCharacter, TCard>(count, order));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <paramref name="count" /> copies of <typeparamref name="TRelic" /> to the starting relics
        ///         when this character entry is registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册此角色条目时，向初始遗物追加 <paramref name="count" /> 个
        ///         <typeparamref name="TRelic" />。
        ///     </para>
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingRelic<TRelic>(int count = 1)
            where TRelic : RelicModel
        {
            return AddStartingRelic<TRelic>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <paramref name="count" /> copies of <typeparamref name="TRelic" /> to the starting relics at
        ///         the specified registration <paramref name="order" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按指定注册顺序 <paramref name="order" />，向初始遗物追加 <paramref name="count" /> 个
        ///         <typeparamref name="TRelic" />。
        ///     </para>
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingRelic<TRelic>(int count, int order)
            where TRelic : RelicModel
        {
            _starterRegistrations.Add(registry =>
                registry.RegisterCharacterStarterRelic<TCharacter, TRelic>(count, order));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <paramref name="count" /> copies of <typeparamref name="TPotion" /> to the starting potions
        ///         when this character entry is registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册此角色条目时，向初始药水追加 <paramref name="count" /> 瓶
        ///         <typeparamref name="TPotion" />。
        ///     </para>
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingPotion<TPotion>(int count = 1)
            where TPotion : PotionModel
        {
            return AddStartingPotion<TPotion>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <paramref name="count" /> copies of <typeparamref name="TPotion" /> to the starting potions
        ///         at the specified registration <paramref name="order" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按指定注册顺序 <paramref name="order" />，向初始药水追加 <paramref name="count" /> 瓶
        ///         <typeparamref name="TPotion" />。
        ///     </para>
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
    ///     <para xml:lang="en">Registers additional starting-deck cards for a known character type.</para>
    ///     <para xml:lang="zh-CN">为已知角色类型注册额外的初始牌组卡牌。</para>
    /// </summary>
    public sealed class CharacterStarterCardRegistrationEntry<TCharacter, TCard>(int count, int order)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TCard : CardModel
    {
        /// <summary>
        ///     <para xml:lang="en">Preserves the legacy constructor with registration order <c>0</c>.</para>
        ///     <para xml:lang="zh-CN">保留注册顺序为 <c>0</c> 的旧版构造函数。</para>
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
    ///     <para xml:lang="en">Registers additional starting relics for a known character type.</para>
    ///     <para xml:lang="zh-CN">为已知角色类型注册额外的初始遗物。</para>
    /// </summary>
    public sealed class CharacterStarterRelicRegistrationEntry<TCharacter, TRelic>(int count, int order)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TRelic : RelicModel
    {
        /// <summary>
        ///     <para xml:lang="en">Preserves the legacy constructor with registration order <c>0</c>.</para>
        ///     <para xml:lang="zh-CN">保留注册顺序为 <c>0</c> 的旧版构造函数。</para>
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
    ///     <para xml:lang="en">Registers additional starting potions for a known character type.</para>
    ///     <para xml:lang="zh-CN">为已知角色类型注册额外的初始药水。</para>
    /// </summary>
    public sealed class CharacterStarterPotionRegistrationEntry<TCharacter, TPotion>(int count, int order)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TPotion : PotionModel
    {
        /// <summary>
        ///     <para xml:lang="en">Preserves the legacy constructor with registration order <c>0</c>.</para>
        ///     <para xml:lang="zh-CN">保留注册顺序为 <c>0</c> 的旧版构造函数。</para>
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
    ///     <para xml:lang="en">Registers a direct asset replacement for a base-game or mod character ID.</para>
    ///     <para xml:lang="zh-CN">为游戏本体或模组角色 ID 注册直接资源替换。</para>
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
    ///     <para xml:lang="en">Registers a mod act model type.</para>
    ///     <para xml:lang="zh-CN">注册模组章节模型类型。</para>
    /// </summary>
    /// <typeparam name="TAct">
    ///     <para xml:lang="en">The concrete <see cref="ActModel" /> type to register.</para>
    ///     <para xml:lang="zh-CN">要注册的具体 <see cref="ActModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a card type in a card pool with optional public-entry settings.</para>
    ///     <para xml:lang="zh-CN">在牌池中注册卡牌类型，并应用可选的公开条目设置。</para>
    /// </summary>
    /// <typeparam name="TPool">
    ///     <para xml:lang="en">The card-pool model type.</para>
    ///     <para xml:lang="zh-CN">牌池模型类型。</para>
    /// </typeparam>
    /// <typeparam name="TCard">
    ///     <para xml:lang="en">The card model type.</para>
    ///     <para xml:lang="zh-CN">卡牌模型类型。</para>
    /// </typeparam>
    /// <param name="publicEntry">
    ///     <para xml:lang="en">The optional stable-entry and visibility settings.</para>
    ///     <para xml:lang="zh-CN">可选的稳定条目和可见性设置。</para>
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
    ///     <para xml:lang="en">
    ///         Registers gold and red hand-glow rules for a card type with
    ///         <see cref="ModCardHandGlowRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="ModCardHandGlowRegistry" /> 为卡牌类型注册金色和红色手牌发光规则。
    ///     </para>
    /// </summary>
    /// <typeparam name="TCard">
    ///     <para xml:lang="en">The <see cref="CardModel" /> subtype.</para>
    ///     <para xml:lang="zh-CN"><see cref="CardModel" /> 子类型。</para>
    /// </typeparam>
    /// <param name="rules">
    ///     <para xml:lang="en">
    ///         The predicate rules, combined through <see cref="ModCardHandGlowRules.Or" /> with earlier
    ///         registrations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         谓词规则；存在较早注册时通过 <see cref="ModCardHandGlowRules.Or" /> 合并。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Registers custom hand-outline color rules for a card type with
    ///         <see cref="ModCardHandOutlineRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="ModCardHandOutlineRegistry" /> 为卡牌类型注册自定义手牌描边颜色规则。
    ///     </para>
    /// </summary>
    public sealed class CardHandOutlineRegistrationEntry<TCard> : IContentRegistrationEntry where TCard : CardModel
    {
        private readonly ModCardHandOutlineRules _rules;

        /// <summary>
        ///     <para xml:lang="en">Creates an entry from typed hand-outline rules.</para>
        ///     <para xml:lang="zh-CN">使用强类型手牌描边规则创建条目。</para>
        /// </summary>
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineRules<TCard> rules)
            : this(rules.ToUntyped(), true)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry from one typed hand-outline switch rule.</para>
        ///     <para xml:lang="zh-CN">使用一条强类型手牌描边切换规则创建条目。</para>
        /// </summary>
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineSwitchRule<TCard> rule)
            : this(ModCardHandOutlineRules<TCard>.Of(rule))
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry from legacy type-erased hand-outline rules.</para>
        ///     <para xml:lang="zh-CN">使用旧版类型擦除手牌描边规则创建条目。</para>
        /// </summary>
        [Obsolete("Use CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineRules<TCard>).")]
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineRules rules)
        {
            _rules = rules;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry from one legacy type-erased switch rule.</para>
        ///     <para xml:lang="zh-CN">使用一条旧版类型擦除切换规则创建条目。</para>
        /// </summary>
        [Obsolete("Use CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineSwitchRule<TCard>).")]
        public CardHandOutlineRegistrationEntry(ModCardHandOutlineSwitchRule rule)
            : this(ModCardHandOutlineRules.Of(rule), true)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry from one legacy hand-outline rule.</para>
        ///     <para xml:lang="zh-CN">使用一条旧版手牌描边规则创建条目。</para>
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
    ///     <para xml:lang="en">Registers a relic type in a relic pool with optional public-entry settings.</para>
    ///     <para xml:lang="zh-CN">在遗物池中注册遗物类型，并应用可选的公开条目设置。</para>
    /// </summary>
    /// <typeparam name="TPool">
    ///     <para xml:lang="en">The relic-pool model type.</para>
    ///     <para xml:lang="zh-CN">遗物池模型类型。</para>
    /// </typeparam>
    /// <typeparam name="TRelic">
    ///     <para xml:lang="en">The relic model type.</para>
    ///     <para xml:lang="zh-CN">遗物模型类型。</para>
    /// </typeparam>
    /// <param name="publicEntry">
    ///     <para xml:lang="en">The optional stable-entry and visibility settings.</para>
    ///     <para xml:lang="zh-CN">可选的稳定条目和可见性设置。</para>
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
    ///     <para xml:lang="en">Registers a potion type in a potion pool with optional public-entry settings.</para>
    ///     <para xml:lang="zh-CN">在药水池中注册药水类型，并应用可选的公开条目设置。</para>
    /// </summary>
    /// <typeparam name="TPool">
    ///     <para xml:lang="en">The potion-pool model type.</para>
    ///     <para xml:lang="zh-CN">药水池模型类型。</para>
    /// </typeparam>
    /// <typeparam name="TPotion">
    ///     <para xml:lang="en">The potion model type.</para>
    ///     <para xml:lang="zh-CN">药水模型类型。</para>
    /// </typeparam>
    /// <param name="publicEntry">
    ///     <para xml:lang="en">The optional stable-entry and visibility settings.</para>
    ///     <para xml:lang="zh-CN">可选的稳定条目和可见性设置。</para>
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
    ///     <para xml:lang="en">Registers a standalone power model type.</para>
    ///     <para xml:lang="zh-CN">注册独立的能力模型类型。</para>
    /// </summary>
    /// <typeparam name="TPower">
    ///     <para xml:lang="en">The concrete <see cref="PowerModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="PowerModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a non-power health-bar forecast source.</para>
    ///     <para xml:lang="zh-CN">注册不属于能力的生命条预测来源。</para>
    /// </summary>
    /// <typeparam name="TSource">
    ///     <para xml:lang="en">The concrete forecast-source type.</para>
    ///     <para xml:lang="zh-CN">具体预测来源类型。</para>
    /// </typeparam>
    /// <param name="sourceId">
    ///     <para xml:lang="en">The optional stable ID; defaults to the source type name.</para>
    ///     <para xml:lang="zh-CN">可选的稳定 ID；默认使用来源类型名称。</para>
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
    ///     <para xml:lang="en">Registers a shared card-pool type without attaching it to a character.</para>
    ///     <para xml:lang="zh-CN">注册不绑定到角色的共享牌池类型。</para>
    /// </summary>
    /// <typeparam name="TPool">
    ///     <para xml:lang="en">The concrete <see cref="CardPoolModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="CardPoolModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod orb model type.</para>
    ///     <para xml:lang="zh-CN">注册模组充能球模型类型。</para>
    /// </summary>
    /// <typeparam name="TOrb">
    ///     <para xml:lang="en">The concrete <see cref="OrbModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="OrbModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod enchantment model type.</para>
    ///     <para xml:lang="zh-CN">注册模组附魔模型类型。</para>
    /// </summary>
    /// <typeparam name="TEnchantment">
    ///     <para xml:lang="en">The concrete <see cref="EnchantmentModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="EnchantmentModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod affliction model type.</para>
    ///     <para xml:lang="zh-CN">注册模组侵蚀模型类型。</para>
    /// </summary>
    /// <typeparam name="TAffliction">
    ///     <para xml:lang="en">The concrete <see cref="AfflictionModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="AfflictionModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod achievement model type.</para>
    ///     <para xml:lang="zh-CN">注册模组成就模型类型。</para>
    /// </summary>
    /// <typeparam name="TAchievement">
    ///     <para xml:lang="en">The concrete <see cref="AchievementModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="AchievementModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod singleton model type.</para>
    ///     <para xml:lang="zh-CN">注册模组单例模型类型。</para>
    /// </summary>
    /// <typeparam name="TSingleton">
    ///     <para xml:lang="en">The concrete <see cref="SingletonModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="SingletonModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod modifier in the good daily-modifier pool.</para>
    ///     <para xml:lang="zh-CN">将模组修正项注册到正面每日修正项池。</para>
    /// </summary>
    /// <typeparam name="TModifier">
    ///     <para xml:lang="en">The concrete <see cref="ModifierModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="ModifierModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod modifier in the bad daily-modifier pool.</para>
    ///     <para xml:lang="zh-CN">将模组修正项注册到负面每日修正项池。</para>
    /// </summary>
    /// <typeparam name="TModifier">
    ///     <para xml:lang="en">The concrete <see cref="ModifierModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="ModifierModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a shared relic-pool model type.</para>
    ///     <para xml:lang="zh-CN">注册共享遗物池模型类型。</para>
    /// </summary>
    /// <typeparam name="TPool">
    ///     <para xml:lang="en">The concrete <see cref="RelicPoolModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="RelicPoolModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a shared potion-pool model type.</para>
    ///     <para xml:lang="zh-CN">注册共享药水池模型类型。</para>
    /// </summary>
    /// <typeparam name="TPool">
    ///     <para xml:lang="en">The concrete <see cref="PotionPoolModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="PotionPoolModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a mod monster model type.</para>
    ///     <para xml:lang="zh-CN">注册模组怪物模型类型。</para>
    /// </summary>
    /// <typeparam name="TMonster">
    ///     <para xml:lang="en">The concrete <see cref="MonsterModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="MonsterModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers a shared event model type.</para>
    ///     <para xml:lang="zh-CN">注册共享事件模型类型。</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">The concrete <see cref="EventModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="EventModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers an encounter model for <typeparamref name="TAct" />.</para>
    ///     <para xml:lang="zh-CN">为 <typeparamref name="TAct" /> 注册遭遇模型。</para>
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
    ///     <para xml:lang="en">Registers an encounter model in every act's encounter list.</para>
    ///     <para xml:lang="zh-CN">将遭遇模型注册到所有章节的遭遇列表中。</para>
    /// </summary>
    /// <typeparam name="TEncounter">
    ///     <para xml:lang="en">The concrete <see cref="EncounterModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="EncounterModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers an event model for <typeparamref name="TAct" />.</para>
    ///     <para xml:lang="zh-CN">为 <typeparamref name="TAct" /> 注册事件模型。</para>
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
    ///     <para xml:lang="en">Registers a shared Ancient event model type.</para>
    ///     <para xml:lang="zh-CN">注册共享先古之民事件模型类型。</para>
    /// </summary>
    /// <typeparam name="TAncient">
    ///     <para xml:lang="en">The concrete <see cref="AncientEventModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="AncientEventModel" /> 类型。</para>
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
    ///     <para xml:lang="en">Registers an Ancient event model for <typeparamref name="TAct" />.</para>
    ///     <para xml:lang="zh-CN">为 <typeparamref name="TAct" /> 注册先古之民事件模型。</para>
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
    ///     <para xml:lang="en">Registers an additional initial-option rule for an Ancient model type.</para>
    ///     <para xml:lang="zh-CN">为先古之民模型类型注册额外的初始选项规则。</para>
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
    ///     <para xml:lang="en">Registers a card candidate for the Trash Heap event's Grab option.</para>
    ///     <para xml:lang="zh-CN">为“垃圾堆”事件的“拿取”选项注册候选卡牌。</para>
    /// </summary>
    public sealed class TrashHeapCardRegistrationEntry<TCard> : IContentRegistrationEntry
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterTrashHeapCard<TCard>();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a relic candidate for the Trash Heap event's Dive In option.</para>
    ///     <para xml:lang="zh-CN">为“垃圾堆”事件的“深入翻找”选项注册候选遗物。</para>
    /// </summary>
    public sealed class TrashHeapRelicRegistrationEntry<TRelic> : IContentRegistrationEntry
        where TRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterTrashHeapRelic<TRelic>();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a generated placeholder card from a stable entry stem.</para>
    ///     <para xml:lang="zh-CN">使用稳定条目前缀注册生成的占位卡牌。</para>
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
    ///     <para xml:lang="en">
    ///         Registers a generated placeholder card with explicit public-entry settings.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用显式公开条目设置注册生成的占位卡牌。
    ///     </para>
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
    ///     <para xml:lang="en">Registers a generated placeholder relic from a stable entry stem.</para>
    ///     <para xml:lang="zh-CN">使用稳定条目前缀注册生成的占位遗物。</para>
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
    ///     <para xml:lang="en">
    ///         Registers a generated placeholder relic with explicit public-entry settings.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用显式公开条目设置注册生成的占位遗物。
    ///     </para>
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
    ///     <para xml:lang="en">Registers a generated placeholder potion from a stable entry stem.</para>
    ///     <para xml:lang="zh-CN">使用稳定条目前缀注册生成的占位药水。</para>
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
    ///     <para xml:lang="en">
    ///         Registers a generated placeholder potion with explicit public-entry settings.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用显式公开条目设置注册生成的占位药水。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Registers an <see cref="ArchaicTooth" /> transcendence mapping from a starting card type to an Ancient
    ///         card type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册从初始卡牌类型到先古卡牌类型的 <see cref="ArchaicTooth" /> 超越映射。
    ///     </para>
    /// </summary>
    /// <typeparam name="TStarterCard">
    ///     <para xml:lang="en">The starting card type to match.</para>
    ///     <para xml:lang="zh-CN">要匹配的初始卡牌类型。</para>
    /// </typeparam>
    /// <typeparam name="TAncientCard">
    ///     <para xml:lang="en">The Ancient card type used as the transformation target.</para>
    ///     <para xml:lang="zh-CN">作为转化目标的先古卡牌类型。</para>
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
    ///     <para xml:lang="en">
    ///         Registers an <see cref="ArchaicTooth" /> transcendence mapping from an explicit starting-card ID to an
    ///         Ancient card type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册从显式初始卡牌 ID 到先古卡牌类型的 <see cref="ArchaicTooth" /> 超越映射。
    ///     </para>
    /// </summary>
    /// <param name="StarterCardId">
    ///     <para xml:lang="en">The starting-card model ID to match.</para>
    ///     <para xml:lang="zh-CN">要匹配的初始卡牌模型 ID。</para>
    /// </param>
    /// <param name="AncientCardType">
    ///     <para xml:lang="en">The concrete Ancient card type resolved through <see cref="ModelDb" />.</para>
    ///     <para xml:lang="zh-CN">通过 <see cref="ModelDb" /> 解析的具体先古卡牌类型。</para>
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
    ///     <para xml:lang="en">Registers a character-to-Ancient-card candidate for <see cref="DustyTome" />.</para>
    ///     <para xml:lang="zh-CN">为 <see cref="DustyTome" /> 注册角色到先古卡牌的候选映射。</para>
    /// </summary>
    public sealed class DustyTomeCardRegistrationEntry<TCharacter, TAncientCard> : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TAncientCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterDustyTomeCard<TCharacter, TAncientCard>(registry.ModId);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers a <see cref="DustyTome" /> candidate with an explicit character ID and Ancient card type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用显式角色 ID 和先古卡牌类型注册 <see cref="DustyTome" /> 候选。
    ///     </para>
    /// </summary>
    public sealed record DustyTomeCardByIdRegistrationEntry(
        ModelId CharacterId,
        Type AncientCardType) : IContentRegistrationEntry
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterDustyTomeCard(CharacterId, AncientCardType, registry.ModId);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers a <see cref="TouchOfOrobas" /> refinement mapping from a starting relic type to an upgraded
    ///         relic type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册从初始遗物类型到升级遗物类型的 <see cref="TouchOfOrobas" /> 精炼映射。
    ///     </para>
    /// </summary>
    /// <typeparam name="TStarterRelic">
    ///     <para xml:lang="en">The starting relic type to match.</para>
    ///     <para xml:lang="zh-CN">要匹配的初始遗物类型。</para>
    /// </typeparam>
    /// <typeparam name="TUpgradedRelic">
    ///     <para xml:lang="en">The upgraded relic type used as the replacement.</para>
    ///     <para xml:lang="zh-CN">作为替换目标的升级遗物类型。</para>
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
    ///     <para xml:lang="en">
    ///         Registers a <see cref="TouchOfOrobas" /> refinement mapping from an explicit starting-relic ID to an
    ///         upgraded relic type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册从显式初始遗物 ID 到升级遗物类型的 <see cref="TouchOfOrobas" /> 精炼映射。
    ///     </para>
    /// </summary>
    /// <param name="StarterRelicId">
    ///     <para xml:lang="en">The starting-relic model ID to match.</para>
    ///     <para xml:lang="zh-CN">要匹配的初始遗物模型 ID。</para>
    /// </param>
    /// <param name="UpgradedRelicType">
    ///     <para xml:lang="en">The concrete upgraded relic type resolved through <see cref="ModelDb" />.</para>
    ///     <para xml:lang="zh-CN">通过 <see cref="ModelDb" /> 解析的具体升级遗物类型。</para>
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
