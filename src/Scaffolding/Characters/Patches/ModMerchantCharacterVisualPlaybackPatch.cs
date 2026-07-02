using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Merchant character scenes without Spine use <see cref="ModCreatureVisualPlayback" /> for
    ///     <see cref="NMerchantCharacter.PlayAnimation" /> (textures, AnimationPlayer, AnimatedSprite2D).
    ///     没有 Spine 的商人角色场景使用 <see cref="ModCreatureVisualPlayback" /> 处理
    ///     <see cref="NMerchantCharacter.PlayAnimation" />（纹理、AnimationPlayer、AnimatedSprite2D）。
    /// </summary>
    internal class ModMerchantCharacterVisualPlaybackPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<Node, StateMachineSlot> StateMachinesByRoot = new();
        public static string PatchId => "mod_merchant_character_visual_playback";

        public static string Description =>
            "Play non-Spine merchant character animations via ModCreatureVisualPlayback";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))];
        }

        public static bool Prefix(NMerchantCharacter __instance, string anim, bool loop)
        {
            var children = __instance.GetChildren();
            if (children.Count == 0)
                return true;

            ModCreatureVisualPlayback.TryResolveMerchantCharacterModel(__instance, out var character);

            if (TryRouteToStateMachine(__instance, character, anim))
                return false;

            if (children[0].GetType().Name.Equals(MegaSprite.spineClassName))
                return true;

            var worldCues = TryGetMerchantWorldCueSet(character);
            return !ModCreatureVisualPlayback.TryPlayOnVisualRoot(__instance, character, anim, loop, worldCues);
        }

        private static bool TryRouteToStateMachine(NMerchantCharacter merchant, CharacterModel? character, string anim)
        {
            if (character is not IModCharacterMerchantAnimationStateMachineFactory factory)
                return false;

            var slot = StateMachinesByRoot.GetValue(merchant, _ => new());
            slot.EnsureBuilt(factory, merchant, character);

            if (slot.StateMachine == null)
                return false;

            slot.StateMachine.SetTrigger(ModCreatureVisualPlayback.MapWorldAnimationToStateMachineTrigger(anim));
            return true;
        }

        private static VisualCueSet? TryGetMerchantWorldCueSet(CharacterModel? character)
        {
            return character is not IModCharacterAssetOverrides
            {
                WorldProceduralVisuals.Merchant.CueSet: { } cueSet,
            }
                ? null
                : cueSet;
        }

        private sealed class StateMachineSlot
        {
            private bool _built;
            public ModAnimStateMachine? StateMachine { get; private set; }

            public void EnsureBuilt(IModCharacterMerchantAnimationStateMachineFactory factory, Node root,
                CharacterModel character)
            {
                if (_built)
                    return;

                _built = true;
                StateMachine = factory.TryCreateMerchantAnimationStateMachine(root, character);
            }
        }
    }
}
