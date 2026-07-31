namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Identifies a built-in lifecycle bucket used to group audio handles for cleanup.</para>
    ///     <para xml:lang="zh-CN">标识用于将音频句柄分组清理的内置生命周期作用域。</para>
    /// </summary>
    public enum AudioLifecycleScope
    {
        /// <summary>
        ///     <para xml:lang="en">No lifecycle event cleans up this scope automatically; callers manage it explicitly.</para>
        ///     <para xml:lang="zh-CN">没有生命周期事件会自动清理此作用域；由调用方显式管理。</para>
        /// </summary>
        Manual = 0,

        /// <summary>
        ///     <para xml:lang="en">Cleaned up when combat ends.</para>
        ///     <para xml:lang="zh-CN">在战斗结束时清理。</para>
        /// </summary>
        Combat = 1,

        /// <summary>
        ///     <para xml:lang="en">Cleaned up when the current room is exited.</para>
        ///     <para xml:lang="zh-CN">在离开当前房间时清理。</para>
        /// </summary>
        Room = 2,

        /// <summary>
        ///     <para xml:lang="en">Cleaned up when the run ends.</para>
        ///     <para xml:lang="zh-CN">在跑局结束时清理。</para>
        /// </summary>
        Run = 3,

        /// <summary>
        ///     <para xml:lang="en">Reserved for screen-scoped flows and currently requires explicit cleanup.</para>
        ///     <para xml:lang="zh-CN">保留给界面作用域流程，目前需要显式清理。</para>
        /// </summary>
        Screen = 4,
    }
}
