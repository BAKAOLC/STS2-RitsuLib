using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers in-hand outline color rules for <typeparamref name="TCard" />. The highest-priority
        ///         matching rule wins.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TCard" /> 注册手牌描边颜色规则。优先级最高的匹配规则生效。
        ///     </para>
        /// </summary>
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineRules<TCard> rules) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rules");
            ModCardHandOutlineRegistry.Register(rules);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers one in-hand outline rule for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册一条手牌描边规则。</para>
        /// </summary>
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard> rule) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rule");
            ModCardHandOutlineRegistry.Register(rule);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers multiple in-hand outline rules for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册多条手牌描边规则。</para>
        /// </summary>
        public void RegisterCardHandOutline<TCard>(params ModCardHandOutlineSwitchRule<TCard>[] rules)
            where TCard : CardModel
        {
            RegisterCardHandOutline(ModCardHandOutlineRules<TCard>.Of(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an in-hand outline resolver for <typeparamref name="TCard" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册手牌描边解析器。</para>
        /// </summary>
        public void RegisterCardHandOutline<TCard>(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
            where TCard : CardModel
        {
            RegisterCardHandOutline(ModCardHandOutlineSwitchRule<TCard>.Switch(
                colorWhen,
                priority,
                visibleWhenUnplayable,
                refreshEveryFrame));
        }
    }
}
