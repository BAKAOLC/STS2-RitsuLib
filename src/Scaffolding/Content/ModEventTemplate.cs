using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="EventModel" /> for mods with localization helpers, relic options, asset
    ///         overrides, and optional runtime factories for the layout, background, and VFX.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="EventModel" />，支持本地化便捷方法、遗物选项、资源替换，以及布局、
    ///         背景和特效的可选运行时工厂。
    ///     </para>
    /// </summary>
    public abstract class ModEventTemplate : EventModel, IModEventAssetOverrides, IModEventLayoutPackedSceneFactory,
        IModEventBackgroundPackedSceneFactory, IModEventVfxFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <see cref="TryCreateEventVfx" /> should be consulted before loading the VFX path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取是否应在加载特效路径前调用 <see cref="TryCreateEventVfx" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns a runtime-provided layout scene, or <see langword="null" /> to resolve
        ///         <see cref="CustomLayoutScenePath" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回运行时提供的布局场景；返回 <see langword="null" /> 时解析
        ///         <see cref="CustomLayoutScenePath" />。
        ///     </para>
        /// </summary>
        protected virtual PackedScene? TryCreateLayoutPackedScene()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a runtime-provided background scene, or <see langword="null" /> to resolve
        ///         <see cref="CustomBackgroundScenePath" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回运行时提供的背景场景；返回 <see langword="null" /> 时解析
        ///         <see cref="CustomBackgroundScenePath" />。
        ///     </para>
        /// </summary>
        protected virtual PackedScene? TryCreateBackgroundPackedScene()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a runtime-provided VFX root when <see cref="SuppliesCustomEventVfx" /> is enabled, or
        ///         <see langword="null" /> to continue with path loading.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         启用 <see cref="SuppliesCustomEventVfx" /> 时返回运行时提供的特效根节点；返回
        ///         <see langword="null" /> 时继续从路径加载。
        ///     </para>
        /// </summary>
        protected virtual Node2D? TryCreateEventVfx()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds an option-localization key from this event's ID, <paramref name="pageName" />, and
        ///         <paramref name="optionName" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用此事件的 ID、<paramref name="pageName" /> 和 <paramref name="optionName" />
        ///         创建选项本地化键。
        ///     </para>
        /// </summary>
        protected string ModOptionKey(string pageName, string optionName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
            return $"{Id.Entry}.pages.{pageName}.options.{optionName}";
        }

        /// <summary>
        ///     <para xml:lang="en">Builds an option-localization key for the <c>INITIAL</c> page.</para>
        ///     <para xml:lang="zh-CN">创建 <c>INITIAL</c> 页面的选项本地化键。</para>
        /// </summary>
        protected new string InitialOptionKey(string optionName)
        {
            return ModOptionKey("INITIAL", optionName);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the localized description for <paramref name="pageName" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="pageName" /> 对应的本地化描述。</para>
        /// </summary>
        protected LocString PageDescription(string pageName)
        {
            return L10NLookup($"{Id.Entry}.pages.{pageName}.description");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a relic option from a mutable copy of <typeparamref name="T" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <typeparamref name="T" /> 的可变副本创建遗物选项。
        ///     </para>
        /// </summary>
        protected EventOption CreateModRelicOption<T>(Func<Task>? onChosen, string pageName = "INITIAL")
            where T : RelicModel
        {
            return CreateModRelicOption(ModelDb.Relic<T>().ToMutable(), onChosen, pageName);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a relic option with a custom <paramref name="onChosen" /> callback and a localization key
        ///         derived from the page and relic ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带自定义 <paramref name="onChosen" /> 回调的遗物选项，其本地化键由页面和遗物 ID 生成。
        ///     </para>
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
