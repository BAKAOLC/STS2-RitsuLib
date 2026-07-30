namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Classifies how a setting value relates to persistence and run state. The settings UI uses this metadata
    ///         for scope badges.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对设置值与持久化及对局状态之间的关系进行分类。设置界面使用此元数据显示作用域徽标。
    ///     </para>
    /// </summary>
    public enum ModSettingsValueSemantics
    {
        /// <summary>
        ///     <para xml:lang="en">A normal global or profile JSON data-store binding.</para>
        ///     <para xml:lang="zh-CN">普通的全局或档案 JSON 数据存储绑定。</para>
        /// </summary>
        Standard,

        /// <summary>
        ///     <para xml:lang="en">
        ///         A value owned by the current run that the mod is responsible for including in run save data.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         属于当前对局的值，模组负责将其写入对局存档数据。
        ///     </para>
        /// </summary>
        RunSnapshot,

        /// <summary>
        ///     <para xml:lang="en">
        ///         A value limited to the current combat or session and not intended for global or profile storage.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅限当前战斗或会话的值，不用于全局或档案存储。
        ///     </para>
        /// </summary>
        SessionCombat,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Optionally supplies semantic metadata for an <see cref="IModSettingsBinding" /> so the settings UI can
    ///         refine its scope badge.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选地为 <see cref="IModSettingsBinding" /> 提供语义元数据，使设置界面能够细化其作用域徽标。
    ///     </para>
    /// </summary>
    public interface IModSettingsBindingSemantics
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the semantic classification displayed by the settings UI.</para>
        ///     <para xml:lang="zh-CN">获取设置界面显示的语义分类。</para>
        /// </summary>
        ModSettingsValueSemantics Semantics { get; }
    }
}
