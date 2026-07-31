using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Current orb value-label display state passed to orb value display contributors.</para>
    ///     <para xml:lang="zh-CN">传给充能球数值标签贡献者的当前显示状态。</para>
    /// </summary>
    public readonly record struct OrbValueDisplayContext(
        OrbModel Orb,
        bool IsEvoking,
        ModOrbValueDisplayMode DisplayMode,
        string PassiveText,
        string EvokeText);

    /// <summary>
    ///     <para xml:lang="en">Resolved orb value-label display state.</para>
    ///     <para xml:lang="zh-CN">已解析的充能球数值标签显示状态。</para>
    /// </summary>
    public readonly record struct OrbValueDisplayState(
        ModOrbValueDisplayMode DisplayMode,
        string PassiveText,
        string EvokeText);

    /// <summary>
    ///     <para xml:lang="en">Context passed to orb hover-tip description contributors.</para>
    ///     <para xml:lang="zh-CN">传给充能球悬停说明贡献者的上下文。</para>
    /// </summary>
    public readonly record struct OrbHoverTipDescriptionContext(
        OrbModel Orb,
        string BaseDescription,
        bool IsSmart);

    /// <summary>
    ///     <para xml:lang="en">Placement for capability-provided orb hover-tip description fragments.</para>
    ///     <para xml:lang="zh-CN">能力提供的充能球悬停说明片段插入位置。</para>
    /// </summary>
    public enum OrbHoverTipDescriptionFragmentPlacement
    {
        /// <summary>
        ///     <para xml:lang="en">Insert before the orb's own description.</para>
        ///     <para xml:lang="zh-CN">插入到充能球自身说明之前。</para>
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     <para xml:lang="en">Insert after the orb's own description.</para>
        ///     <para xml:lang="zh-CN">插入到充能球自身说明之后。</para>
        /// </summary>
        AfterBase,
    }

    /// <summary>
    ///     <para xml:lang="en">Orb hover-tip description fragment contributed by a capability.</para>
    ///     <para xml:lang="zh-CN">由能力贡献的充能球悬停说明片段。</para>
    /// </summary>
    public readonly record struct OrbHoverTipDescriptionFragment(
        string Text,
        OrbHoverTipDescriptionFragmentPlacement Placement = OrbHoverTipDescriptionFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     <para xml:lang="en">Optional orb capability that overrides passive and evoke value-label display.</para>
    ///     <para xml:lang="zh-CN">可选充能球能力：覆盖被动与激发数值标签的显示方式。</para>
    /// </summary>
    public interface IOrbValueDisplayContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns a label visibility override, or <see langword="null" /> to keep the current mode.</para>
        ///     <para xml:lang="zh-CN">返回标签可见性覆盖；返回 <see langword="null" /> 表示保持当前模式。</para>
        /// </summary>
        ModOrbValueDisplayMode? GetValueDisplayMode(OrbValueDisplayContext context)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns passive label text, or <see langword="null" /> to keep the current text.</para>
        ///     <para xml:lang="zh-CN">返回被动标签文本；返回 <see langword="null" /> 表示保持当前文本。</para>
        /// </summary>
        string? GetPassiveValueDisplayText(OrbValueDisplayContext context)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns evoke label text, or <see langword="null" /> to keep the current text.</para>
        ///     <para xml:lang="zh-CN">返回激发标签文本；返回 <see langword="null" /> 表示保持当前文本。</para>
        /// </summary>
        string? GetEvokeValueDisplayText(OrbValueDisplayContext context)
        {
            return null;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional orb capability that contributes text to the owning orb's primary hover tip.</para>
    ///     <para xml:lang="zh-CN">可选充能球能力：向所属充能球的主悬停提示贡献文本。</para>
    /// </summary>
    public interface IOrbHoverTipDescriptionContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns description fragments merged into the primary orb hover tip.</para>
        ///     <para xml:lang="zh-CN">返回合并到主充能球悬停提示中的说明片段。</para>
        /// </summary>
        IEnumerable<OrbHoverTipDescriptionFragment> GetHoverTipDescriptionFragments(
            OrbHoverTipDescriptionContext context);
    }

    internal static partial class ModelCapabilityHost
    {
        private const string OrbValueDisplaySurface = "orb display/value-labels";
        private const string OrbHoverTipDescriptionSurface = "orb display/hover-tip-description";

        internal static OrbValueDisplayState ApplyOrbValueDisplay(OrbValueDisplayContext context)
        {
            var state = new OrbValueDisplayState(context.DisplayMode, context.PassiveText, context.EvokeText);
            var currentContext = context;

            foreach (var capability in GetCapabilities<IOrbValueDisplayContributor>(context.Orb))
            {
                if (capability is not IModelCapability modelCapability)
                    continue;

                // TryRun invokes the callback synchronously before currentContext advances to the next contributor.
                // ReSharper disable AccessToModifiedClosure
                TryRun(modelCapability, context.Orb, OrbValueDisplaySurface, () =>
                {
                    if (capability.GetValueDisplayMode(currentContext) is { } mode)
                        state = state with { DisplayMode = mode };

                    if (capability.GetPassiveValueDisplayText(currentContext) is { } passiveText)
                        state = state with { PassiveText = passiveText };

                    if (capability.GetEvokeValueDisplayText(currentContext) is { } evokeText)
                        state = state with { EvokeText = evokeText };
                });
                // ReSharper restore AccessToModifiedClosure

                currentContext = currentContext with
                {
                    DisplayMode = state.DisplayMode,
                    PassiveText = state.PassiveText,
                    EvokeText = state.EvokeText,
                };
            }

            return state;
        }

        internal static void ApplyOrbHoverTipDescriptionFragments(
            OrbModel orb,
            ref IEnumerable<IHoverTip> result)
        {
            var tips = result.ToList();
            var index = tips.FindIndex(tip => string.Equals(tip.Id, orb.Id.ToString(), StringComparison.Ordinal));
            if (index < 0 || tips[index] is not HoverTip hoverTip)
                return;

            var context = new OrbHoverTipDescriptionContext(orb, hoverTip.Description, hoverTip.IsSmart);
            List<OrderedOrbHoverTipDescriptionFragment> beforeFragments = [];
            List<OrderedOrbHoverTipDescriptionFragment> afterFragments = [];
            var capabilityIndex = 0;

            foreach (var capability in GetCapabilities<IOrbHoverTipDescriptionContributor>(orb))
            {
                if (capability is not IModelCapability modelCapability)
                    continue;

                var sourceIndex = capabilityIndex++;
                TryRun(modelCapability, orb, OrbHoverTipDescriptionSurface, () =>
                {
                    foreach (var fragment in capability.GetHoverTipDescriptionFragments(context) ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(fragment.Text))
                            continue;

                        var ordered = new OrderedOrbHoverTipDescriptionFragment(
                            fragment.Text,
                            fragment.Order,
                            sourceIndex);
                        if (fragment.Placement == OrbHoverTipDescriptionFragmentPlacement.BeforeBase)
                            beforeFragments.Add(ordered);
                        else
                            afterFragments.Add(ordered);
                    }
                });
            }

            if (beforeFragments.Count == 0 && afterFragments.Count == 0)
                return;

            var description = string.Join('\n',
                beforeFragments
                    .OrderBy(static fragment => fragment.Order)
                    .ThenBy(static fragment => fragment.SourceIndex)
                    .Select(static fragment => fragment.Text)
                    .Concat(string.IsNullOrWhiteSpace(hoverTip.Description) ? [] : [hoverTip.Description])
                    .Concat(afterFragments
                        .OrderBy(static fragment => fragment.Order)
                        .ThenBy(static fragment => fragment.SourceIndex)
                        .Select(static fragment => fragment.Text)));

            var replacement = new HoverTip(orb.Title, description, orb.Icon)
            {
                Id = hoverTip.Id,
                IsSmart = hoverTip.IsSmart,
                IsDebuff = hoverTip.IsDebuff,
                IsInstanced = hoverTip.IsInstanced,
                ShouldOverrideTextOverflow = hoverTip.ShouldOverrideTextOverflow,
            };
            if (hoverTip.CanonicalModel is { } canonicalModel)
                replacement.SetCanonicalModel(canonicalModel);

            tips[index] = replacement;
            result = IHoverTip.RemoveDupes(tips);
        }

        private readonly record struct OrderedOrbHoverTipDescriptionFragment(
            string Text,
            int Order,
            int SourceIndex);
    }
}
