namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Combines the mod-facing playback and mixer operations routed through
    ///         <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">组合经由 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> 路由、面向模组的播放与混音操作。</para>
    /// </summary>
    public interface IGameFmodAudio : IFmodOneShotPlayback, IFmodLoopPlayback, IFmodMusicPlayback, IFmodMixerVolumes;
}
