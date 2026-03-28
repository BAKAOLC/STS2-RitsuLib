namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Base type for interop types whose instance methods forward to a wrapped runtime object
    ///     (see <see cref="ModInteropAttribute" />).
    /// </summary>
    public abstract class InteropClassWrapper
    {
        /// <summary>
        ///     Runtime instance in the remote mod that receives forwarded calls.
        /// </summary>
        public object Value = null!;
    }
}
