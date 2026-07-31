using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers secondary-resource cost hooks for model types that cannot implement
    ///         <see cref="ISecondaryResourceHookListener" /> directly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为无法直接实现 <see cref="ISecondaryResourceHookListener" /> 的模型类型注册次级资源费用钩子。
    ///     </para>
    /// </summary>
    public static class SecondaryResourceModelHookRegistry
    {
        private static readonly ConcurrentDictionary<Type, CostHooks> Hooks = new(new Dictionary<Type, CostHooks>
        {
            [typeof(VoidFormPower)] = new(null, ModifyVoidFormCostLate),
            [typeof(BrilliantScarf)] = new(null, ModifyBrilliantScarfCostLate),
        });

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces cost hooks for the exact model type <typeparamref name="TModel" />.</para>
        ///     <para xml:lang="zh-CN">为精确模型类型 <typeparamref name="TModel" /> 注册或替换费用钩子。</para>
        /// </summary>
        public static void RegisterCostHooks<TModel>(
            Func<TModel, SecondaryResourceCostContext, decimal, decimal>? modifyCost = null,
            Func<TModel, SecondaryResourceCostContext, decimal, decimal>? modifyCostLate = null)
            where TModel : AbstractModel
        {
            if (modifyCost == null && modifyCostLate == null)
                throw new ArgumentException("At least one cost hook must be provided.");

            RegisterCostHooks(
                typeof(TModel),
                modifyCost == null ? null : (model, context, cost) => modifyCost((TModel)model, context, cost),
                modifyCostLate == null ? null : (model, context, cost) => modifyCostLate((TModel)model, context, cost));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces cost hooks for an exact runtime model type.</para>
        ///     <para xml:lang="zh-CN">为精确的运行时模型类型注册或替换费用钩子。</para>
        /// </summary>
        public static void RegisterCostHooks(
            Type modelType,
            Func<AbstractModel, SecondaryResourceCostContext, decimal, decimal>? modifyCost = null,
            Func<AbstractModel, SecondaryResourceCostContext, decimal, decimal>? modifyCostLate = null)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            if (!modelType.IsAssignableTo(typeof(AbstractModel)))
                throw new ArgumentException($"{modelType.FullName} must derive from {typeof(AbstractModel).FullName}.",
                    nameof(modelType));
            if (modifyCost == null && modifyCostLate == null)
                throw new ArgumentException("At least one cost hook must be provided.");

            Hooks[modelType] = new(modifyCost, modifyCostLate);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes cost hooks for the exact model type <typeparamref name="TModel" />, including built-in
        ///         compatibility hooks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除精确模型类型 <typeparamref name="TModel" /> 的费用钩子，包括内置兼容性钩子。
        ///     </para>
        /// </summary>
        public static bool UnregisterCostHooks<TModel>()
            where TModel : AbstractModel
        {
            return UnregisterCostHooks(typeof(TModel));
        }

        /// <summary>
        ///     <para xml:lang="en">Removes cost hooks for an exact runtime model type.</para>
        ///     <para xml:lang="zh-CN">移除精确运行时模型类型的费用钩子。</para>
        /// </summary>
        public static bool UnregisterCostHooks(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            return Hooks.TryRemove(modelType, out _);
        }

        internal static ISecondaryResourceHookListener? Bind(AbstractModel model)
        {
            return Hooks.TryGetValue(model.GetType(), out var hooks)
                ? new BoundCostHooks(model, hooks)
                : null;
        }

        private static decimal ModifyVoidFormCostLate(
            AbstractModel model,
            SecondaryResourceCostContext context,
            decimal cost)
        {
            var power = (VoidFormPower)model;
            if (context.Card.Owner.Creature != power.Owner || !IsInHandOrPlay(context.Card))
                return cost;

            var data = power.GetInternalData<VoidFormPower.Data>();
            return data.cardsPlayedThisTurn < power.Amount ? 0m : cost;
        }

        private static decimal ModifyBrilliantScarfCostLate(
            AbstractModel model,
            SecondaryResourceCostContext context,
            decimal cost)
        {
            var relic = (BrilliantScarf)model;
            if (!CombatManager.Instance.IsInProgress ||
                context.Card.Owner.Creature != relic.Owner.Creature ||
                relic.DisplayAmount != relic.DynamicVars.Cards.BaseValue - 1m ||
                !IsInHandOrPlay(context.Card))
                return cost;

            return 0m;
        }

        private static bool IsInHandOrPlay(CardModel card)
        {
            return card.Pile?.Type is PileType.Hand or PileType.Play;
        }

        private readonly record struct CostHooks(
            Func<AbstractModel, SecondaryResourceCostContext, decimal, decimal>? ModifyCost,
            Func<AbstractModel, SecondaryResourceCostContext, decimal, decimal>? ModifyCostLate);

        private sealed class BoundCostHooks(
            AbstractModel model,
            CostHooks hooks) : ISecondaryResourceHookListener
        {
            public decimal ModifySecondaryResourceCost(SecondaryResourceCostContext context, decimal cost)
            {
                return hooks.ModifyCost?.Invoke(model, context, cost) ?? cost;
            }

            public decimal ModifySecondaryResourceCostLate(SecondaryResourceCostContext context, decimal cost)
            {
                return hooks.ModifyCostLate?.Invoke(model, context, cost) ?? cost;
            }
        }
    }
}
