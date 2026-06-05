using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Factory helpers for common mod card <see cref="DynamicVar" /> shapes.
    ///     常见 mod 卡牌 <see cref="DynamicVar" /> 形态的工厂辅助方法。
    /// </summary>
    public static class ModCardVars
    {
        /// <summary>
        ///     Creates an integer-backed dynamic var named <paramref name="name" /> with amount
        ///     <paramref name="amount" />.
        ///     创建名为 <paramref name="name" />、数值为 <paramref name="amount" /> 的整数型动态变量。
        /// </summary>
        public static IntVar Int(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     Creates a string dynamic var named <paramref name="name" />.
        ///     创建名为 <paramref name="name" /> 的字符串动态变量。
        /// </summary>
        public static StringVar String(string name, string value = "")
        {
            return new(name, value);
        }

        /// <summary>
        ///     Creates a <see cref="ComputedDynamicVar" /> with optional preview-specific computation.
        ///     创建带可选预览专用计算的 <see cref="ComputedDynamicVar" />。
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
        ///     Creates a <see cref="ComputedDynamicVar" /> with target-aware computation.
        ///     创建支持目标感知计算的 <see cref="ComputedDynamicVar" />。
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
        ///     Creates a computed damage variable whose card preview is processed through
        ///     <see cref="Hook.ModifyDamage" /> when global hooks are enabled.
        ///     创建计算型伤害变量；启用全局 hook 的卡牌预览会经过 <see cref="Hook.ModifyDamage" />。
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
        ///     Creates a computed damage variable with preview-specific base-value computation.
        ///     创建支持预览专用基础值计算的计算型伤害变量。
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
        ///     Creates a computed damage variable whose value does not depend on a target.
        ///     创建不依赖目标的计算型伤害变量。
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
        ///     Creates a computed damage variable with a custom damage dealer, such as Osty.
        ///     创建可自定义伤害来源（例如奥斯蒂）的计算型伤害变量。
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
        ///     Creates a computed damage variable with a custom dealer and preview-specific base-value computation.
        ///     创建可自定义伤害来源、并支持预览专用基础值计算的计算型伤害变量。
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
        ///     Creates a computed damage variable whose damage dealer is the owner's Osty.
        ///     创建伤害来源为拥有者奥斯蒂的计算型伤害变量。
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
        ///     Creates an Osty damage variable with preview-specific base-value computation.
        ///     创建支持预览专用基础值计算的奥斯蒂伤害变量。
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
        ///     Creates a computed block variable whose card preview is processed through
        ///     <see cref="Hook.ModifyBlock" /> when global hooks are enabled.
        ///     创建计算型格挡变量；启用全局 hook 的卡牌预览会经过 <see cref="Hook.ModifyBlock" />。
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
        ///     Creates a computed block variable with preview-specific base-value computation.
        ///     创建支持预览专用基础值计算的计算型格挡变量。
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
        ///     Creates a computed block variable whose value does not depend on a target.
        ///     创建不依赖目标的计算型格挡变量。
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
        ///     Creates a computed block variable with a custom block receiver.
        ///     创建可自定义格挡接收者的计算型格挡变量。
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
        ///     Creates a computed block variable with a custom receiver and preview-specific base-value computation.
        ///     创建可自定义格挡接收者、并支持预览专用基础值计算的计算型格挡变量。
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
                        ModifyDamageHookType.All,
                        previewMode,
                        out _),
                    0m);
            }

            if (card is not { IsEnchantmentPreview: false, Enchantment: { } enchantment }) return Math.Max(value, 0m);
            value += enchantment.EnchantDamageAdditive(value, props);
            value *= enchantment.EnchantDamageMultiplicative(value, props);

            return Math.Max(value, 0m);
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

            if (card is not { IsEnchantmentPreview: false, Enchantment: { } enchantment }) return value;
#if STS2_AT_LEAST_0_106_0
            value += enchantment.EnchantBlockAdditive(value);
            value *= enchantment.EnchantBlockMultiplicative(value);
#else
            value += enchantment.EnchantBlockAdditive(value, props);
            value *= enchantment.EnchantBlockMultiplicative(value, props);
#endif

            return value;
        }
    }
}
