using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Eagerly builds combat animation state machines after the combat visuals are ready for creature models
    ///         that provide a compatible state-machine factory.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对提供兼容状态机工厂的生物模型，在战斗形象准备就绪后立即创建战斗动画状态机。
    ///     </para>
    /// </summary>
    internal class NCreatureCombatAnimationInitialBootstrapPatch : IPatchMethod
    {
        public static string PatchId => "ncreature_combat_animation_initial_bootstrap";

        public static string Description =>
            "Build combat animation state machine at NCreature._Ready for opted-in models";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature._Ready))];
        }

        public static void Postfix(NCreature __instance)
        {
            if (!HasCombatStateMachineFactory(__instance))
                return;

            _ = ModCreatureCombatAnimationPlaybackPatch.TryGetCombatAnimationStateMachine(__instance);
        }

        private static bool HasCombatStateMachineFactory(NCreature creature)
        {
            var entity = creature.Entity;
            if (entity == null)
                return false;

            var character = entity.Player?.Character;
            if (character is IModCreatureCombatAnimationStateMachineFactory
#pragma warning disable CS0618
                or IModNonSpineAnimationStateMachineFactory
#pragma warning restore CS0618
               )
                return true;

            var monster = entity.Monster;
            return monster is IModCreatureCombatAnimationStateMachineFactory
#pragma warning disable CS0618
                or IModNonSpineAnimationStateMachineFactory;
#pragma warning restore CS0618
        }
    }
}
