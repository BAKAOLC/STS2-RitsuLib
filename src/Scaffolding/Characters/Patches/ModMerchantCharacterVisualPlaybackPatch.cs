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

        private static readonly ConditionalWeakTable<NMerchantCharacter, RegisteredMerchantVisual>
            RitsuMerchantVisuals =
                new();

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
            var isRitsuMerchantVisual = TryGetRegisteredMerchantCharacter(__instance, out var registeredCharacter);
            if (__instance.GetChildCount() == 0)
                return !isRitsuMerchantVisual;

            if (!ModCreatureVisualPlayback.TryResolveMerchantCharacterModel(__instance, out var character))
                character = registeredCharacter;

            if (TryRouteToStateMachine(__instance, character, anim))
                return false;

            if (IsFirstChildSpine(__instance))
                return true;

            var worldCues = TryGetMerchantWorldCueSet(character);
            if (ModCreatureVisualPlayback.TryPlayOnVisualRoot(__instance, character, anim, loop, worldCues))
                return false;

            return !isRitsuMerchantVisual;
        }

        internal static void RegisterRitsuMerchantVisual(NMerchantCharacter visual, CharacterModel character)
        {
            RitsuMerchantVisuals.Remove(visual);
            RitsuMerchantVisuals.Add(visual, new(character));
        }

        internal static bool IsFirstChildSpine(NMerchantCharacter visual)
        {
            return visual.GetChildCount() > 0 && visual.GetChild(0).GetClass() == MegaSprite.spineClassName;
        }

        internal static bool TryGetRegisteredMerchantCharacter(NMerchantCharacter visual,
            out CharacterModel? character)
        {
            if (RitsuMerchantVisuals.TryGetValue(visual, out var registration))
            {
                character = registration.Character;
                return true;
            }

            character = null;
            return false;
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

        private sealed class RegisteredMerchantVisual(CharacterModel character)
        {
            public CharacterModel Character { get; } = character;
        }
    }

    /// <summary>
    ///     Skips vanilla <see cref="NMerchantCharacter._Ready" /> for non-Spine merchant visuals so the vanilla
    ///     <see cref="MegaSprite" /> constructor does not reject procedural or Godot-animation roots.
    ///     对非 Spine 商人视觉跳过原版 <see cref="NMerchantCharacter._Ready" />，避免原版 <see cref="MegaSprite" />
    ///     构造函数拒绝程序化或 Godot 动画根节点。
    /// </summary>
    internal class ModMerchantCharacterReadyPlaybackPatch : IPatchMethod
    {
        public static string PatchId => "mod_merchant_character_ready_visual_playback";

        public static string Description =>
            "Initialize non-Spine merchant character visuals without constructing MegaSprite";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))];
        }

        public static bool Prefix(NMerchantCharacter __instance)
        {
            if (!ModMerchantCharacterVisualPlaybackPatch.TryGetRegisteredMerchantCharacter(__instance, out _))
                return true;

            if (ModMerchantCharacterVisualPlaybackPatch.IsFirstChildSpine(__instance))
                return true;

            __instance.PlayAnimation("relaxed_loop", true);
            return false;
        }
    }
}
