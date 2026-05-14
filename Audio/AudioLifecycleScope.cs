namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Built-in lifecycle buckets for automatic audio cleanup.
    ///     用于自动音频清理的内置生命周期分组。
    /// </summary>
    public enum AudioLifecycleScope
    {
        /// <summary>
        ///     Caller-managed only.
        ///     仅由调用方管理。
        /// </summary>
        Manual = 0,

        /// <summary>
        ///     Stops when combat ends.
        ///     战斗结束时停止。
        /// </summary>
        Combat = 1,

        /// <summary>
        ///     Stops when the current room is exited.
        ///     离开当前房间时停止。
        /// </summary>
        Room = 2,

        /// <summary>
        ///     Stops when the run ends.
        ///     跑局结束时停止。
        /// </summary>
        Run = 3,

        /// <summary>
        ///     Reserved for screen-scoped flows.
        ///     保留给屏幕作用域流程。
        /// </summary>
        Screen = 4,
    }
}
