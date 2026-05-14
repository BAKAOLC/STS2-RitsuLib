using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     Fluent builder for <see cref="CharacterWorldProceduralVisualSet" />.
    ///     <see cref="CharacterWorldProceduralVisualSet" /> 的流式构建器。
    /// </summary>
    public sealed class CharacterWorldProceduralVisualSetBuilder
    {
        private CharacterMerchantWorldDefinition? _merchant;
        private CharacterRestSiteWorldDefinition? _restSite;

        private CharacterWorldProceduralVisualSetBuilder()
        {
        }

        /// <summary>
        ///     Starts a world procedural visual set.
        ///     开始一个世界场景程序化视觉集合。
        /// </summary>
        public static CharacterWorldProceduralVisualSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Uses a programmatic merchant-room character (no merchant <c>tscn</c>) with the given cue set.
        ///     使用给定的 cue set 创建程序化商人房间角色（不需要商人 <c>tscn</c>）。
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder Merchant(VisualCueSet cueSet)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _merchant = new(cueSet);
            return this;
        }

        /// <summary>
        ///     Uses <see cref="ModVisualCues.CueSet" /> output for the merchant room.
        ///     为商人房间使用 <see cref="ModVisualCues.CueSet" /> 的输出。
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder Merchant(Action<VisualCueSetBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var inner = VisualCueSetBuilder.Create();
            configure(inner);
            return Merchant(inner.Build());
        }

        /// <summary>
        ///     Uses a programmatic rest-site character shell (no rest-site character <c>tscn</c>) with the given cue
        ///     set.
        ///     使用给定的 cue set 创建程序化休息点角色外壳（不需要休息点角色 <c>tscn</c>）。
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder RestSite(VisualCueSet cueSet)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _restSite = new(cueSet);
            return this;
        }

        /// <summary>
        ///     Uses <see cref="ModVisualCues.CueSet" /> output for the rest site.
        ///     为休息点使用 <see cref="ModVisualCues.CueSet" /> 的输出。
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder RestSite(Action<VisualCueSetBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var inner = VisualCueSetBuilder.Create();
            configure(inner);
            return RestSite(inner.Build());
        }

        /// <summary>
        ///     Materializes the set (components may be null).
        ///     实体化该集合（组件可以为 null）。
        /// </summary>
        public CharacterWorldProceduralVisualSet Build()
        {
            return new(_merchant, _restSite);
        }
    }
}
