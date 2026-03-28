namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Full surface of <see cref="MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager" /> for mods.
    /// </summary>
    public interface IGameFmodAudio : IFmodOneShotPlayback, IFmodLoopPlayback, IFmodMusicPlayback, IFmodMixerVolumes;
}
