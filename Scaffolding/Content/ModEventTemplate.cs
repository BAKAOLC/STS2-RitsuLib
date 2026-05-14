using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="EventModel" /> for mods: localization helpers, relic options,
    ///     <see cref="IModEventAssetOverrides" />
    ///     paths, and optional runtime hooks <see cref="TryCreateLayoutPackedScene" />,
    ///     <see cref="TryCreateBackgroundPackedScene" />, <see cref="TryCreateEventVfx" />.
    ///     <see cref="TryCreateLayoutPackedScene" />、<see cref="TryCreateBackgroundPackedScene" />、
    ///     <see cref="TryCreateEventVfx" />。
    ///     Mod 事件的基础 <see cref="EventModel" />：本地化 helper、遗物选项、<see cref="IModEventAssetOverrides" /> 路径，以及可选运行时钩子
    ///     <see cref="TryCreateLayoutPackedScene" />、<see cref="TryCreateBackgroundPackedScene" />、
    ///     <see cref="TryCreateEventVfx" />。
    ///     <see cref="TryCreateLayoutPackedScene" />、<see cref="TryCreateBackgroundPackedScene" />、
    ///     <see cref="TryCreateEventVfx" />。
    /// </summary>
    public abstract class ModEventTemplate : EventModel, IModEventAssetOverrides, IModEventLayoutPackedSceneFactory,
        IModEventBackgroundPackedSceneFactory, IModEventVfxFactory
    {
        /// <summary>
        ///     <c>true</c> enables <see cref="TryCreateEventVfx" /> instead of the default VFX path.
        ///     <c>true</c> 时启用 <see cref="TryCreateEventVfx" />，而不是默认 VFX 路径。
        /// </summary>
        protected virtual bool SuppliesCustomEventVfx => false;

        /// <inheritdoc />
        public virtual EventAssetProfile AssetProfile => EventAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomLayoutScenePath => AssetProfile.LayoutScenePath;

        /// <inheritdoc />
        public virtual string? CustomInitialPortraitPath => AssetProfile.InitialPortraitPath;

        /// <inheritdoc />
        public virtual string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <inheritdoc />
        public virtual string? CustomVfxScenePath => AssetProfile.VfxScenePath;

        PackedScene? IModEventBackgroundPackedSceneFactory.TryCreateBackgroundPackedScene()
        {
            return TryCreateBackgroundPackedScene();
        }

        PackedScene? IModEventLayoutPackedSceneFactory.TryCreateLayoutPackedScene()
        {
            return TryCreateLayoutPackedScene();
        }

        bool IModEventVfxFactory.SuppliesCustomEventVfx => SuppliesCustomEventVfx;

        Node2D? IModEventVfxFactory.TryCreateEventVfx()
        {
            return TryCreateEventVfx();
        }

        /// <summary>
        ///     Non-null layout scene; otherwise <see cref="CustomLayoutScenePath" /> resolution runs.
        ///     返回非 null 布局场景时直接使用；否则解析 <see cref="CustomLayoutScenePath" />。
        /// </summary>
        protected virtual PackedScene? TryCreateLayoutPackedScene()
        {
            return null;
        }

        /// <summary>
        ///     Non-null background scene; otherwise <see cref="CustomBackgroundScenePath" /> resolution runs.
        ///     返回非 null 背景场景时直接使用；否则解析 <see cref="CustomBackgroundScenePath" />。
        /// </summary>
        protected virtual PackedScene? TryCreateBackgroundPackedScene()
        {
            return null;
        }

        /// <summary>
        ///     VFX root when <see cref="SuppliesCustomEventVfx" /> is <c>true</c>; <c>null</c> falls through to path loading.
        ///     当 <see cref="SuppliesCustomEventVfx" /> 为 <c>true</c> 时使用的 VFX 根节点；<c>null</c> 会继续走路径加载。
        /// </summary>
        protected virtual Node2D? TryCreateEventVfx()
        {
            return null;
        }

        /// <summary>
        ///     Builds a namespaced option key for <paramref name="pageName" /> / <paramref name="optionName" /> under this event
        ///     id.
        ///     在此事件 id 下为 <paramref name="pageName" /> / <paramref name="optionName" /> 构建带命名空间的选项键。
        /// </summary>
        protected string ModOptionKey(string pageName, string optionName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
            return $"{Id.Entry}.pages.{pageName}.options.{optionName}";
        }

        /// <summary>
        ///     Shortcut for <see cref="ModOptionKey" /> with the <c>INITIAL</c> page.
        ///     使用 <see cref="ModOptionKey" /> 页面调用 <c>INITIAL</c> 的快捷方法。
        /// </summary>
        protected new string InitialOptionKey(string optionName)
        {
            return ModOptionKey("INITIAL", optionName);
        }

        /// <summary>
        ///     Gets the localized description for a page.
        ///     获取某个页面的本地化描述。
        /// </summary>
        protected LocString PageDescription(string pageName)
        {
            return L10NLookup($"{Id.Entry}.pages.{pageName}.description");
        }

        /// <summary>
        ///     Creates a relic-grant option for a mutable relic resolved from <typeparamref name="T" />.
        ///     为从 <c>T</c> 解析出的可变遗物创建一个授予遗物的选项。
        /// </summary>
        protected EventOption CreateModRelicOption<T>(Func<Task>? onChosen, string pageName = "INITIAL")
            where T : RelicModel
        {
            return CreateModRelicOption(ModelDb.Relic<T>().ToMutable(), onChosen, pageName);
        }

        /// <summary>
        ///     Creates a relic-grant option with a custom <paramref name="onChosen" /> callback and localization key.
        ///     创建一个带自定义 <paramref name="onChosen" /> 回调和本地化键的授予遗物选项。
        /// </summary>
        protected EventOption CreateModRelicOption(RelicModel relic, Func<Task>? onChosen, string pageName = "INITIAL")
        {
            relic.AssertMutable();
            relic.Owner = Owner ?? throw new InvalidOperationException(
                $"Event '{Id.Entry}' tried to create a relic option before the event owner was assigned.");
            return EventOption.FromRelic(relic, this, onChosen, ModOptionKey(pageName, relic.Id.Entry));
        }
    }
}
