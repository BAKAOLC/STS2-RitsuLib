using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides an <see cref="EpochModel" /> base that unlocks <typeparamref name="TCharacter" /> and optionally
    ///         expands the timeline.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供解锁 <typeparamref name="TCharacter" /> 的 <see cref="EpochModel" /> 基类，也可选择扩展时间线。
    ///     </para>
    /// </summary>
    /// <typeparam name="TCharacter">
    ///     <para xml:lang="en">The character model type to unlock.</para>
    ///     <para xml:lang="zh-CN">要解锁的角色模型类型。</para>
    /// </typeparam>
    public abstract class CharacterUnlockEpochTemplate<TCharacter> : ModEpochTemplate
        where TCharacter : CharacterModel
    {
#if !STS2_AT_LEAST_0_106_0
        /// <inheritdoc />
        public override bool IsArtPlaceholder => false;
#endif

        /// <summary>
        ///     <para xml:lang="en">Gets additional epoch types appended when this epoch unlocks.</para>
        ///     <para xml:lang="zh-CN">获取此纪元解锁时追加的其他纪元类型。</para>
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <inheritdoc />
        public override EpochModel[] GetTimelineExpansion()
        {
            return [.. ExpansionEpochTypes.Select(type => Get(GetId(type)))];
        }

        /// <inheritdoc />
        public override void QueueUnlocks()
        {
            NTimelineScreen.Instance.QueueCharacterUnlock<TCharacter>(this);
            SaveManager.Instance.Progress.PendingCharacterUnlock = ModelDb.GetId<TCharacter>();

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
