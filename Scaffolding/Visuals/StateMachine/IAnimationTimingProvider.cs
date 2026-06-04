namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Optional timing surface for animation backends that can report clip duration and remaining playback time.
    ///     Existing <see cref="IAnimationBackend" /> implementers do not need to implement this interface.
    /// </summary>
    public interface IAnimationTimingProvider
    {
        /// <summary>
        ///     Returns the total playback duration of <paramref name="id" /> in real seconds when known.
        /// </summary>
        bool TryGetAnimationDuration(string id, out float seconds);

        /// <summary>
        ///     Returns the remaining real seconds for the currently active animation when known.
        /// </summary>
        bool TryGetCurrentAnimationRemaining(out float seconds);
    }
}
