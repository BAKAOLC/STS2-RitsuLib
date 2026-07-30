using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a fluent builder for <see cref="CharacterWorldProceduralVisualSet" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 <see cref="CharacterWorldProceduralVisualSet" /> 的流式构建器。
    ///     </para>
    /// </summary>
    public sealed class CharacterWorldProceduralVisualSetBuilder
    {
        private CharacterMerchantWorldDefinition? _merchant;
        private CharacterRestSiteWorldDefinition? _restSite;

        private CharacterWorldProceduralVisualSetBuilder()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an empty world-scene visual builder.</para>
        ///     <para xml:lang="zh-CN">创建空的世界场景形象构建器。</para>
        /// </summary>
        public static CharacterWorldProceduralVisualSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures a procedural merchant-room character with the supplied cue set and no merchant
        ///         <c>tscn</c> scene.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用指定的形象提示集合配置程序化商人房间角色，无需商人 <c>tscn</c> 场景。
        ///     </para>
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder Merchant(VisualCueSet cueSet)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _merchant = new(cueSet);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures merchant-room cues through a <see cref="VisualCueSetBuilder" /> callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="VisualCueSetBuilder" /> 回调配置商人房间的形象提示。
        ///     </para>
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder Merchant(Action<VisualCueSetBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var inner = VisualCueSetBuilder.Create();
            configure(inner);
            return Merchant(inner.Build());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures a procedural rest-site character with the supplied cue set and no rest-site character
        ///         <c>tscn</c> scene.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用指定的形象提示集合配置程序化休息处角色，无需休息处角色 <c>tscn</c> 场景。
        ///     </para>
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder RestSite(VisualCueSet cueSet)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _restSite = new(cueSet);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures rest-site cues through a <see cref="VisualCueSetBuilder" /> callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="VisualCueSetBuilder" /> 回调配置休息处的形象提示。
        ///     </para>
        /// </summary>
        public CharacterWorldProceduralVisualSetBuilder RestSite(Action<VisualCueSetBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var inner = VisualCueSetBuilder.Create();
            configure(inner);
            return RestSite(inner.Build());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the configured visual set. Unconfigured components remain <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建已配置的形象集合；未配置的组成部分保持为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public CharacterWorldProceduralVisualSet Build()
        {
            return new(_merchant, _restSite);
        }
    }
}
