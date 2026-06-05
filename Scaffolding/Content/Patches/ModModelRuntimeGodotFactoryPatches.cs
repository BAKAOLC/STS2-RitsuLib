using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Harmony patches that call mod runtime Godot factory interfaces from vanilla model entry points. Prefixes use
    ///     Harmony <c>Priority.First</c> so path-based overrides still run when factories return <c>null</c>.
    ///     从原版模型入口点调用 mod 运行时 Godot 工厂接口的 Harmony 补丁。前缀使用
    ///     Harmony <c>Priority.First</c> 因此当工厂返回 <c>null</c> 时，基于路径的覆盖仍会运行。
    /// </summary>
    public static class ModModelRuntimeGodotFactoryPatches
    {
        /// <summary>
        ///     Patches <see cref="MonsterModel.CreateVisuals" /> for <see cref="IModCreatureVisualsFactory" />.
        ///     为 <see cref="IModCreatureVisualsFactory" /> 修补<see cref="MonsterModel.CreateVisuals" />。
        /// </summary>
        public class MonsterCreatureVisualsRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_monster_creature_visuals";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod monsters to supply NCreatureVisuals from code before VisualsPath load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))];
            }

            /// <summary>
            ///     Uses <see cref="IModCreatureVisualsFactory.TryCreateCreatureVisuals" /> when it returns non-null,
            ///     falling back to the obsolete <see cref="IModMonsterCreatureVisualsFactory" /> for existing mods.
            ///     当 <see cref="IModCreatureVisualsFactory.TryCreateCreatureVisuals" /> 返回非 null 时使用它，
            ///     并为现有 mod 回退到已过时的 <see cref="IModMonsterCreatureVisualsFactory" />。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
            {
                NCreatureVisuals? created = null;
                if (__instance is IModCreatureVisualsFactory factory)
                    created = factory.TryCreateCreatureVisuals();

#pragma warning disable CS0618
                if (created == null && __instance is IModMonsterCreatureVisualsFactory legacyFactory)
                    created = legacyFactory.TryCreateCreatureVisuals();
#pragma warning restore CS0618

                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="CharacterModel.CreateVisuals" /> for <see cref="IModCreatureVisualsFactory" />.
        ///     为 <see cref="IModCreatureVisualsFactory" /> 修补<see cref="CharacterModel.CreateVisuals" />。
        /// </summary>
        public class CharacterCreatureVisualsRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_character_creature_visuals";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod characters to supply NCreatureVisuals from code before VisualsPath load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))];
            }

            /// <summary>
            ///     Uses <see cref="IModCreatureVisualsFactory.TryCreateCreatureVisuals" /> when it returns non-null,
            ///     falling back to the obsolete <see cref="IModCharacterCreatureVisualsFactory" /> for existing mods.
            ///     当 <see cref="IModCreatureVisualsFactory.TryCreateCreatureVisuals" /> 返回非 null 时使用它，
            ///     并为现有 mod 回退到已过时的 <see cref="IModCharacterCreatureVisualsFactory" />。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(CharacterModel __instance, ref NCreatureVisuals __result)
            {
                NCreatureVisuals? created = null;
                if (__instance is IModCreatureVisualsFactory factory)
                    created = factory.TryCreateCreatureVisuals();

#pragma warning disable CS0618
                if (created == null && __instance is IModCharacterCreatureVisualsFactory legacyFactory)
                    created = legacyFactory.TryCreateCreatureVisuals();
#pragma warning restore CS0618

                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="CharacterModel.GenerateAnimator" /> for
        ///     <see cref="IModCreatureAnimatorFactory" />.
        ///     为 <see cref="IModCreatureAnimatorFactory" /> 修补
        ///     <see cref="CharacterModel.GenerateAnimator" />。
        /// </summary>
        public class CharacterCreatureAnimatorRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_character_creature_animator";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod characters to supply CreatureAnimator (Spine state graph) from code";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CharacterModel), nameof(CharacterModel.GenerateAnimator))];
            }

            /// <summary>
            ///     Uses <see cref="IModCreatureAnimatorFactory.TryCreateCreatureAnimator" /> when it returns non-null,
            ///     falling back to the obsolete <see cref="IModCharacterCreatureAnimatorFactory" /> for existing mods.
            ///     当 <see cref="IModCreatureAnimatorFactory.TryCreateCreatureAnimator" /> 返回非 null 时使用它，
            ///     并为现有 mod 回退到已过时的 <see cref="IModCharacterCreatureAnimatorFactory" />。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(CharacterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
            {
                CreatureAnimator? created = null;
                if (__instance is IModCreatureAnimatorFactory factory)
                    created = factory.TryCreateCreatureAnimator(controller);

#pragma warning disable CS0618
                if (created == null && __instance is IModCharacterCreatureAnimatorFactory legacyFactory)
                    created = legacyFactory.TryCreateCreatureAnimator(controller);
#pragma warning restore CS0618

                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="MonsterModel.GenerateAnimator" /> for <see cref="IModCreatureAnimatorFactory" />.
        ///     为 <see cref="IModCreatureAnimatorFactory" /> 修补<see cref="MonsterModel.GenerateAnimator" />。
        /// </summary>
        public class MonsterCreatureAnimatorRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_monster_creature_animator";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod monsters to supply CreatureAnimator (Spine state graph) from code";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(MonsterModel), nameof(MonsterModel.GenerateAnimator))];
            }

            /// <summary>
            ///     Uses <see cref="IModCreatureAnimatorFactory.TryCreateCreatureAnimator" /> when it returns non-null.
            ///     当 <see cref="IModCreatureAnimatorFactory.TryCreateCreatureAnimator" /> 返回非 null 时使用它。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
            {
                if (__instance is not IModCreatureAnimatorFactory factory)
                    return true;

                var created = factory.TryCreateCreatureAnimator(controller);
                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="EncounterModel.CreateScene" /> for <see cref="IModEncounterCombatSceneFactory" />.
        ///     为 <see cref="IModEncounterCombatSceneFactory" /> 修补<see cref="EncounterModel.CreateScene" />。
        /// </summary>
        public class EncounterCombatSceneRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_encounter_combat_scene";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod encounters to supply combat Control from code before encounter scene path load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EncounterModel), nameof(EncounterModel.CreateScene))];
            }

            /// <summary>
            ///     Uses <see cref="IModEncounterCombatSceneFactory.TryCreateEncounterCombatScene" /> when it returns non-null.
            ///     当 <see cref="IModEncounterCombatSceneFactory.TryCreateEncounterCombatScene" /> 返回非 null 时使用它。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EncounterModel __instance, ref Control __result)
            {
                if (__instance is not IModEncounterCombatSceneFactory factory)
                    return true;

                var created = factory.TryCreateEncounterCombatScene();
                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="EventModel.CreateScene" /> for <see cref="IModEventLayoutPackedSceneFactory" />.
        ///     为 <see cref="IModEventLayoutPackedSceneFactory" /> 修补<see cref="EventModel.CreateScene" />。
        /// </summary>
        public class EventLayoutPackedSceneRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_event_layout_packed_scene";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod events to supply layout PackedScene from code before LayoutScenePath load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EventModel), nameof(EventModel.CreateScene))];
            }

            /// <summary>
            ///     Uses <see cref="IModEventLayoutPackedSceneFactory.TryCreateLayoutPackedScene" /> when it returns non-null.
            ///     当 <see cref="IModEventLayoutPackedSceneFactory.TryCreateLayoutPackedScene" /> 返回非 null 时使用它。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref PackedScene __result)
            {
                if (__instance is not IModEventLayoutPackedSceneFactory factory)
                    return true;

                var created = factory.TryCreateLayoutPackedScene();
                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="EventModel.CreateBackgroundScene" /> for
        ///     <see cref="IModEventBackgroundPackedSceneFactory" />.
        ///     为
        ///     <see cref="IModEventBackgroundPackedSceneFactory" /> 修补 <see cref="EventModel.CreateBackgroundScene" />。
        /// </summary>
        public class EventBackgroundPackedSceneRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_event_background_packed_scene";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Allow mod events to supply background PackedScene from code before BackgroundScenePath load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))];
            }

            /// <summary>
            ///     Uses <see cref="IModEventBackgroundPackedSceneFactory.TryCreateBackgroundPackedScene" /> when it returns
            ///     non-null.
            ///     当 <see cref="IModEventBackgroundPackedSceneFactory.TryCreateBackgroundPackedScene" /> 返回
            ///     非 null 时使用。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref PackedScene __result)
            {
                if (__instance is IModAncientEventAssetOverrides
                    {
                        AncientPresentationAssetProfile.StageProcedural: not null,
                    })
                    return true;

                if (__instance is not IModEventBackgroundPackedSceneFactory factory)
                    return true;

                var created = factory.TryCreateBackgroundPackedScene();
                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <c>EventModel.HasVfx</c> for <see cref="IModEventVfxFactory" />.
        ///     为 <see cref="IModEventVfxFactory" /> 修补<c>EventModel.HasVfx</c>。
        /// </summary>
        public class EventHasVfxRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_event_has_vfx";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description => "Treat mod event Vfx factory as HasVfx when flagged";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EventModel), "HasVfx", MethodType.Getter)];
            }

            /// <summary>
            ///     Yields <c>true</c> when <see cref="IModEventVfxFactory.SuppliesCustomEventVfx" /> is set.
            ///     当 <see cref="IModEventVfxFactory.SuppliesCustomEventVfx" /> 已设置时生成 <c>true</c>。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref bool __result)
            {
                if (__instance is not IModEventVfxFactory { SuppliesCustomEventVfx: true })
                    return true;

                __result = true;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="EventModel.CreateVfx" /> for <see cref="IModEventVfxFactory" />.
        ///     为 <see cref="IModEventVfxFactory" /> 修补<see cref="EventModel.CreateVfx" />。
        /// </summary>
        public class EventCreateVfxRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_event_create_vfx";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description => "Allow mod events to supply VFX Node2D from code before VfxPath load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EventModel), nameof(EventModel.CreateVfx))];
            }

            /// <summary>
            ///     Uses <see cref="IModEventVfxFactory.TryCreateEventVfx" /> when it returns non-null.
            ///     当 <see cref="IModEventVfxFactory.TryCreateEventVfx" /> 返回非 null 时使用它。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref Node2D __result)
            {
                if (__instance is not IModEventVfxFactory { SuppliesCustomEventVfx: true } factory)
                    return true;

                var created = factory.TryCreateEventVfx();
                if (created == null)
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="OrbModel.CreateSprite" /> for <see cref="IModOrbSpriteFactory" />.
        ///     为 <see cref="IModOrbSpriteFactory" /> 修补<see cref="OrbModel.CreateSprite" />。
        /// </summary>
        public class OrbSpriteRuntimeFactoryPatch : IPatchMethod
        {
            /// <inheritdoc cref="IPatchMethod.PatchId" />
            public static string PatchId => "runtime_godot_factory_orb_sprite";

            /// <inheritdoc cref="IPatchMethod.IsCritical" />
            public static bool IsCritical => false;

            /// <inheritdoc cref="IPatchMethod.Description" />
            public static string Description =>
                "Mod orbs: code factory first, then Ritsu Godot Node2D scene conversion (baselib-style tscn) before raw vanilla load";

            /// <inheritdoc cref="IPatchMethod.GetTargets" />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbModel), nameof(OrbModel.CreateSprite))];
            }

            /// <summary>
            ///     Uses <see cref="IModOrbSpriteFactory.TryCreateOrbSprite" /> when it returns non-null.
            ///     当 <see cref="IModOrbSpriteFactory.TryCreateOrbSprite" /> 返回非 null 时使用它。
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(OrbModel __instance, ref Node2D __result)
            {
                if (__instance is IModOrbSpriteFactory spriteFactory)
                {
                    var fromFactory = spriteFactory.TryCreateOrbSprite();
                    if (fromFactory != null)
                    {
                        __result = fromFactory;
                        return false;
                    }
                }

                if (__instance is not IModOrbAssetOverrides)
                    return true;

                var path = __instance.SpritePath;
                if (string.IsNullOrEmpty(path) || !GodotResourcePath.ResourceExists(path))
                    return true;

                var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
                if (scene == null)
                {
                    ContentAssetOverridePatchHelper.LogLoadFailure(__instance,
                        nameof(IModOrbAssetOverrides.CustomVisualsScenePath), path, nameof(PackedScene));
                    return true;
                }

                var node2D = RitsuGodotNodeFactories.CreateFromScene<Node2D>(scene, PackedScene.GenEditState.Disabled);
                if (node2D.GetNodeOrNull("SpineSkeleton") is { } spineNode)
                    new MegaSprite(spineNode).GetAnimationState().SetAnimation("idle_loop");

                __result = node2D;
                return false;
            }
        }
    }
}
