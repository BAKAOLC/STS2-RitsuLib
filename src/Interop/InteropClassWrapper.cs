namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Base type for interop stubs whose instance members forward to a wrapped runtime object.
    ///         See <see cref="ModInteropAttribute" /> and <see cref="AssemblyInteropAttribute" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         实例成员会转发到所包装运行时对象的互操作存根基类。参见
    ///         <see cref="ModInteropAttribute" /> 和 <see cref="AssemblyInteropAttribute" />。
    ///     </para>
    /// </summary>
    public abstract class InteropClassWrapper
    {
        /// <summary>
        ///     <para xml:lang="en">Runtime instance that receives forwarded member calls.</para>
        ///     <para xml:lang="zh-CN">接收成员转发调用的运行时实例。</para>
        /// </summary>
        public object Value = null!;
    }
}
