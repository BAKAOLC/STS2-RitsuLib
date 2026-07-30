using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Localization;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="AncientEventModel" /> with namespaced option keys, relic options that can
    ///         complete the event, presentation-asset overrides, and dialogue loaded from the <c>ancients</c>
    ///         localization table through <see cref="AncientDialogueLocalization.BuildDialogueSetForModAncient" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供基础 <see cref="AncientEventModel" />，支持带命名空间的选项键、可完成事件的遗物选项、表现资源
    ///         替换，以及通过 <see cref="AncientDialogueLocalization.BuildDialogueSetForModAncient" /> 从
    ///         <c>ancients</c> 本地化表加载的对话。
    ///     </para>
    /// </summary>
    public abstract class ModAncientEventTemplate : AncientEventModel, IModAncientEventAssetOverrides,
        IModAncientActValidity
    {
        /// <inheritdoc />
        public virtual bool IsValidForAct(ActModel act)
        {
            return true;
        }

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

        /// <inheritdoc />
        public virtual AncientEventPresentationAssetProfile AncientPresentationAssetProfile =>
            AncientEventPresentationAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomMapIconPath => AncientPresentationAssetProfile?.MapIconPath;

        /// <inheritdoc />
        public virtual string? CustomMapIconOutlinePath => AncientPresentationAssetProfile?.MapIconOutlinePath;

        /// <inheritdoc />
        public virtual string? CustomRunHistoryIconPath => AncientPresentationAssetProfile?.RunHistoryIconPath;

        /// <inheritdoc />
        public virtual string? CustomRunHistoryIconOutlinePath =>
            AncientPresentationAssetProfile?.RunHistoryIconOutlinePath;

        /// <inheritdoc />
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The default implementation scans loaded localization data, including <c>ancients</c> JSON, for
        ///         this Ancient's <c>talk</c> keys. Override it for a non-localized or custom dialogue structure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         默认实现会扫描已加载的本地化数据（包括 <c>ancients</c> JSON），查找此先古之民的
        ///         <c>talk</c> 键。如需使用非本地化或自定义对话结构，请重写此方法。
        ///     </para>
        /// </remarks>
        protected override AncientDialogueSet DefineDialogues()
        {
            return AncientDialogueLocalization.BuildDialogueSetForModAncient(Id.Entry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds an option-localization key from this Ancient's ID, <paramref name="pageName" />, and
        ///         <paramref name="optionName" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用此先古之民的 ID、<paramref name="pageName" /> 和 <paramref name="optionName" />
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
        ///     <para xml:lang="en">
        ///         Builds an option-localization key for the <c>INITIAL</c> page.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <c>INITIAL</c> 页面的选项本地化键。
        ///     </para>
        /// </summary>
        protected new string InitialOptionKey(string optionName)
        {
            return ModOptionKey("INITIAL", optionName);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a relic option that grants a mutable copy of the specified relic type to the event owner
        ///         and then calls <see cref="AncientEventModel.Done" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建一个遗物选项，将指定遗物类型的可变副本授予事件拥有者，然后调用
        ///         <see cref="AncientEventModel.Done" />。
        ///     </para>
        /// </summary>
        protected EventOption CreateModRelicOption<T>(string pageName = "INITIAL") where T : RelicModel
        {
            return CreateModRelicOption(ModelDb.Relic<T>().ToMutable(), pageName);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a relic option that grants <paramref name="relic" /> to the event owner and completes the
        ///         Ancient event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建一个遗物选项，将 <paramref name="relic" /> 授予事件拥有者并完成先古之民事件。
        ///     </para>
        /// </summary>
        protected EventOption CreateModRelicOption(RelicModel relic, string pageName = "INITIAL")
        {
            return CreateModRelicOption(
                relic,
                async () =>
                {
                    var owner = Owner ?? throw new InvalidOperationException(
                        $"Ancient '{Id.Entry}' had no owner when a relic option was chosen.");
                    relic.Owner = owner;
                    await RelicCmd.Obtain(relic, owner);
                    Done();
                },
                pageName);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a relic option with an explicit post-selection callback and a localization key derived
        ///         from <paramref name="pageName" /> and the relic ID. If <see cref="EventModel.Owner" /> is not yet
        ///         assigned, the relic owner remains unset.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带显式选择后回调的遗物选项，其本地化键由 <paramref name="pageName" /> 和遗物 ID 生成。如果
        ///         <see cref="EventModel.Owner" /> 尚未设置，则遗物拥有者保持未设置。
        ///     </para>
        /// </summary>
        protected EventOption CreateModRelicOption(
            RelicModel relic,
            Func<Task>? onChosen,
            string pageName = "INITIAL")
        {
            relic.AssertMutable();
            if (Owner != null)
                relic.Owner = Owner;

            return EventOption.FromRelic(relic, this, onChosen, ModOptionKey(pageName, relic.Id.Entry));
        }
    }
}
