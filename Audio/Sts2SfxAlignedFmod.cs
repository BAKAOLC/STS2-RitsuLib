using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     SFX aligned with <see cref="SfxCmd" /> guards. Use <see cref="GameFmod.Studio" /> for music, mixer, or unguarded
    ///     playback.
    /// </summary>
    public static class Sts2SfxAlignedFmod
    {
        /// <summary>
        ///     One-shot through <see cref="SfxCmd.Play(string, float)" />.
        /// </summary>
        public static void PlayOneShot(string eventPath, float volume = 1f)
        {
            SfxCmd.Play(eventPath, volume);
        }

        /// <summary>
        ///     One-shot with a single named parameter via <see cref="SfxCmd" />.
        /// </summary>
        public static void PlayOneShot(string eventPath, string parameterName, float parameterValue, float volume = 1f)
        {
            SfxCmd.Play(eventPath, parameterName, parameterValue, volume);
        }

        /// <summary>
        ///     One-shot with parameters; skips when non-interactive or combat is ending, otherwise uses
        ///     <see cref="GameFmod.Studio" />.
        /// </summary>
        public static void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float> parameters,
            float volume = 1f)
        {
            if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
                return;

            GameFmod.Studio.PlayOneShot(eventPath, parameters, volume);
        }

        /// <summary>
        ///     Starts a guarded loop via <see cref="SfxCmd.PlayLoop" />.
        /// </summary>
        public static void PlayLoop(string eventPath, bool usesLoopParam = true)
        {
            SfxCmd.PlayLoop(eventPath, usesLoopParam);
        }

        /// <summary>
        ///     Stops a loop via <see cref="SfxCmd.StopLoop" />.
        /// </summary>
        public static void StopLoop(string eventPath)
        {
            SfxCmd.StopLoop(eventPath);
        }

        /// <summary>
        ///     Sets a loop parameter via <see cref="SfxCmd.SetParam" />.
        /// </summary>
        public static void SetLoopParameter(string eventPath, string parameterName, float value)
        {
            SfxCmd.SetParam(eventPath, parameterName, value);
        }
    }
}
