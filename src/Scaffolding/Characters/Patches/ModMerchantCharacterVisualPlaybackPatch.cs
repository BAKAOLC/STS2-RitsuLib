using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
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
    ///     <para xml:lang="en">
    ///         Routes <see cref="NMerchantCharacter.PlayAnimation" /> for registered non-Spine merchant visuals
    ///         through a mod state machine or <see cref="ModCreatureVisualPlayback" />, including texture,
    ///         <see cref="AnimationPlayer" />, and <see cref="AnimatedSprite2D" /> playback.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对已注册的非 Spine 商人形象，将 <see cref="NMerchantCharacter.PlayAnimation" /> 路由到模组状态机或
    ///         <see cref="ModCreatureVisualPlayback" />，以支持纹理、<see cref="AnimationPlayer" /> 和
    ///         <see cref="AnimatedSprite2D" /> 动画。
    ///     </para>
    /// </summary>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    internal class ModMerchantCharacterVisualPlaybackPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<Node, StateMachineSlot> StateMachinesByRoot = [];

        private static readonly ConditionalWeakTable<NMerchantCharacter, RegisteredMerchantVisual>
            RitsuMerchantVisuals =
                [];

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
            if (!TryGetRegisteredMerchantCharacter(__instance, out var character))
                return true;

            if (__instance.GetChildCount() == 0)
                return false;

            if (TryRouteToStateMachine(__instance, character, anim))
                return false;

            if (IsFirstChildSpine(__instance))
                return true;

            var worldCues = TryGetMerchantWorldCueSet(character);
            return ModCreatureVisualPlayback.TryPlayOnVisualRoot(__instance, character, anim, loop, worldCues) && false;
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

                StateMachine = factory.TryCreateMerchantAnimationStateMachine(root, character);
                _built = true;
            }
        }

        private sealed class RegisteredMerchantVisual(CharacterModel character)
        {
            public CharacterModel Character { get; } = character;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Skips the base <see cref="NMerchantCharacter._Ready" /> implementation for registered non-Spine
    ///         merchant visuals so its <see cref="MegaSprite" /> construction does not reject procedural or
    ///         Godot-animation roots.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对已注册的非 Spine 商人形象跳过游戏本体的 <see cref="NMerchantCharacter._Ready" /> 实现，避免其中的
    ///         <see cref="MegaSprite" /> 创建过程拒绝程序化根节点或 Godot 动画根节点。
    ///     </para>
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
