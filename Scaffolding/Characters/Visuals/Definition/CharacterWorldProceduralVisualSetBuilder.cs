using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     Fluent builder for <see cref="CharacterWorldProceduralVisualSet" />.
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
        /// </summary>
        public static CharacterWorldProceduralVisualSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Uses a programmatic merchant-room character (no merchant <c>tscn</c>) with the given cue set.
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder Merchant(VisualCueSet cueSet)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _merchant = new(cueSet);
            return this;
        }

        /// <summary>
        ///     Uses <see cref="ModVisualCues.CueSet" /> output for the merchant room.
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
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder RestSite(VisualCueSet cueSet)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _restSite = new(cueSet);
            return this;
        }

        /// <summary>
        ///     Uses <see cref="ModVisualCues.CueSet" /> output for the rest site.
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
        /// </summary>
        public CharacterWorldProceduralVisualSet Build()
        {
            return new(_merchant, _restSite);
        }
    }
}
