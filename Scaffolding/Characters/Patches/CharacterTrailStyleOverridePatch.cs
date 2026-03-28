using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     After <see cref="NCardTrailVfx.Create" />, applies <see cref="IModCharacterAssetOverrides.CustomTrailStyle" />
    ///     modulates and widths to line, particle, and sprite nodes when present.
    /// </summary>
    public class CharacterTrailStyleOverridePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "character_trail_style_override";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Allow mod characters to reuse a vanilla trail scene and override its visual properties";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create), [typeof(Control), typeof(string)])];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Mutates the created trail instance in place when the owning card’s character supplies a trail style.
        /// </summary>
        public static void Postfix(Control card, ref NCardTrailVfx? __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result == null || card is not NCard nCard)
                return;

            var style = (nCard.Model?.Owner?.Character as IModCharacterAssetOverrides)?.CustomTrailStyle;
            if (style == null)
                return;

            ApplyLineStyle(__result, "Trails/OuterTrail", style.OuterTrailModulate, style.OuterTrailWidth);
            ApplyLineStyle(__result, "Trails/InnerTrail", style.InnerTrailModulate, style.InnerTrailWidth);
            ApplyParticleColor(__result, "Sprites/BigSparks", style.BigSparksColor);
            ApplyParticleColor(__result, "Sprites/LittleSparks", style.LittleSparksColor);
            ApplySpriteStyle(__result, "Sprites/Sprite2D2", style.PrimarySpriteModulate, style.PrimarySpriteScale);
            ApplySpriteStyle(__result, "Sprites/Sprite2D3", style.SecondarySpriteModulate, style.SecondarySpriteScale);
        }

        private static void ApplyLineStyle(Node root, string nodePath, Color? modulate, float? width)
        {
            if (root.GetNodeOrNull<Line2D>(nodePath) is not { } line)
                return;

            if (modulate.HasValue)
                line.Modulate = modulate.Value;

            if (width.HasValue)
                line.Width = width.Value;
        }

        private static void ApplyParticleColor(Node root, string nodePath, Color? color)
        {
            if (!color.HasValue)
                return;

            if (root.GetNodeOrNull<CpuParticles2D>(nodePath) is { } particles)
                particles.Color = color.Value;
        }

        private static void ApplySpriteStyle(Node root, string nodePath, Color? modulate, Vector2? scale)
        {
            if (root.GetNodeOrNull<Sprite2D>(nodePath) is not { } sprite)
                return;

            if (modulate.HasValue)
                sprite.Modulate = modulate.Value;

            if (scale.HasValue)
                sprite.Scale = scale.Value;
        }
    }
}
