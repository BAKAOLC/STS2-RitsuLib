namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides native-routed and handle-based entry points for FMOD playback.</para>
    ///     <para xml:lang="zh-CN">提供使用原生路由和基于句柄的 FMOD 播放入口。</para>
    /// </summary>
    public static class GameFmod
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />-routed API.</para>
        ///     <para xml:lang="zh-CN">获取经由 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> 路由的 API。</para>
        /// </summary>
        public static IGameFmodAudio Studio => GameFmodAudioService.Shared;

        /// <summary>
        ///     <para xml:lang="en">Gets the high-level playback API with typed handles, routing, and lifecycle scopes.</para>
        ///     <para xml:lang="zh-CN">获取带有类型化句柄、路由和生命周期作用域的高级播放 API。</para>
        /// </summary>
        public static IGameAudio Playback => GameAudioService.Shared;
    }
}
