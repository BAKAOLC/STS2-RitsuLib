using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A typed audio handle backed by an FMOD Studio event instance.</para>
    ///     <para xml:lang="zh-CN">由 FMOD Studio 事件实例支撑的类型化音频句柄。</para>
    /// </summary>
    public sealed class AudioEventHandle : AudioHandleBase
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes an event handle around an existing FMOD Studio instance.</para>
        ///     <para xml:lang="zh-CN">围绕现有 FMOD Studio 实例初始化事件句柄。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">The logical audio source represented by the instance.</para>
        ///     <para xml:lang="zh-CN">该实例所代表的逻辑音频源。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">The lifecycle scope associated with the handle.</para>
        ///     <para xml:lang="zh-CN">与句柄关联的生命周期作用域。</para>
        /// </param>
        /// <param name="rawInstance">
        ///     <para xml:lang="en">The underlying FMOD Godot object, or null to create an invalid handle.</para>
        ///     <para xml:lang="zh-CN">底层 FMOD Godot 对象；为 <see langword="null" /> 时创建无效句柄。</para>
        /// </param>
        public AudioEventHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
            : base(source, scope, rawInstance)
        {
        }
    }
}
