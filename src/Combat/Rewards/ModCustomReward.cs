using System.Text.Json.Serialization.Metadata;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base implementation for RitsuLib custom rewards, including localization, optional icon
    ///         loading, and save-data creation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 RitsuLib 自定义奖励提供基础实现，包括本地化、可选图标加载以及存档数据创建。
    ///     </para>
    /// </summary>
    /// <param name="player">
    ///     <para xml:lang="en">The player who owns the reward.</para>
    ///     <para xml:lang="zh-CN">拥有该奖励的玩家。</para>
    /// </param>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The base game synchronizes which reward is selected from a reward set. Reward-specific side effects
    ///         must still be deterministic on every client or explicitly synchronized by the derived type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         原版会同步奖励集合中选中的奖励。奖励自身的副作用仍须在各客户端确定性执行，
    ///         或由派生类型显式同步。
    ///     </para>
    /// </remarks>
    public abstract class ModCustomReward(Player player) : Reward(player), IModSerializableReward
    {
        /// <inheritdoc />
        protected override RewardType RewardType => ModRewardType;

        /// <inheritdoc />
        public override int RewardsSetIndex => 9;

        /// <inheritdoc />
        public override bool IsPopulated => true;

        /// <inheritdoc />
        public override LocString Description => new(DescriptionLocTable, DescriptionLocKey);

        /// <summary>
        ///     <para xml:lang="en">Gets the localization table used by <see cref="Description" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Description" /> 使用的本地化表。</para>
        /// </summary>
        protected virtual string DescriptionLocTable => "gameplay_ui";

        /// <summary>
        ///     <para xml:lang="en">Gets the localization key used by <see cref="Description" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Description" /> 使用的本地化键。</para>
        /// </summary>
        protected virtual string DescriptionLocKey => ModRewardRegistry.TryGetId(ModRewardType, out var id)
            ? id
            : ModRewardType.ToString();

        /// <summary>
        ///     <para xml:lang="en">Gets the optional Godot resource path for the reward icon.</para>
        ///     <para xml:lang="zh-CN">获取奖励图标的可选 Godot 资源路径。</para>
        /// </summary>
        protected virtual string? RewardIconPath => null;

        /// <inheritdoc />
        public abstract RewardType ModRewardType { get; }

        /// <inheritdoc />
#if STS2_AT_LEAST_0_105_0
        public override void Populate()
        {
        }
#else
        public override Task Populate()
        {
            return Task.CompletedTask;
        }
#endif

        /// <inheritdoc />
        public override Control? CreateIcon()
        {
            if (TestMode.IsOn)
                return null;

            var iconPath = RewardIconPath;
            if (string.IsNullOrWhiteSpace(iconPath))
                return new();

            var texture = TryLoadIcon(iconPath);
            if (texture == null)
                return new();

            var icon = new TextureRect
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }

        /// <inheritdoc />
        public virtual string? ToModRewardJson()
        {
            return null;
        }

        /// <inheritdoc />
        public override SerializableReward ToSerializable()
        {
            return ModRewardSerialization.CreateSerializable(this);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates serializable reward data with a strongly typed, mod-owned payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用由模组维护的强类型载荷创建可序列化奖励数据。
        ///     </para>
        /// </summary>
        /// <typeparam name="TPayload">
        ///     <para xml:lang="en">The payload type.</para>
        ///     <para xml:lang="zh-CN">载荷类型。</para>
        /// </typeparam>
        /// <param name="payload">
        ///     <para xml:lang="en">The mod-owned payload to serialize.</para>
        ///     <para xml:lang="zh-CN">要序列化、由模组维护的载荷。</para>
        /// </param>
        /// <param name="jsonTypeInfo">
        ///     <para xml:lang="en">The source-generated JSON metadata for <typeparamref name="TPayload" />.</para>
        ///     <para xml:lang="zh-CN"><typeparamref name="TPayload" /> 的源生成 JSON 元数据。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The serializable reward data.</para>
        ///     <para xml:lang="zh-CN">可序列化的奖励数据。</para>
        /// </returns>
        protected SerializableReward ToSerializable<TPayload>(
            TPayload payload,
            JsonTypeInfo<TPayload> jsonTypeInfo)
        {
            return ModRewardSerialization.CreateSerializable(ModRewardType, payload, jsonTypeInfo);
        }

        private static Texture2D? TryLoadIcon(string iconPath)
        {
            try
            {
                return PreloadManager.Cache.GetCompressedTexture2D(iconPath);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Debug(
                    $"[RitsuLib] Custom reward icon was not preloaded, trying ResourceLoader: {iconPath} ({ex.Message})");
            }

            try
            {
                return ResourceLoader.Load<Texture2D>(iconPath);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RitsuLib] Failed to load custom reward icon '{iconPath}': {ex.Message}");
                return null;
            }
        }
    }
}
