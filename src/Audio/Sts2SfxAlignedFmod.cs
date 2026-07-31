using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides sound-effect helpers with the same non-interactive and combat-ending guards as
    ///         <see cref="SfxCmd" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供与 <see cref="SfxCmd" /> 相同、受非交互状态和战斗结束状态保护的音效辅助方法。</para>
    /// </summary>
    public static class Sts2SfxAlignedFmod
    {
        /// <summary>
        ///     <para xml:lang="en">Plays a guarded one-shot through <see cref="SfxCmd.Play(string, float)" />.</para>
        ///     <para xml:lang="zh-CN">通过 <see cref="SfxCmd.Play(string, float)" /> 播放受保护的单次音效。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear instance volume.</para>
        ///     <para xml:lang="zh-CN">未钳制的实例线性音量。</para>
        /// </param>
        public static void PlayOneShot(string eventPath, float volume = 1f)
        {
            SfxCmd.Play(eventPath, volume);
        }

        /// <summary>
        ///     <para xml:lang="en">Plays a guarded one-shot with one initial numeric parameter through <see cref="SfxCmd" />.</para>
        ///     <para xml:lang="zh-CN">通过 <see cref="SfxCmd" /> 播放带一个初始数值参数的受保护单次音效。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="parameterName">
        ///     <para xml:lang="en">The initial FMOD parameter name.</para>
        ///     <para xml:lang="zh-CN">初始 FMOD 参数名称。</para>
        /// </param>
        /// <param name="parameterValue">
        ///     <para xml:lang="en">The initial parameter value.</para>
        ///     <para xml:lang="zh-CN">初始参数值。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear instance volume.</para>
        ///     <para xml:lang="zh-CN">未钳制的实例线性音量。</para>
        /// </param>
        public static void PlayOneShot(string eventPath, string parameterName, float parameterValue, float volume = 1f)
        {
            SfxCmd.Play(eventPath, parameterName, parameterValue, volume);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Plays a parameterized one-shot through <see cref="GameFmod.Studio" /> after applying the same
        ///         guards as <see cref="SfxCmd.Play(string, float)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用与 <see cref="SfxCmd.Play(string, float)" /> 相同的保护条件后，通过 <see cref="GameFmod.Studio" />
        ///         播放带参数的单次音效。
        ///     </para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">The initial numeric parameter values.</para>
        ///     <para xml:lang="zh-CN">初始数值参数。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear instance volume.</para>
        ///     <para xml:lang="zh-CN">未钳制的实例线性音量。</para>
        /// </param>
        public static void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float> parameters,
            float volume = 1f)
        {
            if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
                return;

            GameFmod.Studio.PlayOneShot(eventPath, parameters, volume);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starts a loop through <see cref="SfxCmd.PlayLoop(string, bool)" />, which skips playback in
        ///         non-interactive mode.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过 <see cref="SfxCmd.PlayLoop(string, bool)" /> 启动循环；非交互模式下会跳过播放。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="usesLoopParam">
        ///     <para xml:lang="en">Whether stopping uses the game's <c>loop = 1</c> convention.</para>
        ///     <para xml:lang="zh-CN">停止时是否使用游戏的 <c>loop = 1</c> 约定。</para>
        /// </param>
        public static void PlayLoop(string eventPath, bool usesLoopParam = true)
        {
            SfxCmd.PlayLoop(eventPath, usesLoopParam);
        }

        /// <summary>
        ///     <para xml:lang="en">Stops the oldest loop under an event path through <see cref="SfxCmd.StopLoop(string)" />.</para>
        ///     <para xml:lang="zh-CN">通过 <see cref="SfxCmd.StopLoop(string)" /> 停止事件路径下最早的循环。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path used to group the loop.</para>
        ///     <para xml:lang="zh-CN">用于对循环分组的 FMOD Studio 事件路径。</para>
        /// </param>
        public static void StopLoop(string eventPath)
        {
            SfxCmd.StopLoop(eventPath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a numeric parameter on the oldest loop under an event path through
        ///         <see cref="SfxCmd.SetParam" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过 <see cref="SfxCmd.SetParam" /> 为事件路径下最早的循环设置数值参数。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio event path used to find the loop.</para>
        ///     <para xml:lang="zh-CN">用于查找循环的 FMOD Studio 事件路径。</para>
        /// </param>
        /// <param name="parameterName">
        ///     <para xml:lang="en">The FMOD parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD 参数名称。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The numeric parameter value.</para>
        ///     <para xml:lang="zh-CN">数值参数值。</para>
        /// </param>
        public static void SetLoopParameter(string eventPath, string parameterName, float value)
        {
            SfxCmd.SetParam(eventPath, parameterName, value);
        }
    }
}
