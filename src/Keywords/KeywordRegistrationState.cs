namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Indicates whether <see cref="ModKeywordRegistry" /> still accepts keyword registrations from mods.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示 <see cref="ModKeywordRegistry" /> 当前是否仍接受模组注册关键词。
    ///     </para>
    /// </summary>
    public enum KeywordRegistrationState
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registrations are allowed until the framework freezes this registry alongside the other model
        ///         registries.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在框架随其他模型注册表一同冻结此注册表之前，均可注册关键词。
        ///     </para>
        /// </summary>
        Open = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The global keyword registry is sealed, and further registration attempts throw.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         全局关键词注册表已封闭，继续尝试注册会抛出异常。
        ///     </para>
        /// </summary>
        Frozen = 1,
    }
}
