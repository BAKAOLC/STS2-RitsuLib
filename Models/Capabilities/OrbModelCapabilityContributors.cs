using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Current orb value-label display state passed to orb value display contributors.
    ///     传给充能球数值标签贡献者的当前显示状态。
    /// </summary>
    public readonly record struct OrbValueDisplayContext(
        OrbModel Orb,
        bool IsEvoking,
        ModOrbValueDisplayMode DisplayMode,
        string PassiveText,
        string EvokeText);

    /// <summary>
    ///     Resolved orb value-label display state.
    ///     已解析的充能球数值标签显示状态。
    /// </summary>
    public readonly record struct OrbValueDisplayState(
        ModOrbValueDisplayMode DisplayMode,
        string PassiveText,
        string EvokeText);

    /// <summary>
    ///     Context passed to orb hover-tip description contributors.
    ///     传给充能球悬停说明贡献者的上下文。
    /// </summary>
    public readonly record struct OrbHoverTipDescriptionContext(
        OrbModel Orb,
        string BaseDescription,
        bool IsSmart);

    /// <summary>
    ///     Placement for capability-provided orb hover-tip description fragments.
    ///     能力提供的充能球悬停说明片段插入位置。
    /// </summary>
    public enum OrbHoverTipDescriptionFragmentPlacement
    {
        /// <summary>
        ///     Insert before the orb's own description.
        ///     插入到充能球自身说明之前。
        /// </summary>
        BeforeBase,

        /// <summary>
        ///     Insert after the orb's own description.
        ///     插入到充能球自身说明之后。
        /// </summary>
        AfterBase,
    }

    /// <summary>
    ///     Orb hover-tip description fragment contributed by a capability.
    ///     由能力贡献的充能球悬停说明片段。
    /// </summary>
    public readonly record struct OrbHoverTipDescriptionFragment(
        string Text,
        OrbHoverTipDescriptionFragmentPlacement Placement = OrbHoverTipDescriptionFragmentPlacement.AfterBase,
        int Order = 0);

    /// <summary>
    ///     Optional orb capability that contributes passive/evoke value-label display overrides.
    ///     可选充能球能力：贡献被动/激发数值标签显示覆盖。
    /// </summary>
    public interface IOrbValueDisplayContributor
    {
        /// <summary>
        ///     Returns a label visibility override, or null to keep the current mode.
        ///     返回标签可见性覆盖；返回 null 表示保持当前模式。
        /// </summary>
        ModOrbValueDisplayMode? GetValueDisplayMode(OrbValueDisplayContext context)
        {
            return null;
        }

        /// <summary>
        ///     Returns passive label text, or null to keep the current text.
        ///     返回被动标签文本；返回 null 表示保持当前文本。
        /// </summary>
        string? GetPassiveValueDisplayText(OrbValueDisplayContext context)
        {
            return null;
        }

        /// <summary>
        ///     Returns evoke label text, or null to keep the current text.
        ///     返回激发标签文本；返回 null 表示保持当前文本。
        /// </summary>
        string? GetEvokeValueDisplayText(OrbValueDisplayContext context)
        {
            return null;
        }
    }

    /// <summary>
    ///     Optional orb capability that contributes text to the owning orb's primary hover tip.
    ///     可选充能球能力：向所属充能球的主悬停提示贡献文本。
    /// </summary>
    public interface IOrbHoverTipDescriptionContributor
    {
        /// <summary>
        ///     Returns description fragments merged into the primary orb hover tip.
        ///     返回合并到主充能球悬停提示中的说明片段。
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

                TryRun(modelCapability, context.Orb, OrbValueDisplaySurface, () =>
                {
                    if (capability.GetValueDisplayMode(currentContext) is { } mode)
                        state = state with { DisplayMode = mode };

                    if (capability.GetPassiveValueDisplayText(currentContext) is { } passiveText)
                        state = state with { PassiveText = passiveText };

                    if (capability.GetEvokeValueDisplayText(currentContext) is { } evokeText)
                        state = state with { EvokeText = evokeText };
                });

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

            tips[index] = new HoverTip(orb.Title, description, orb.Icon)
            {
                Id = hoverTip.Id,
                IsSmart = hoverTip.IsSmart,
                IsDebuff = hoverTip.IsDebuff,
                IsInstanced = hoverTip.IsInstanced,
                ShouldOverrideTextOverflow = hoverTip.ShouldOverrideTextOverflow,
            };
            result = IHoverTip.RemoveDupes(tips);
        }

        private readonly record struct OrderedOrbHoverTipDescriptionFragment(
            string Text,
            int Order,
            int SourceIndex);
    }
}
