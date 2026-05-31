using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Context passed to card-description contributors.
    ///     传给卡牌描述组件的上下文。
    /// </summary>
    public readonly record struct CardDescriptionContext(
        CardModel Card,
        PileType PileType,
        Creature? Target,
        bool IsUpgradePreview);

    /// <summary>
    ///     Placement for component-provided card description fragments.
    ///     组件提供的卡牌描述片段插入位置。
    /// </summary>
    public enum CardDescriptionFragmentPlacement
    {
        /// <summary>
        ///     Insert before the card's own description.
        ///     插入到卡牌自身描述之前。
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     Insert after the card's own description.
        ///     插入到卡牌自身描述之后。
        /// </summary>
        AfterBase,
    }

    /// <summary>
    ///     Localized card description fragment contributed by a component.
    ///     由组件贡献的本地化卡牌描述片段。
    /// </summary>
    public readonly record struct CardDescriptionFragment(
        LocString Text,
        CardDescriptionFragmentPlacement Placement = CardDescriptionFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     Optional model capability that contributes localized card-description fragments.
    ///     可选组件能力：贡献本地化卡牌描述片段。
    /// </summary>
    public interface ICardDescriptionContributor
    {
        /// <summary>
        ///     Returns extra localized description fragments. Fragments are formatted through the game
        ///     <see cref="LocString" /> pipeline with the owning card's dynamic vars.
        ///     返回额外的本地化描述片段。片段会带所属卡牌动态变量进入游戏原生 <see cref="LocString" /> 格式化管线。
        /// </summary>
        IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context);
    }

    /// <summary>
    ///     Optional model capability that contributes card hover tips.
    ///     可选组件能力：贡献卡牌悬停提示。
    /// </summary>
    public interface ICardHoverTipContributor
    {
        /// <summary>
        ///     Returns additional hover tips for <paramref name="card" />.
        ///     返回 <paramref name="card" /> 的额外悬停提示。
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(CardModel card);
    }

    /// <summary>
    ///     Optional model capability that contributes hand glow predicates.
    ///     可选组件能力：贡献手牌发光判定。
    /// </summary>
    public interface ICardGlowContributor
    {
        /// <summary>
        ///     Returns true to make the card glow gold.
        ///     返回 true 时让卡牌显示金色发光。
        /// </summary>
        bool ShouldGlowGold(CardModel card)
        {
            return false;
        }

        /// <summary>
        ///     Returns true to make the card glow red.
        ///     返回 true 时让卡牌显示红色发光。
        /// </summary>
        bool ShouldGlowRed(CardModel card)
        {
            return false;
        }
    }

    internal static class CardModelCapabilityHost
    {
        internal static void UpdateDynamicVarPreviews(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target)
        {
            foreach (var component in GetCapabilities<IModelDynamicVarContributor>(card))
            {
                DynamicVarSet? dynamicVars = null;
                TryRun(component, card, () => dynamicVars = component.GetDynamicVars(card));
                if (dynamicVars == null)
                    continue;

                TryRun(component, card, () =>
                {
                    dynamicVars.ClearPreview();
                    card.UpdateDynamicVarPreview(previewMode, target, dynamicVars);
                });
            }
        }

        internal static void ApplyDescriptionFragments(CardDescriptionContext context, ref string description)
        {
            List<OrderedDescriptionFragment> beforeFragments = [];
            List<OrderedDescriptionFragment> afterFragments = [];
            var componentIndex = 0;
            foreach (var component in GetCapabilities<ICardDescriptionContributor>(context.Card))
            {
                var currentSourceIndex = componentIndex++;
                IReadOnlyList<CardDescriptionFragment> fragments;
                try
                {
                    fragments = (component.GetDescriptionFragments(context) ?? []).ToArray();
                }
                catch (Exception ex)
                {
                    LogFailure(component, context.Card, ex);
                    continue;
                }

                foreach (var (locString, cardDescriptionFragmentPlacement, order) in fragments)
                    TryRun(component, context.Card, () =>
                    {
                        PrepareDescriptionFragment(context, locString, component);

                        var text = locString.GetFormattedText();
                        if (string.IsNullOrWhiteSpace(text)) return;
                        var orderedFragment = new OrderedDescriptionFragment(
                            text,
                            order,
                            currentSourceIndex);
                        if (cardDescriptionFragmentPlacement == CardDescriptionFragmentPlacement.BeforeBase)
                            beforeFragments.Add(orderedFragment);
                        else
                            afterFragments.Add(orderedFragment);
                    });
            }

            if (beforeFragments.Count == 0 && afterFragments.Count == 0)
                return;

            var parts = beforeFragments
                .OrderBy(static fragment => fragment.Order)
                .ThenBy(static fragment => fragment.SourceIndex)
                .Select(static fragment => fragment.Text)
                .Concat(string.IsNullOrWhiteSpace(description) ? [] : [description])
                .Concat(afterFragments
                    .OrderBy(static fragment => fragment.Order)
                    .ThenBy(static fragment => fragment.SourceIndex)
                    .Select(static fragment => fragment.Text))
                .ToArray();

            description = string.Join('\n', parts);
        }

        internal static void AfterOwnerCardUpgraded(CardModel card)
        {
            foreach (var component in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(component, card))
                    continue;

                TryRun(component, card, () =>
                {
                    if (!IsStillAttachedToCard(component, card))
                        return;

                    component.NotifyOwnerCardUpgraded(card);
                    if (!IsStillAttachedToCard(component, card))
                        return;

                    component.RecalculateDynamicVarsForUpgradeOrEnchant();
                });
            }
        }

        internal static void AfterOwnerCardUpgradeFinalized(CardModel card)
        {
            foreach (var component in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(component, card))
                    continue;

                TryRun(component, card, () =>
                {
                    if (!IsStillAttachedToCard(component, card))
                        return;

                    component.FinalizeDynamicVarUpgrade();
                    if (!IsStillAttachedToCard(component, card))
                        return;

                    component.NotifyOwnerCardUpgradeFinalized(card);
                });
            }
        }

        internal static void AfterOwnerCardDowngraded(CardModel card)
        {
            foreach (var component in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(component, card))
                    continue;

                TryRun(component, card, () =>
                {
                    if (!IsStillAttachedToCard(component, card))
                        return;

                    component.NotifyOwnerCardDowngraded(card);
                });
            }
        }

        internal static void AfterOwnerCardTransformedFrom(CardModel card)
        {
            foreach (var component in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(component, card))
                    continue;

                TryRun(component, card, () => component.NotifyOwnerCardTransformedFrom(card));
            }
        }

        internal static void AfterOwnerCardTransformedTo(CardModel card)
        {
            foreach (var component in GetCapabilities<CardCapability>(card))
            {
                if (!IsStillAttachedToCard(component, card))
                    continue;

                TryRun(component, card, () => component.NotifyOwnerCardTransformedTo(card));
            }
        }

        private static void PrepareDescriptionFragment(
            CardDescriptionContext context,
            LocString locString,
            ICardDescriptionContributor source)
        {
            var card = context.Card;

            card.DynamicVars.AddTo(locString);
            if (source is IModelDynamicVarContributor dynamicVarComponent)
                dynamicVarComponent.GetDynamicVars(card).AddTo(locString);

            var upgradeDisplay = context.IsUpgradePreview
                ? UpgradeDisplay.UpgradePreview
                : card.IsUpgraded
                    ? UpgradeDisplay.Upgraded
                    : UpgradeDisplay.Normal;
            locString.Add(new IfUpgradedVar(upgradeDisplay));

            var onTable = context.PileType is PileType.Hand or PileType.Play;
            locString.Add("OnTable", onTable);
            locString.Add("InCombat",
                CombatManager.Instance.IsInProgress && (card.Pile?.IsCombatPile ?? context.PileType.IsCombatPile()));
            locString.Add("IsTargeting", context.Target != null);
            locString.Add("TargetType", card.TargetType.ToString());
            locString.Add("GainsBlock", card.GainsBlock);
            locString.Add("energyPrefix", EnergyIconHelper.GetPrefix(card));
            locString.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");

            var energyPrefix = EnergyIconHelper.GetPrefix(card);
            foreach (var value in locString.Variables.Values)
                if (value is EnergyVar energyVar)
                    energyVar.ColorPrefix = energyPrefix;
        }

        internal static IEnumerable<IHoverTip> GetHoverTips(CardModel card)
        {
            foreach (var tip in ModelCapabilityHost.GetHoverTips(card))
                yield return tip;

            foreach (var component in GetCapabilities<ICardHoverTipContributor>(card))
            {
                IEnumerable<IHoverTip> tips = [];
                TryRun(component, card, () => tips = component.GetHoverTips(card));

                foreach (var tip in tips)
                    yield return tip;
            }
        }

        internal static bool ShouldGlowGold(CardModel card)
        {
            foreach (var component in GetCapabilities<ICardGlowContributor>(card))
            {
                var shouldGlow = false;
                TryRun(component, card, () => shouldGlow = component.ShouldGlowGold(card));
                if (shouldGlow)
                    return true;
            }

            return false;
        }

        internal static bool ShouldGlowRed(CardModel card)
        {
            foreach (var component in GetCapabilities<ICardGlowContributor>(card))
            {
                var shouldGlow = false;
                TryRun(component, card, () => shouldGlow = component.ShouldGlowRed(card));
                if (shouldGlow)
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<TCapability> GetCapabilities<TCapability>(CardModel card)
            where TCapability : class
        {
            return ModelCapabilityHost.GetCapabilities<TCapability>(card).ToArray();
        }

        private static void TryRun<TCapability>(TCapability component, CardModel card, Action action)
            where TCapability : class
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogFailure(component, card, ex);
            }
        }

        private static void LogFailure<TCapability>(TCapability component, CardModel card, Exception ex)
            where TCapability : class
        {
            RitsuLibFramework.Logger.Warn(
                $"[ModelCapabilities] Card model capability '{component.GetType().FullName}' failed for {card.Id}: {ex.Message}");
        }

        private static bool IsStillAttachedToCard(CardCapability component, CardModel card)
        {
            return ReferenceEquals(component.Owner, card);
        }

        private readonly record struct OrderedDescriptionFragment(string Text, int Order, int SourceIndex);
    }
}
