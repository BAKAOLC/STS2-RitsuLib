using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A typed audio handle for an active FMOD Studio snapshot instance.</para>
    ///     <para xml:lang="zh-CN">用于活动 FMOD Studio 快照实例的类型化音频句柄。</para>
    /// </summary>
    /// <param name="source">
    ///     <para xml:lang="en">The logical snapshot source represented by the instance.</para>
    ///     <para xml:lang="zh-CN">该实例所代表的逻辑快照源。</para>
    /// </param>
    /// <param name="scope">
    ///     <para xml:lang="en">The lifecycle scope associated with the handle.</para>
    ///     <para xml:lang="zh-CN">与句柄关联的生命周期作用域。</para>
    /// </param>
    /// <param name="rawInstance">
    ///     <para xml:lang="en">The underlying FMOD Godot object, or null to create an invalid handle.</para>
    ///     <para xml:lang="zh-CN">底层 FMOD Godot 对象；为 <see langword="null" /> 时创建无效句柄。</para>
    /// </param>
    public sealed class AudioSnapshotHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance);
}
