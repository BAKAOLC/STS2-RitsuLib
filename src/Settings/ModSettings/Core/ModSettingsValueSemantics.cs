namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Declares how a setting value relates to persistence and run lifecycle (for UI chips and mod author intent).
    ///     声明设置值与持久化和 run 生命周期的关系（用于 UI chip 和表达 Mod 作者意图）。
    /// </summary>
    public enum ModSettingsValueSemantics
    {
        /// <summary>
        ///     Normal global/profile JSON store binding.
        ///     普通全局/档案 JSON 存储绑定。
        /// </summary>
        Standard,

        /// <summary>
        ///     Value is logically owned by the current run (must be captured into run save data by the mod).
        ///     值在逻辑上属于当前 run（必须由 Mod 捕获进 run 存档数据）。
        /// </summary>
        RunSnapshot,

        /// <summary>
        ///     Value applies only to the current combat/session and is not expected in global/profile stores.
        ///     值仅适用于当前战斗/会话，不应出现在全局/档案存储中。
        /// </summary>
        SessionCombat,
    }

    /// <summary>
    ///     Optional marker on <see cref="IModSettingsBinding" /> implementations to refine scope chip text.
    ///     <see cref="IModSettingsBinding" /> 实现上的可选标记，用于细化作用域 chip 文本。
    /// </summary>
    public interface IModSettingsBindingSemantics
    {
        /// <summary>
        ///     Semantic classification for documentation-style UI chips.
        ///     文档风格 UI chip 使用的语义分类。
        /// </summary>
        ModSettingsValueSemantics Semantics { get; }
    }
}
