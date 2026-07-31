using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Forwards every entry behavior to an inner definition so a section builder can decorate one concern
    ///         without discarding the others.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将条目的全部行为转发到内部定义，使节构建器能够装饰单项功能而不丢失其他功能。
    ///     </para>
    /// </summary>
    /// <param name="inner">
    ///     <para xml:lang="en">The entry definition whose behavior is forwarded.</para>
    ///     <para xml:lang="zh-CN">其行为将被转发的条目定义。</para>
    /// </param>
    internal abstract class ModSettingsEntryDecorator(ModSettingsEntryDefinition inner)
        : ModSettingsEntryDefinition(inner.Id, inner.Label, inner.Description)
    {
        internal ModSettingsEntryDefinition Inner { get; } = inner;

        public override Func<bool>? VisibilityPredicate => Inner.VisibilityPredicate;

        public override Func<bool>? EnabledPredicate => Inner.EnabledPredicate;

        internal override string? VisibilityTargetPageId => Inner.VisibilityTargetPageId;

        internal override bool CanResetToDefault => Inner.CanResetToDefault;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return Inner.CreateControl(context);
        }

        internal override void CollectChromeBindingSnapshots(
            Dictionary<string, ModSettingsChromeBindingSnapshot> target)
        {
            Inner.CollectChromeBindingSnapshots(target);
        }

        internal override bool TryPasteChromeBindingSnapshot(ModSettingsChromeBindingSnapshot snap,
            IModSettingsUiActionHost host)
        {
            return Inner.TryPasteChromeBindingSnapshot(snap, host);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return Inner.TryResetToDefault(host);
        }
    }
}
