using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Patches;
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
    internal static class ModModelRuntimeGodotFactoryPatches
    {
        private static bool TryCreateCharacterResourceVisuals(CharacterModel character,
            out NCreatureVisuals created)
        {
            created = null!;

            if (!CharacterAssetOverridePatchHelper.TryResolveOverridePath(
                    character,
                    static o => o.CustomVisualsPath,
                    nameof(IModCharacterAssetOverrides.CustomVisualsPath),
                    out var path))
                return false;

            return TryCreateCreatureVisualsFromSceneOrTexture(
                character,
                path,
                nameof(IModCharacterAssetOverrides.CustomVisualsPath),
                out created) || TryCreateFallbackCharacterVisuals(character, out created);
        }

        private static bool TryCreateCreatureVisualsFromSceneOrTexture(
            CharacterModel character,
            string path,
            string memberName,
            out NCreatureVisuals created)
        {
            created = null!;

            try
            {
                var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
                if (scene != null)
                {
                    created = RitsuGodotNodeFactories.CreateFromScene<NCreatureVisuals>(
                        scene,
                        PackedScene.GenEditState.Disabled);
                    return true;
                }

                var texture = ContentAssetOverridePatchHelper.ResolveTexture2D(path);
                if (texture != null)
                {
                    created = RitsuGodotNodeFactories.CreateFromResource<NCreatureVisuals>(texture);
                    return true;
                }

                ContentAssetOverridePatchHelper.WarnOverrideUnavailable(
                    character,
                    memberName,
                    path,
                    $"{nameof(PackedScene)} or {nameof(Texture2D)}");
                return false;
            }
            catch (Exception ex)
            {
                LogFactoryConversionFailure(character, memberName, path, nameof(NCreatureVisuals), ex);
                return false;
            }
        }

        private static bool TryCreateCharacterIconFromSceneOrTexture(
            CharacterModel character,
            string path,
            string memberName,
            out Control created)
        {
            created = null!;

            try
            {
                var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
                if (scene != null)
                {
                    created = RitsuGodotNodeFactories.CreateFromScene<Control>(
                        scene,
                        PackedScene.GenEditState.Disabled);
                    return true;
                }

                var texture = ContentAssetOverridePatchHelper.ResolveTexture2D(path);
                if (texture != null)
                {
                    created = CreateCharacterIconFromTexture(texture);
                    return true;
                }

                ContentAssetOverridePatchHelper.WarnOverrideUnavailable(
                    character,
                    memberName,
                    path,
                    $"{nameof(PackedScene)} or {nameof(Texture2D)}");
                return false;
            }
            catch (Exception ex)
            {
                LogFactoryConversionFailure(character, memberName, path, nameof(Control), ex);
                return false;
            }
        }

        private static bool TryCreateFallbackCharacterVisuals(CharacterModel character, out NCreatureVisuals created)
        {
            created = null!;
            foreach (var path in EnumerateFallbackCharacterAssetPaths(
                         character,
                         static profile => profile.Scenes?.VisualsPath))
            {
                var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
                if (scene == null)
                    continue;

                try
                {
                    created = RitsuGodotNodeFactories.CreateFromScene<NCreatureVisuals>(
                        scene,
                        PackedScene.GenEditState.Disabled);
                    RitsuLibFramework.Logger.Warn(
                        $"[Godot] Falling back to character visuals scene '{path}' for {DescribeCharacter(character)}.");
                    return true;
                }
                catch (Exception ex)
                {
                    LogFactoryConversionFailure(
                        character,
                        nameof(IModCharacterAssetOverrides.CustomVisualsPath),
                        path,
                        nameof(NCreatureVisuals),
                        ex);
                }
            }

            return false;
        }

        private static bool TryCreateFallbackCharacterIcon(CharacterModel character, out Control created)
        {
            created = null!;
            foreach (var path in EnumerateFallbackCharacterAssetPaths(
                         character,
                         static profile => profile.Ui?.IconPath))
                try
                {
                    var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
                    if (scene != null)
                    {
                        created = RitsuGodotNodeFactories.CreateFromScene<Control>(
                            scene,
                            PackedScene.GenEditState.Disabled);
                        RitsuLibFramework.Logger.Warn(
                            $"[Godot] Falling back to character icon scene '{path}' for {DescribeCharacter(character)}.");
                        return true;
                    }

                    var texture = ContentAssetOverridePatchHelper.ResolveTexture2D(path);
                    if (texture == null)
                        continue;

                    created = CreateCharacterIconFromTexture(texture);
                    RitsuLibFramework.Logger.Warn(
                        $"[Godot] Falling back to character icon texture '{path}' for {DescribeCharacter(character)}.");
                    return true;
                }
                catch (Exception ex)
                {
                    LogFactoryConversionFailure(
                        character,
                        nameof(IModCharacterAssetOverrides.CustomIconPath),
                        path,
                        nameof(Control),
                        ex);
                }

            return false;
        }

        private static TextureRect CreateCharacterIconFromTexture(Texture2D texture)
        {
            return new()
            {
                Name = StableTextureRectNodeName(texture.ResourcePath, "CharacterIcon"),
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
        }

        private static string StableTextureRectNodeName(string? resourcePath, string fallback)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return fallback;

            var s = resourcePath.AsSpan();
            var slash = s.LastIndexOf('/');
            if (slash >= 0)
                s = s[(slash + 1)..];

            var dot = s.LastIndexOf('.');
            if (dot > 0)
                s = s[..dot];

            if (s.IsEmpty)
                return fallback;

            Span<char> buf = stackalloc char[s.Length];
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                buf[i] = char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_';
            }

            return new(buf);
        }

        private static IEnumerable<string> EnumerateFallbackCharacterAssetPaths(
            CharacterModel character,
            Func<CharacterAssetProfile, string?> selector)
        {
            var entry = character.Id.Entry;
            if (!string.IsNullOrWhiteSpace(entry))
            {
                var path = selector(CharacterAssetProfiles.FromCharacterId(entry));
                if (!string.IsNullOrWhiteSpace(path))
                    yield return path;
            }

            if (string.Equals(
                    entry,
                    CharacterAssetProfiles.DefaultPlaceholderCharacterId,
                    StringComparison.OrdinalIgnoreCase))
                yield break;

            var placeholder = selector(
                CharacterAssetProfiles.FromCharacterId(CharacterAssetProfiles.DefaultPlaceholderCharacterId));
            if (!string.IsNullOrWhiteSpace(placeholder))
                yield return placeholder;
        }

        private static void LogFactoryConversionFailure(
            CharacterModel character,
            string memberName,
            string path,
            string targetType,
            Exception ex)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Godot] Failed to auto-convert {DescribeCharacter(character)}.{memberName} '{path}' to {targetType}: {ex.Message}. Falling back.");
        }

        private static string DescribeCharacter(CharacterModel character)
        {
            try
            {
                return $"{character.GetType().Name}<{character.Id.Entry}>";
            }
            catch
            {
                return character.GetType().Name;
            }
        }

        /// <summary>
        ///     Patches <see cref="MonsterModel.CreateVisuals" /> for <see cref="IModCreatureVisualsFactory" />.
        ///     为 <see cref="IModCreatureVisualsFactory" /> 修补<see cref="MonsterModel.CreateVisuals" />。
        /// </summary>
        internal class MonsterCreatureVisualsRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_monster_creature_visuals";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod monsters to supply NCreatureVisuals from code before VisualsPath load";

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
        internal class CharacterCreatureVisualsRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_character_creature_visuals";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod characters to supply or auto-convert NCreatureVisuals before VisualsPath load";

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

                if (created == null && !TryCreateCharacterResourceVisuals(__instance, out created)) return true;
                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="CharacterModel.Icon" /> so <see cref="CharacterAssetProfile" /><c>.Ui.IconPath</c> may
        ///     be either the vanilla <see cref="PackedScene" /> or a plain <see cref="Texture2D" />.
        ///     修补 <see cref="CharacterModel.Icon" />，让 <see cref="CharacterAssetProfile" /><c>.Ui.IconPath</c>
        ///     可以是原版 <see cref="PackedScene" />，也可以是普通 <see cref="Texture2D" />。
        /// </summary>
        internal class CharacterIconRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_character_icon";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow character IconPath to load PackedScene or auto-convert Texture2D into a Control icon";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CharacterModel), nameof(CharacterModel.Icon), MethodType.Getter)];
            }

            [HarmonyPriority(Priority.First)]
            public static bool Prefix(CharacterModel __instance, ref Control __result)
            {
                if (!CharacterAssetOverridePatchHelper.TryResolveOverridePath(
                        __instance,
                        static o => o.CustomIconPath,
                        nameof(IModCharacterAssetOverrides.CustomIconPath),
                        out var path))
                    return true;

                if (!TryCreateCharacterIconFromSceneOrTexture(
                        __instance,
                        path,
                        nameof(IModCharacterAssetOverrides.CustomIconPath),
                        out var icon) && !TryCreateFallbackCharacterIcon(__instance, out icon)) return true;
                __result = icon;
                return false;
            }
        }

        /// <summary>
        ///     Patches <see cref="CharacterModel.GenerateAnimator" /> for
        ///     <see cref="IModCreatureAnimatorFactory" />.
        ///     为 <see cref="IModCreatureAnimatorFactory" /> 修补
        ///     <see cref="CharacterModel.GenerateAnimator" />。
        /// </summary>
        internal class CharacterCreatureAnimatorRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_character_creature_animator";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod characters to supply CreatureAnimator (Spine state graph) from code";

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
        internal class MonsterCreatureAnimatorRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_monster_creature_animator";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod monsters to supply CreatureAnimator (Spine state graph) from code";

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
        internal class EncounterCombatSceneRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_encounter_combat_scene";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod encounters to supply combat Control from code before encounter scene path load";

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
        internal class EventLayoutPackedSceneRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_event_layout_packed_scene";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod events to supply layout PackedScene from code before LayoutScenePath load";

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

                if (!ContentAssetOverridePatchHelper.IsPackedSceneOverrideAvailable(
                        __instance,
                        created,
                        nameof(IModEventLayoutPackedSceneFactory.TryCreateLayoutPackedScene),
                        $"runtime factory '{factory.GetType().FullName}'"))
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
        internal class EventBackgroundPackedSceneRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_event_background_packed_scene";
            public static bool IsCritical => false;

            public static string Description =>
                "Allow mod events to supply background PackedScene from code before BackgroundScenePath load";

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

                if (!ContentAssetOverridePatchHelper.IsPackedSceneOverrideAvailable(
                        __instance,
                        created,
                        nameof(IModEventBackgroundPackedSceneFactory.TryCreateBackgroundPackedScene),
                        $"runtime factory '{factory.GetType().FullName}'"))
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     Patches <c>EventModel.HasVfx</c> for <see cref="IModEventVfxFactory" />.
        ///     为 <see cref="IModEventVfxFactory" /> 修补<c>EventModel.HasVfx</c>。
        /// </summary>
        internal class EventHasVfxRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_event_has_vfx";
            public static bool IsCritical => false;
            public static string Description => "Treat mod event Vfx factory as HasVfx when flagged";

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
        internal class EventCreateVfxRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_event_create_vfx";
            public static bool IsCritical => false;
            public static string Description => "Allow mod events to supply VFX Node2D from code before VfxPath load";

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
        internal class OrbSpriteRuntimeFactoryPatch : IPatchMethod
        {
            public static string PatchId => "runtime_godot_factory_orb_sprite";
            public static bool IsCritical => false;

            public static string Description =>
                "Mod orbs: code factory first, then Ritsu Godot Node2D scene conversion (baselib-style tscn) before raw vanilla load";

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
