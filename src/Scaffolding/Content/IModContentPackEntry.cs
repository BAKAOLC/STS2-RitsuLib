namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a declarative content-pack step for timelines, unlocks, or other
    ///         <see cref="ModContentPackContext" /> features. Unlike <see cref="IContentRegistrationEntry" />, it
    ///         receives the complete pack context.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示用于时间线、解锁或其他 <see cref="ModContentPackContext" /> 功能的声明式内容包步骤。与
    ///         <see cref="IContentRegistrationEntry" /> 不同，它会接收完整的内容包上下文。
    ///     </para>
    /// </summary>
    public interface IModContentPackEntry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies this step during <see cref="ModContentPackBuilder.Apply" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="ModContentPackBuilder.Apply" /> 期间应用此步骤。
        ///     </para>
        /// </summary>
        void Apply(ModContentPackContext context);
    }
}
