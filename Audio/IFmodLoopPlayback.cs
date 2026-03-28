namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Looping events keyed by path (vanilla loop dictionary semantics).
    /// </summary>
    public interface IFmodLoopPlayback
    {
        /// <summary>
        ///     Starts or continues a loop; <paramref name="usesLoopParam" /> matches vanilla loop-parameter convention.
        /// </summary>
        void PlayLoop(string eventPath, bool usesLoopParam = true);

        /// <summary>
        ///     Stops a previously started loop for <paramref name="eventPath" />.
        /// </summary>
        void StopLoop(string eventPath);

        /// <summary>
        ///     Sets a parameter on the active loop instance for <paramref name="eventPath" />.
        /// </summary>
        void SetLoopParameter(string eventPath, string parameterName, float value);

        /// <summary>
        ///     Stops every managed loop.
        /// </summary>
        void StopAllLoops();
    }
}
