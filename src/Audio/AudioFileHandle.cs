using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A typed audio handle backed by an FMOD loose-file or streaming instance.</para>
    ///     <para xml:lang="zh-CN">由 FMOD 松散文件或流式实例支撑的类型化音频句柄。</para>
    /// </summary>
    public sealed class AudioFileHandle : AudioHandleBase
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a file handle around an existing FMOD sound instance.</para>
        ///     <para xml:lang="zh-CN">围绕现有 FMOD 声音实例初始化文件句柄。</para>
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
        public AudioFileHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
            : base(source, scope, rawInstance)
        {
        }
    }
}
