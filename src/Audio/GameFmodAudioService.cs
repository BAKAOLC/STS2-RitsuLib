using MegaCrit.Sts2.Core.Nodes.Audio;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Forwards the native-style audio surface to <see cref="NAudioManager" />, preserving the game's
    ///         routing and runtime-mode checks.
    ///     </para>
    ///     <para xml:lang="zh-CN">将原生风格的音频接口转发到 <see cref="NAudioManager" />，保留游戏的路由与运行模式检查。</para>
    /// </summary>
    public sealed class GameFmodAudioService : IGameFmodAudio
    {
        private GameFmodAudioService()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared service provided by <see cref="GameFmod.Studio" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="GameFmod.Studio" /> 提供的共享服务。</para>
        /// </summary>
        public static GameFmodAudioService Shared { get; } = new();

        private static NAudioManager? Manager => NAudioManager.Instance;
        internal static bool IsAvailable => Manager is not null;

        /// <inheritdoc />
        public void PlayOneShot(string eventPath, float volume = 1f)
        {
            TryPlayOneShot(eventPath, volume);
        }

        /// <inheritdoc />
        public void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float> parameters, float volume = 1f)
        {
            TryPlayOneShot(eventPath, parameters, volume);
        }

        /// <inheritdoc />
        public void PlayLoop(string eventPath, bool usesLoopParam = true)
        {
            Manager?.PlayLoop(eventPath, usesLoopParam);
        }

        /// <inheritdoc />
        public void StopLoop(string eventPath)
        {
            Manager?.StopLoop(eventPath);
        }

        /// <inheritdoc />
        public void SetLoopParameter(string eventPath, string parameterName, float value)
        {
            Manager?.SetParam(eventPath, parameterName, value);
        }

        /// <inheritdoc />
        public void StopAllLoops()
        {
            Manager?.StopAllLoops();
        }

        /// <inheritdoc />
        public void PlayMusic(string eventPath)
        {
            Manager?.PlayMusic(eventPath);
        }

        /// <inheritdoc />
        public void StopMusic()
        {
            Manager?.StopMusic();
        }

        /// <inheritdoc />
        public void UpdateMusicParameter(string parameterName, string labelValue)
        {
            Manager?.UpdateMusicParameter(parameterName, labelValue);
        }

        /// <inheritdoc />
        public void SetMasterVolume(float linear01)
        {
            Manager?.SetMasterVol(linear01);
        }

        /// <inheritdoc />
        public void SetSfxVolume(float linear01)
        {
            Manager?.SetSfxVol(linear01);
        }

        /// <inheritdoc />
        public void SetAmbienceVolume(float linear01)
        {
            Manager?.SetAmbienceVol(linear01);
        }

        /// <inheritdoc />
        public void SetBgmVolume(float linear01)
        {
            Manager?.SetBgmVol(linear01);
        }

        internal bool TryPlayOneShot(string eventPath, float volume)
        {
            var manager = Manager;
            if (manager is null)
                return false;

            manager.PlayOneShot(eventPath, volume);
            return true;
        }

        internal bool TryPlayOneShot(
            string eventPath,
            IReadOnlyDictionary<string, float> parameters,
            float volume)
        {
            var manager = Manager;
            if (manager is null)
                return false;

            if (parameters.Count == 0)
            {
                manager.PlayOneShot(eventPath, volume);
                return true;
            }

            manager.PlayOneShot(eventPath, ToManagedDictionary(parameters), volume);
            return true;
        }

        private static Dictionary<string, float> ToManagedDictionary(IReadOnlyDictionary<string, float> parameters)
        {
            var d = new Dictionary<string, float>(parameters.Count);
            foreach (var kv in parameters)
                d[kv.Key] = kv.Value;

            return d;
        }
    }
}
