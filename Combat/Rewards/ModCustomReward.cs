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
    ///     Base class for RitsuLib custom rewards with built-in description, icon, and save/load plumbing.
    ///     RitsuLib 自定义 reward 基类，封装描述、图标与存读档基础逻辑。
    /// </summary>
    /// <remarks>
    ///     Reward-set selection is synchronized by vanilla, but reward-specific side effects must either be
    ///     deterministic on every client or explicitly synchronized by the derived reward.
    ///     奖励集合中“选择了哪个奖励”由原版同步；但奖励自身造成的副作用必须在每个客户端确定性执行，
    ///     或由派生奖励显式同步。
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
        ///     Localization table used by <see cref="Description" />.
        ///     <see cref="Description" /> 使用的本地化表。
        /// </summary>
        protected virtual string DescriptionLocTable => "gameplay_ui";

        /// <summary>
        ///     Localization key used by <see cref="Description" />.
        ///     <see cref="Description" /> 使用的本地化 key。
        /// </summary>
        protected virtual string DescriptionLocKey => ModRewardRegistry.TryGetId(ModRewardType, out var id)
            ? id
            : ModRewardType.ToString();

        /// <summary>
        ///     Optional Godot resource path for the reward icon.
        ///     reward 图标的可选 Godot 资源路径。
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
        ///     Creates a serializable reward with a typed mod-owned payload.
        ///     使用类型化 payload 创建可存档 reward。
        /// </summary>
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
