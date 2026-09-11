using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Replaces the crystal-sphere card-reward back texture and/or material when a card pool supplies
    ///         <see cref="ICrystalSphereCardPool" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当卡池提供 <see cref="ICrystalSphereCardPool" /> 时，按非空项替换水晶球卡奖励格的底图和/或材质。
    ///     </para>
    /// </summary>
    internal class CrystalSphereCardPoolCardBackPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NCrystalSphereItem, CrystalSphereItem> ItemRef =
            AccessTools.FieldRefAccess<NCrystalSphereItem, CrystalSphereItem>("_item");

        private static readonly AccessTools.FieldRef<NCrystalSphereItem, TextureRect> CardFrameRef =
            AccessTools.FieldRefAccess<NCrystalSphereItem, TextureRect>("_cardFrame");

        private static readonly AccessTools.FieldRef<CrystalSphereCardReward, Player> OwnerRef =
            AccessTools.FieldRefAccess<CrystalSphereCardReward, Player>("_owner");

        public static string PatchId => "crystal_sphere_card_pool_card_back";
        public static string Description =>
            "Allow card pools to replace the crystal-sphere card-reward back texture and material";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
            [new(typeof(NCrystalSphereItem), nameof(NCrystalSphereItem._Ready))];

        public static void Postfix(NCrystalSphereItem __instance)
        {
            if (ItemRef(__instance) is not CrystalSphereCardReward reward)
                return;
            if (OwnerRef(reward).Character?.CardPool is not ICrystalSphereCardPool overrides)
                return;

            var cardFrame = CardFrameRef(__instance);
            if (cardFrame == null || !GodotObject.IsInstanceValid(cardFrame))
                return;

            if (overrides.CrystalSphereCardTexture is { } texture && GodotObject.IsInstanceValid(texture))
                cardFrame.Texture = texture;
            if (overrides.CrystalSphereCardMaterial is { } material && GodotObject.IsInstanceValid(material))
                cardFrame.Material = material;
        }
    }
}
