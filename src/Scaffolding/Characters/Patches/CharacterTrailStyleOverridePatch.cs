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
    ///     <para xml:lang="en">
    ///         Converts compatible card-trail scenes to <see cref="NCardTrailVfx" /> and, after creation, applies the
    ///         colors, widths, and scales from <see cref="IModCharacterAssetOverrides.CustomTrailStyle" /> to
    ///         recognized line, particle, and sprite nodes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将兼容的卡牌拖尾场景转换为 <see cref="NCardTrailVfx" />，并在创建后把
    ///         <see cref="IModCharacterAssetOverrides.CustomTrailStyle" /> 中的颜色、宽度和缩放应用到可识别的线条、
    ///         粒子及精灵节点。
    ///     </para>
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
