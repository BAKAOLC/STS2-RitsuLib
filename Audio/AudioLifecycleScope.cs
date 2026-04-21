namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Built-in lifecycle buckets for automatic audio cleanup.
    /// </summary>
    public enum AudioLifecycleScope
    {
        /// <summary>
        ///     Caller-managed only.
        /// </summary>
        Manual = 0,

        /// <summary>
        ///     Stops when combat ends.
        /// </summary>
        Combat = 1,

        /// <summary>
        ///     Stops when the current room is exited.
        /// </summary>
        Room = 2,

        /// <summary>
        ///     Stops when the run ends.
        /// </summary>
        Run = 3,

        /// <summary>
        ///     Reserved for screen-scoped flows.
        /// </summary>
        Screen = 4,
    }
}
