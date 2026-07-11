using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Preserves the complete behavior of an entry while one aspect is decorated by a section builder.
    ///     在 section builder 装饰条目的某一项行为时，保留该条目的完整行为。
    /// </summary>
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
