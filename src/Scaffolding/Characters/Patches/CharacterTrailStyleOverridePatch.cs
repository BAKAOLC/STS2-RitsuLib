using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     After <see cref="NCardTrailVfx.Create" />, applies <see cref="IModCharacterAssetOverrides.CustomTrailStyle" />
    ///     modulates and widths to line, particle, and sprite nodes when present.
    ///     在 <see cref="NCardTrailVfx.Create" /> 之后，如果存在，则将 <see cref="IModCharacterAssetOverrides.CustomTrailStyle" /> 的
    ///     modulate 和宽度应用到 line、particle 和 sprite 节点。
    /// </summary>
    internal class CharacterTrailStyleOverridePatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NCardTrailVfx, Control> NodeToFollowRef =
            AccessTools.FieldRefAccess<NCardTrailVfx, Control>("_nodeToFollow");

        public static string PatchId => "character_trail_style_override";

        public static string Description =>
            "Allow mod characters to reuse a vanilla trail scene and override its visual properties";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create), [typeof(Control), typeof(string)])];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Control card, string characterTrailPath, ref NCardTrailVfx? __result)
        {
            var scene = ContentAssetOverridePatchHelper.ResolveScene(characterTrailPath);
            if (scene == null)
                return true;

            try
            {
                var created = RitsuGodotNodeFactories.CreateFromScene<NCardTrailVfx>(
                    scene,
                    PackedScene.GenEditState.Disabled);
                NodeToFollowRef(created) = card;
                __result = created;
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Godot] Failed to auto-convert card trail scene '{characterTrailPath}' to {nameof(NCardTrailVfx)}: {ex.Message}. Falling back.");
                return true;
            }
        }

        public static void Postfix(Control card, ref NCardTrailVfx? __result)
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
