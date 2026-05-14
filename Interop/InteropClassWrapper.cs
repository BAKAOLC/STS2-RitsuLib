namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Base type for interop types whose instance methods forward to a wrapped runtime object
    ///     (see <see cref="ModInteropAttribute" />).
    ///     interop 类型的基类；这些类型的实例方法会转发到包装的运行时对象
    ///     （参见 <see cref="ModInteropAttribute" />）。
    /// </summary>
    public abstract class InteropClassWrapper
    {
        /// <summary>
        ///     Runtime instance in the remote mod that receives forwarded calls.
        ///     接收转发调用的远端 mod 运行时实例。
        /// </summary>
        public object Value = null!;
    }
}
