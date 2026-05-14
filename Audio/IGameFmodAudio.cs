namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Full surface of <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> for mods.
    ///     供 mod 使用的完整 <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> 接口面。
    /// </summary>
    public interface IGameFmodAudio : IFmodOneShotPlayback, IFmodLoopPlayback, IFmodMusicPlayback, IFmodMixerVolumes;
}
