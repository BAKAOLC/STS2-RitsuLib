using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <para xml:lang="en">Provides factories for common mod card <see cref="DynamicVar" /> types.</para>
    ///     <para xml:lang="zh-CN">提供常见模组卡牌 <see cref="DynamicVar" /> 类型的工厂方法。</para>
    /// </summary>
    public static class ModCardVars
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an integer dynamic variable with the specified name and value.</para>
        ///     <para xml:lang="zh-CN">创建具有指定名称和值的整数动态变量。</para>
        /// </summary>
        public static IntVar Int(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a string dynamic variable.</para>
        ///     <para xml:lang="zh-CN">创建字符串动态变量。</para>
        /// </summary>
        public static StringVar String(string name, string value = "")
        {
            return new(name, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a Boolean dynamic variable.</para>
        ///     <para xml:lang="zh-CN">创建布尔动态变量。</para>
        /// </summary>
        public static BoolVar Bool(string name, bool value = false)
        {
            return new(name, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default card-count variable.</para>
        ///     <para xml:lang="zh-CN">创建默认卡牌数量变量。</para>
        /// </summary>
        public static CardsVar Cards(int amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named card-count variable.</para>
        ///     <para xml:lang="zh-CN">创建具名卡牌数量变量。</para>
        /// </summary>
        public static CardsVar Cards(string name, int amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default damage variable.</para>
        ///     <para xml:lang="zh-CN">创建默认伤害变量。</para>
        /// </summary>
        public static DamageVar Damage(decimal amount, ValueProp props = ValueProp.Move)
        {
            return new(amount, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named damage variable.</para>
        ///     <para xml:lang="zh-CN">创建具名伤害变量。</para>
        /// </summary>
        public static DamageVar Damage(string name, decimal amount, ValueProp props = ValueProp.Move)
        {
            return new(name, amount, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default Osty damage variable.</para>
        ///     <para xml:lang="zh-CN">创建默认奥斯蒂伤害变量。</para>
        /// </summary>
        public static OstyDamageVar OstyDamage(decimal amount, ValueProp props = ValueProp.Move)
        {
            return new(amount, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named Osty damage variable.</para>
        ///     <para xml:lang="zh-CN">创建具名奥斯蒂伤害变量。</para>
        /// </summary>
        public static OstyDamageVar OstyDamage(string name, decimal amount, ValueProp props = ValueProp.Move)
        {
            return new(name, amount, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default Block variable.</para>
        ///     <para xml:lang="zh-CN">创建默认格挡变量。</para>
        /// </summary>
        public static BlockVar Block(decimal amount, ValueProp props = ValueProp.Move)
        {
            return new(amount, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named Block variable.</para>
        ///     <para xml:lang="zh-CN">创建具名格挡变量。</para>
        /// </summary>
        public static BlockVar Block(string name, decimal amount, ValueProp props = ValueProp.Move)
        {
            return new(name, amount, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default Gold variable.</para>
        ///     <para xml:lang="zh-CN">创建默认金币变量。</para>
        /// </summary>
        public static GoldVar Gold(int amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named Gold variable.</para>
        ///     <para xml:lang="zh-CN">创建具名金币变量。</para>
        /// </summary>
        public static GoldVar Gold(string name, int amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default healing variable.</para>
        ///     <para xml:lang="zh-CN">创建默认治疗变量。</para>
        /// </summary>
        public static HealVar Heal(decimal amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named healing variable.</para>
        ///     <para xml:lang="zh-CN">创建具名治疗变量。</para>
        /// </summary>
        public static HealVar Heal(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default HP-loss variable.</para>
        ///     <para xml:lang="zh-CN">创建默认生命损失变量。</para>
        /// </summary>
        public static HpLossVar HpLoss(decimal amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named HP-loss variable.</para>
        ///     <para xml:lang="zh-CN">创建具名生命损失变量。</para>
        /// </summary>
        public static HpLossVar HpLoss(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default maximum-HP variable.</para>
        ///     <para xml:lang="zh-CN">创建默认最大生命值变量。</para>
        /// </summary>
        public static MaxHpVar MaxHp(decimal amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named maximum-HP variable.</para>
        ///     <para xml:lang="zh-CN">创建具名最大生命值变量。</para>
        /// </summary>
        public static MaxHpVar MaxHp(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default repeat-count variable.</para>
        ///     <para xml:lang="zh-CN">创建默认重复次数变量。</para>
        /// </summary>
        public static RepeatVar Repeat(int amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named repeat-count variable.</para>
        ///     <para xml:lang="zh-CN">创建具名重复次数变量。</para>
        /// </summary>
        public static RepeatVar Repeat(string name, int amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default forge-count variable.</para>
        ///     <para xml:lang="zh-CN">创建默认锻造次数变量。</para>
        /// </summary>
        public static ForgeVar Forge(int amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named forge-count variable.</para>
        ///     <para xml:lang="zh-CN">创建具名锻造次数变量。</para>
        /// </summary>
        public static ForgeVar Forge(string name, int amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default summon-amount variable.</para>
        ///     <para xml:lang="zh-CN">创建默认召唤数量变量。</para>
        /// </summary>
        public static SummonVar Summon(decimal amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named summon-amount variable.</para>
        ///     <para xml:lang="zh-CN">创建具名召唤数量变量。</para>
        /// </summary>
        public static SummonVar Summon(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default energy-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建默认能量图标变量。</para>
        /// </summary>
        public static EnergyVar Energy(int amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named energy-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建具名能量图标变量。</para>
        /// </summary>
        public static EnergyVar Energy(string name, int amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the default star-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建默认星星图标变量。</para>
        /// </summary>
        public static StarsVar Stars(int amount)
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named star-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建具名星星图标变量。</para>
        /// </summary>
        public static StarsVar Stars(string name, int amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a power-amount variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名的能力层数变量。</para>
        /// </summary>
        public static PowerVar<T> Power<T>(decimal amount) where T : PowerModel
        {
            return new(amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named power-amount variable.</para>
        ///     <para xml:lang="zh-CN">创建具名能力层数变量。</para>
        /// </summary>
        public static PowerVar<T> Power<T>(string name, decimal amount) where T : PowerModel
        {
            return new(name, amount);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a <see cref="ComputedDynamicVar" /> with optional preview-specific evaluation.</para>
        ///     <para xml:lang="zh-CN">创建可指定预览求值逻辑的 <see cref="ComputedDynamicVar" />。</para>
        /// </summary>
        public static ComputedDynamicVar Computed(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target-aware <see cref="ComputedDynamicVar" />.</para>
        ///     <para xml:lang="zh-CN">创建支持目标感知求值的 <see cref="ComputedDynamicVar" />。</para>
        /// </summary>
        public static ComputedDynamicVar Computed(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed variable from a context-aware evaluator.</para>
        ///     <para xml:lang="zh-CN">使用上下文感知求值器创建计算型变量。</para>
        /// </summary>
        public static ComputedDynamicVar Computed(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
        {
            return new(name, baseValue, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed energy-icon variable compatible with the game's <c>energyIcons</c> formatter.</para>
        ///     <para xml:lang="zh-CN">创建兼容游戏 <c>energyIcons</c> 格式化器的计算型能量图标变量。</para>
        /// </summary>
        public static ComputedEnergyVar ComputedEnergy(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target-aware computed energy-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建支持目标感知求值的计算型能量图标变量。</para>
        /// </summary>
        public static ComputedEnergyVar ComputedEnergy(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed energy-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建上下文感知的计算型能量图标变量。</para>
        /// </summary>
        public static ComputedEnergyVar ComputedEnergy(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
        {
            return new(name, baseValue, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed star-icon variable compatible with the game's <c>starIcons</c> formatter.</para>
        ///     <para xml:lang="zh-CN">创建兼容游戏 <c>starIcons</c> 格式化器的计算型星星图标变量。</para>
        /// </summary>
        public static ComputedStarsVar ComputedStars(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target-aware computed star-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建支持目标感知求值的计算型星星图标变量。</para>
        /// </summary>
        public static ComputedStarsVar ComputedStars(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed star-icon variable.</para>
        ///     <para xml:lang="zh-CN">创建上下文感知的计算型星星图标变量。</para>
        /// </summary>
        public static ComputedStarsVar ComputedStars(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
        {
            return new(name, baseValue, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed power-amount variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名的计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPower<T>(
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            where T : PowerModel
        {
            return new(baseValue, currentValueFactory, previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named computed power-amount variable.</para>
        ///     <para xml:lang="zh-CN">创建具名计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPower<T>(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            where T : PowerModel
        {
            return new(name, baseValue, currentValueFactory, previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target-aware computed power-amount variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名且支持目标感知求值的计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPower<T>(
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            where T : PowerModel
        {
            return new(baseValue, currentValueFactory, previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named, target-aware computed power-amount variable.</para>
        ///     <para xml:lang="zh-CN">创建具名且支持目标感知求值的计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPower<T>(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            where T : PowerModel
        {
            return new(name, baseValue, currentValueFactory, previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed power-amount variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名的上下文感知计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPower<T>(
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
            where T : PowerModel
        {
            return new(typeof(T).Name, baseValue, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named, context-aware computed power-amount variable.</para>
        ///     <para xml:lang="zh-CN">创建具名的上下文感知计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPower<T>(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
            where T : PowerModel
        {
            return new(name, baseValue, factory);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a computed power-amount variable whose previews pass through
        ///         <see cref="Hook.ModifyPowerAmountGiven" /> when global hooks are enabled and combat context is
        ///         available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建计算型能力层数变量；启用全局钩子且存在战斗上下文时，预览值会经过
        ///         <see cref="Hook.ModifyPowerAmountGiven" />。
        ///     </para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPowerAmountGiven<T>(
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory)
            where T : PowerModel
        {
            return ComputedPowerAmountGivenCore<T>(typeof(T).Name, baseValue, currentValueFactory, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a named computed power-amount variable whose previews pass through
        ///         <see cref="Hook.ModifyPowerAmountGiven" /> when global hooks are enabled and combat context is
        ///         available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建具名计算型能力层数变量；启用全局钩子且存在战斗上下文时，预览值会经过
        ///         <see cref="Hook.ModifyPowerAmountGiven" />。
        ///     </para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPowerAmountGiven<T>(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory)
            where T : PowerModel
        {
            return ComputedPowerAmountGivenCore<T>(name, baseValue, currentValueFactory, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed power-amount variable with preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建支持预览专用基础值求值的计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPowerAmountGiven<T>(
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory)
            where T : PowerModel
        {
            return ComputedPowerAmountGivenCore<T>(
                typeof(T).Name,
                baseValue,
                currentValueFactory,
                previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named computed power-amount variable with preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建支持预览专用基础值求值的具名计算型能力层数变量。</para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPowerAmountGiven<T>(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory)
            where T : PowerModel
        {
            return ComputedPowerAmountGivenCore<T>(name, baseValue, currentValueFactory, previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a context-aware computed power amount whose previews pass through
        ///         <see cref="Hook.ModifyPowerAmountGiven" /> when requested and combat context is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建上下文感知的计算型能力层数；请求全局钩子且存在战斗上下文时，预览值会经过
        ///         <see cref="Hook.ModifyPowerAmountGiven" />。
        ///     </para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPowerAmountGiven<T>(
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
            where T : PowerModel
        {
            return ComputedPowerAmountGiven<T>(typeof(T).Name, factory, baseValue);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a named, context-aware computed power amount whose previews pass through
        ///         <see cref="Hook.ModifyPowerAmountGiven" /> when requested and combat context is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建具名的上下文感知计算型能力层数；请求全局钩子且存在战斗上下文时，预览值会经过
        ///         <see cref="Hook.ModifyPowerAmountGiven" />。
        ///     </para>
        /// </summary>
        public static ComputedPowerVar<T> ComputedPowerAmountGiven<T>(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m)
            where T : PowerModel
        {
            ArgumentNullException.ThrowIfNull(factory);

            return new(
                name,
                baseValue,
                context => CalculatePowerAmountGivenPreview<T>(context, factory(context)));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a computed damage variable whose previews pass through <see cref="Hook.ModifyDamage" /> when
        ///         global hooks are enabled and run and combat context are available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建计算型伤害变量；启用全局钩子且存在一局游戏与战斗上下文时，预览值会经过
        ///         <see cref="Hook.ModifyDamage" />。
        ///     </para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageCore(name, baseValue, currentValueFactory, null, card => card.Owner.Creature, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed damage variable with preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建支持预览专用基础值求值的计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageCore(
                name,
                baseValue,
                currentValueFactory,
                previewBaseValueFactory,
                card => card.Owner.Creature,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed damage variable whose value does not depend on a target.</para>
        ///     <para xml:lang="zh-CN">创建数值不依赖目标的计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            ValueProp props = ValueProp.Move)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            return ComputedDamage(name, baseValue, (card, _) => currentValueFactory(card), props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed damage variable with a custom damage dealer, such as Osty.</para>
        ///     <para xml:lang="zh-CN">创建可自定义伤害来源（例如奥斯蒂）的计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel, Creature?> dealerFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageCore(name, baseValue, currentValueFactory, null, dealerFactory, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed damage variable with a custom dealer and preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建可自定义伤害来源且支持预览专用基础值求值的计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            Func<CardModel, Creature?> dealerFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageCore(
                name,
                baseValue,
                currentValueFactory,
                previewBaseValueFactory,
                dealerFactory,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a context-aware computed damage variable whose previews apply enchantment modifiers and,
        ///         when requested context is available, normal damage hooks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建上下文感知的计算型伤害变量；预览值会应用附魔修正，并在请求钩子且所需上下文可用时应用普通伤害钩子。
        ///     </para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageFromContextCore(
                name,
                baseValue,
                factory,
                static context => context.SourceCreature,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed damage variable with a custom damage dealer.</para>
        ///     <para xml:lang="zh-CN">创建可自定义伤害来源的上下文感知计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedDamage(
            string name,
            ComputedDynamicVarFactory factory,
            Func<ComputedDynamicVarContext, Creature?> dealerFactory,
            decimal baseValue = 0m,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageFromContextCore(name, baseValue, factory, dealerFactory, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed damage variable whose damage dealer is the owner's Osty.</para>
        ///     <para xml:lang="zh-CN">创建伤害来源为拥有者奥斯蒂的计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedOstyDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageCore(name, baseValue, currentValueFactory, null, card => card.Owner.Osty, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed Osty damage variable with preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建支持预览专用基础值求值的奥斯蒂计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedOstyDamage(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageCore(
                name,
                baseValue,
                currentValueFactory,
                previewBaseValueFactory,
                card => card.Owner.Osty,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates context-aware computed damage whose dealer is the owner's Osty.</para>
        ///     <para xml:lang="zh-CN">创建伤害来源为拥有者奥斯蒂的上下文感知计算型伤害变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedOstyDamage(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m,
            ValueProp props = ValueProp.Move)
        {
            return ComputedDamageFromContextCore(
                name,
                baseValue,
                factory,
                static context => context.Player?.Osty,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a computed Block variable whose previews pass through <see cref="Hook.ModifyBlock" /> when
        ///         global hooks are enabled and combat context is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建计算型格挡变量；启用全局钩子且存在战斗上下文时，预览值会经过
        ///         <see cref="Hook.ModifyBlock" />。
        ///     </para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedBlockCore(name, baseValue, currentValueFactory, null, card => card.Owner.Creature, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed Block variable with preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建支持预览专用基础值求值的计算型格挡变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedBlockCore(
                name,
                baseValue,
                currentValueFactory,
                previewBaseValueFactory,
                card => card.Owner.Creature,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed Block variable whose value does not depend on a target.</para>
        ///     <para xml:lang="zh-CN">创建数值不依赖目标的计算型格挡变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            ValueProp props = ValueProp.Move)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            return ComputedBlock(name, baseValue, (card, _) => currentValueFactory(card), props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed Block variable with a custom receiver.</para>
        ///     <para xml:lang="zh-CN">创建可自定义格挡接收者的计算型格挡变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel, Creature> blockTargetFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedBlockCore(name, baseValue, currentValueFactory, null, blockTargetFactory, props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed Block variable with a custom receiver and preview-specific base-value evaluation.</para>
        ///     <para xml:lang="zh-CN">创建可自定义格挡接收者且支持预览专用基础值求值的计算型格挡变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            Func<CardModel, Creature> blockTargetFactory,
            ValueProp props = ValueProp.Move)
        {
            return ComputedBlockCore(
                name,
                baseValue,
                currentValueFactory,
                previewBaseValueFactory,
                blockTargetFactory,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a context-aware computed Block variable whose previews apply enchantment modifiers and,
        ///         when requested context is available, normal Block hooks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建上下文感知的计算型格挡变量；预览值会应用附魔修正，并在请求钩子且所需上下文可用时应用普通格挡钩子。
        ///     </para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            ComputedDynamicVarFactory factory,
            decimal baseValue = 0m,
            ValueProp props = ValueProp.Move)
        {
            return ComputedBlockFromContextCore(
                name,
                baseValue,
                factory,
                static context => context.SourceCreature,
                props);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed Block variable with a custom receiver.</para>
        ///     <para xml:lang="zh-CN">创建可自定义接收者的上下文感知计算型格挡变量。</para>
        /// </summary>
        public static ComputedDynamicVar ComputedBlock(
            string name,
            ComputedDynamicVarFactory factory,
            Func<ComputedDynamicVarContext, Creature?> blockTargetFactory,
            decimal baseValue = 0m,
            ValueProp props = ValueProp.Move)
        {
            return ComputedBlockFromContextCore(name, baseValue, factory, blockTargetFactory, props);
        }

        private static ComputedDynamicVar ComputedDamageFromContextCore(
            string name,
            decimal baseValue,
            ComputedDynamicVarFactory factory,
            Func<ComputedDynamicVarContext, Creature?> dealerFactory,
            ValueProp props)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(dealerFactory);

            return Computed(
                name,
                context => CalculateDamagePreview(
                    context,
                    factory(context),
                    dealerFactory,
                    props),
                baseValue);
        }

        private static ComputedDynamicVar ComputedBlockFromContextCore(
            string name,
            decimal baseValue,
            ComputedDynamicVarFactory factory,
            Func<ComputedDynamicVarContext, Creature?> blockTargetFactory,
            ValueProp props)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(blockTargetFactory);

            return Computed(
                name,
                context => CalculateBlockPreview(
                    context,
                    factory(context),
                    blockTargetFactory,
                    props),
                baseValue);
        }

        private static ComputedDynamicVar ComputedDamageCore(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory,
            Func<CardModel, Creature?> dealerFactory,
            ValueProp props)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);
            ArgumentNullException.ThrowIfNull(dealerFactory);

            return Computed(
                name,
                baseValue,
                currentValueFactory,
                (card, previewMode, target, runGlobalHooks) => CalculateDamagePreview(
                    card,
                    previewMode,
                    target,
                    runGlobalHooks,
                    previewBaseValueFactory ?? ((previewCard, _, previewTarget, _) =>
                        currentValueFactory(previewCard, previewTarget)),
                    dealerFactory,
                    props));
        }

        private static ComputedPowerVar<T> ComputedPowerAmountGivenCore<T>(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory)
            where T : PowerModel
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            return new(
                name,
                baseValue,
                currentValueFactory,
                (card, previewMode, target, runGlobalHooks) => CalculatePowerAmountGivenPreview<T>(
                    card,
                    previewMode,
                    target,
                    runGlobalHooks,
                    previewBaseValueFactory ?? ((previewCard, _, previewTarget, _) =>
                        currentValueFactory(previewCard, previewTarget))));
        }

        private static ComputedDynamicVar ComputedBlockCore(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory,
            Func<CardModel, Creature> blockTargetFactory,
            ValueProp props)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);
            ArgumentNullException.ThrowIfNull(blockTargetFactory);

            return Computed(
                name,
                baseValue,
                currentValueFactory,
                (card, previewMode, target, runGlobalHooks) => CalculateBlockPreview(
                    card,
                    previewMode,
                    target,
                    runGlobalHooks,
                    previewBaseValueFactory ?? ((previewCard, _, previewTarget, _) =>
                        currentValueFactory(previewCard, previewTarget)),
                    blockTargetFactory,
                    props));
        }

        private static decimal CalculateDamagePreview(
            CardModel? card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            Func<CardModel, Creature?> dealerFactory,
            ValueProp props)
        {
            var value = previewBaseValueFactory(card, previewMode, target, runGlobalHooks);
            if (card is null) return Math.Max(value, 0m);

            if (runGlobalHooks && card.RunState is { } runState)
            {
                var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
                return Math.Max(
                    Hook.ModifyDamage(
                        runState,
                        combatState,
                        target,
                        dealerFactory(card),
                        value,
                        props,
                        card,
#if STS2_AT_LEAST_0_108_0
                        null,
#endif
                        ModifyDamageHookType.All,
                        previewMode,
                        out _),
                    0m);
            }

            if (card.Enchantment is not { } enchantment) return Math.Max(value, 0m);
            value += enchantment.EnchantDamageAdditive(value, props);
            value *= enchantment.EnchantDamageMultiplicative(value, props);

            return Math.Max(value, 0m);
        }

        private static decimal CalculateDamagePreview(
            ComputedDynamicVarContext context,
            decimal value,
            Func<ComputedDynamicVarContext, Creature?> dealerFactory,
            ValueProp props)
        {
            if (context.PreviewMode is not { } previewMode || context.Card is not { } card)
                return Math.Max(value, 0m);

            var enchantedValue = value;
            if (card.Enchantment is { } enchantment)
            {
                enchantedValue += enchantment.EnchantDamageAdditive(enchantedValue, props);
                enchantedValue *= enchantment.EnchantDamageMultiplicative(enchantedValue, props);
                if (!context.IsEnchantmentPreview)
                    context.Variable.EnchantedValue = enchantedValue;
            }

            if (!context.RunGlobalHooks || context.RunState is not { } runState ||
                context.CombatState is not { } combatState)
                return Math.Max(enchantedValue, 0m);

            return Math.Max(
                Hook.ModifyDamage(
                    runState,
                    combatState,
                    context.Target,
                    dealerFactory(context),
                    value,
                    props,
                    card,
#if STS2_AT_LEAST_0_108_0
                    null,
#endif
                    ModifyDamageHookType.All,
                    previewMode,
                    out _),
                0m);
        }

        private static decimal CalculatePowerAmountGivenPreview<T>(
            CardModel? card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory)
            where T : PowerModel
        {
            var value = previewBaseValueFactory(card, previewMode, target, runGlobalHooks);
            if (card is null) return value;

            return runGlobalHooks && card.CombatState is { } combatState
                ? Hook.ModifyPowerAmountGiven(
                    combatState,
                    ModelDb.Power<T>(),
                    card.Owner.Creature,
                    value,
                    target,
                    card,
                    out _)
                : value;
        }

        private static decimal CalculatePowerAmountGivenPreview<T>(
            ComputedDynamicVarContext context,
            decimal value)
            where T : PowerModel
        {
            if (!context.IsPreview || !context.RunGlobalHooks || context.Card is not { } card ||
                context.CombatState is not { } combatState || context.SourceCreature is not { } source)
                return value;

            return Hook.ModifyPowerAmountGiven(
                combatState,
                ModelDb.Power<T>(),
                source,
                value,
                context.Target,
                card,
                out _);
        }

        private static decimal CalculateBlockPreview(
            CardModel? card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal> previewBaseValueFactory,
            Func<CardModel, Creature> blockTargetFactory,
            ValueProp props)
        {
            var value = previewBaseValueFactory(card, previewMode, target, runGlobalHooks);
            if (card is null) return value;

            if (runGlobalHooks && card.CombatState is { } combatState)
                return Hook.ModifyBlock(
                    combatState,
                    blockTargetFactory(card),
                    value,
                    props,
                    card,
                    null,
                    out _);

            if (card.Enchantment is not { } enchantment) return value;
#if STS2_AT_LEAST_0_106_0
            value += enchantment.EnchantBlockAdditive(value);
            value *= enchantment.EnchantBlockMultiplicative(value);
#else
            value += enchantment.EnchantBlockAdditive(value, props);
            value *= enchantment.EnchantBlockMultiplicative(value, props);
#endif

            return value;
        }

        private static decimal CalculateBlockPreview(
            ComputedDynamicVarContext context,
            decimal value,
            Func<ComputedDynamicVarContext, Creature?> blockTargetFactory,
            ValueProp props)
        {
            if (!context.IsPreview || context.Card is not { } card)
                return value;

            var enchantedValue = value;
            if (card.Enchantment is { } enchantment)
            {
#if STS2_AT_LEAST_0_106_0
                enchantedValue += enchantment.EnchantBlockAdditive(enchantedValue);
                enchantedValue *= enchantment.EnchantBlockMultiplicative(enchantedValue);
#else
                enchantedValue += enchantment.EnchantBlockAdditive(enchantedValue, props);
                enchantedValue *= enchantment.EnchantBlockMultiplicative(enchantedValue, props);
#endif
                if (!context.IsEnchantmentPreview)
                    context.Variable.EnchantedValue = enchantedValue;
            }

            if (!context.RunGlobalHooks || context.CombatState is not { } combatState ||
                blockTargetFactory(context) is not { } receiver)
                return enchantedValue;

            return Hook.ModifyBlock(
                combatState,
                receiver,
                value,
                props,
                card,
                null,
                out _);
        }
    }
}
