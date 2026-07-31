using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Patches;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Godot.NodeFactories;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies Harmony prefixes that invoke mod runtime Godot factories from vanilla model entry points.
    ///         A factory returning <see langword="null" />, throwing, or producing an invalid Godot object leaves the
    ///         original path available to later override patches and the base game.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供从原版模型入口调用模组运行时 Godot 工厂的 Harmony 前缀。工厂返回 <see langword="null" />、抛出异常或
    ///         产生无效的 Godot 对象时，后续覆盖补丁和游戏本体仍可使用原有路径。
    ///     </para>
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

        private static bool TryInvokeGodotFactory<TResult>(
            object owner,
            string memberName,
            Func<TResult?> factory,
            out TResult created)
            where TResult : GodotObject
        {
            created = null!;

            TResult? candidate;
            try
            {
                candidate = factory();
            }
            catch (Exception ex)
            {
                LogRuntimeFactoryFailure(owner, memberName, ex);
                return false;
            }

            if (candidate == null)
                return false;

            if (!GodotObject.IsInstanceValid(candidate))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Godot] Runtime factory {DescribeFactoryOwner(owner)}.{memberName} returned an invalid {typeof(TResult).Name}. Falling back.");
                return false;
            }

            created = candidate;
            return true;
        }

        private static bool TryInvokeFactory<TResult>(
            object owner,
            string memberName,
            Func<TResult?> factory,
            out TResult created)
            where TResult : class
        {
            created = null!;

            try
            {
                var candidate = factory();
                if (candidate == null)
                    return false;

                created = candidate;
                return true;
            }
            catch (Exception ex)
            {
                LogRuntimeFactoryFailure(owner, memberName, ex);
                return false;
            }
        }

        private static bool TryGetFactoryFlag(object owner, string memberName, Func<bool> accessor)
        {
            try
            {
                return accessor();
            }
            catch (Exception ex)
            {
                LogRuntimeFactoryFailure(owner, memberName, ex);
                return false;
            }
        }

        private static void LogRuntimeFactoryFailure(object owner, string memberName, Exception ex)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Godot] Runtime factory {DescribeFactoryOwner(owner)}.{memberName} failed: {ex.Message}. Falling back.");
        }

        private static string DescribeFactoryOwner(object owner)
        {
            return owner.GetType().FullName ?? owner.GetType().Name;
        }

        /// <summary>
        ///     <para xml:lang="en">Integrates runtime creature-visual factories with <see cref="MonsterModel.CreateVisuals" />.</para>
        ///     <para xml:lang="zh-CN">将运行时生物视觉工厂接入 <see cref="MonsterModel.CreateVisuals" />。</para>
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
            ///     <para xml:lang="en">
            ///         Uses a valid result from <see cref="IModCreatureVisualsFactory.TryCreateCreatureVisuals" />; otherwise
            ///         attempts the obsolete monster-specific factory, then permits the original method to run.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         使用 <see cref="IModCreatureVisualsFactory.TryCreateCreatureVisuals" /> 的有效结果；否则尝试已过时的
            ///         怪物专用工厂，仍无结果时让原方法继续执行。
            ///     </para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
            {
                // Preserve the explicit preference order between current and obsolete factory interfaces.
                // ReSharper disable once DuplicatedSequentialIfBodies
                if (__instance is IModCreatureVisualsFactory factory &&
                    TryInvokeGodotFactory(
                        __instance,
                        nameof(IModCreatureVisualsFactory.TryCreateCreatureVisuals),
                        factory.TryCreateCreatureVisuals,
                        out NCreatureVisuals created))
                    return UseCreatedVisuals(created, out __result);

#pragma warning disable CS0618
                if (__instance is IModMonsterCreatureVisualsFactory legacyFactory &&
                    TryInvokeGodotFactory(
                        __instance,
                        nameof(IModMonsterCreatureVisualsFactory.TryCreateCreatureVisuals),
                        legacyFactory.TryCreateCreatureVisuals,
                        out created))
                    return UseCreatedVisuals(created, out __result);
#pragma warning restore CS0618

                return true;

                static bool UseCreatedVisuals(NCreatureVisuals created, out NCreatureVisuals result)
                {
                    ModCreatureVisualPlayback.RegisterRitsuCreatureVisual(created);
                    result = created;
                    return false;
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Integrates creature-visual factories and profile resources with
        ///         <see cref="CharacterModel.CreateVisuals" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">将生物视觉工厂和配置资源接入 <see cref="CharacterModel.CreateVisuals" />。</para>
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
            ///     <para xml:lang="en">
            ///         Prefers a valid general factory result, then the obsolete character-specific factory, then a configured
            ///         visuals scene or texture (including the placeholder-character fallback) before the original method.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         依次优先使用通用工厂的有效结果、已过时的角色专用工厂，以及已配置的视觉场景或纹理（包括占位角色回退）；
            ///         都不可用时再执行原方法。
            ///     </para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(CharacterModel __instance, ref NCreatureVisuals __result)
            {
                // Preserve the explicit preference order between current and obsolete factory interfaces.
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                // ReSharper disable once DuplicatedSequentialIfBodies
                if (__instance is IModCreatureVisualsFactory factory &&
                    TryInvokeGodotFactory(
                        __instance,
                        nameof(IModCreatureVisualsFactory.TryCreateCreatureVisuals),
                        factory.TryCreateCreatureVisuals,
                        out NCreatureVisuals created))
                    return UseCreatedVisuals(created, out __result);

#pragma warning disable CS0618
                if (__instance is IModCharacterCreatureVisualsFactory legacyFactory &&
                    TryInvokeGodotFactory(
                        __instance,
                        nameof(IModCharacterCreatureVisualsFactory.TryCreateCreatureVisuals),
                        legacyFactory.TryCreateCreatureVisuals,
                        out created))
                    return UseCreatedVisuals(created, out __result);
#pragma warning restore CS0618

                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (!TryCreateCharacterResourceVisuals(__instance, out created))
                    return true;

                return UseCreatedVisuals(created, out __result);

                static bool UseCreatedVisuals(NCreatureVisuals created, out NCreatureVisuals result)
                {
                    RitsuNCreatureVisualsNodeFactory.EnsureFormVfxHolder(created);
                    ModCreatureVisualPlayback.RegisterRitsuCreatureVisual(created);
                    result = created;
                    return false;
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Lets a character's icon path resolve to either a <see cref="PackedScene" /> or a <see cref="Texture2D" />;
        ///         textures are wrapped in a configured <see cref="TextureRect" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使角色图标路径既可解析为 <see cref="PackedScene" />，也可解析为 <see cref="Texture2D" />；纹理会包装为
        ///         已配置的 <see cref="TextureRect" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Integrates runtime creature-animator factories with
        ///         <see cref="CharacterModel.GenerateAnimator" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">将运行时生物动画器工厂接入 <see cref="CharacterModel.GenerateAnimator" />。</para>
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
            ///     <para xml:lang="en">
            ///         Uses a non-null general animator factory result, then the obsolete character-specific factory; otherwise
            ///         permits the original method to create the animator.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         使用非空的通用动画器工厂结果，再尝试已过时的角色专用工厂；都没有结果时让原方法创建动画器。
            ///     </para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(CharacterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
            {
                // Preserve the explicit preference order between current and obsolete factory interfaces.
                // ReSharper disable once DuplicatedSequentialIfBodies
                // ReSharper disable once InvertIf
                if (__instance is IModCreatureAnimatorFactory factory &&
                    TryInvokeFactory(
                        __instance,
                        nameof(IModCreatureAnimatorFactory.TryCreateCreatureAnimator),
                        () => factory.TryCreateCreatureAnimator(controller),
                        out CreatureAnimator created))
                {
                    __result = created;
                    return false;
                }

#pragma warning disable CS0618
                // Preserve the obsolete factory as an explicit second-choice branch.
                // ReSharper disable once InvertIf
                if (__instance is IModCharacterCreatureAnimatorFactory legacyFactory &&
                    TryInvokeFactory(
                        __instance,
                        nameof(IModCharacterCreatureAnimatorFactory.TryCreateCreatureAnimator),
                        () => legacyFactory.TryCreateCreatureAnimator(controller),
                        out created))
                {
                    __result = created;
                    return false;
                }
#pragma warning restore CS0618

                return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Integrates runtime creature-animator factories with
        ///         <see cref="MonsterModel.GenerateAnimator" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">将运行时生物动画器工厂接入 <see cref="MonsterModel.GenerateAnimator" />。</para>
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
            ///     <para xml:lang="en">Uses a non-null factory result; otherwise permits the original method to create the animator.</para>
            ///     <para xml:lang="zh-CN">使用非空的工厂结果；否则让原方法创建动画器。</para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator __result)
            {
                if (__instance is not IModCreatureAnimatorFactory factory)
                    return true;

                if (!TryInvokeFactory(
                        __instance,
                        nameof(IModCreatureAnimatorFactory.TryCreateCreatureAnimator),
                        () => factory.TryCreateCreatureAnimator(controller),
                        out var created))
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Integrates encounter combat-scene factories with <see cref="EncounterModel.CreateScene" />.</para>
        ///     <para xml:lang="zh-CN">将遭遇战斗场景工厂接入 <see cref="EncounterModel.CreateScene" />。</para>
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
            ///     <para xml:lang="en">Uses a valid factory result; otherwise permits the original scene creation path.</para>
            ///     <para xml:lang="zh-CN">使用有效的工厂结果；否则让原始场景创建路径继续执行。</para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EncounterModel __instance, ref Control __result)
            {
                if (__instance is not IModEncounterCombatSceneFactory factory)
                    return true;

                if (!TryInvokeGodotFactory(
                        __instance,
                        nameof(IModEncounterCombatSceneFactory.TryCreateEncounterCombatScene),
                        factory.TryCreateEncounterCombatScene,
                        out var created))
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Integrates event-layout scene factories with <see cref="EventModel.CreateScene" />.</para>
        ///     <para xml:lang="zh-CN">将事件布局场景工厂接入 <see cref="EventModel.CreateScene" />。</para>
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
            ///     <para xml:lang="en">Uses a valid factory result; otherwise permits the original scene creation path.</para>
            ///     <para xml:lang="zh-CN">使用有效的工厂结果；否则让原始场景创建路径继续执行。</para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref PackedScene __result)
            {
                if (__instance is not IModEventLayoutPackedSceneFactory factory)
                    return true;

                if (!TryInvokeGodotFactory(
                        __instance,
                        nameof(IModEventLayoutPackedSceneFactory.TryCreateLayoutPackedScene),
                        factory.TryCreateLayoutPackedScene,
                        out var created))
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Integrates event-background scene factories with
        ///         <see cref="EventModel.CreateBackgroundScene" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">将事件背景场景工厂接入 <see cref="EventModel.CreateBackgroundScene" />。</para>
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
            ///     <para xml:lang="en">
            ///         Uses a valid factory result unless the event supplies a procedural Ancient-stage presentation, which
            ///         retains precedence; otherwise permits the original scene creation path.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         除非事件提供了优先的程序化先古事件舞台表现，否则使用有效的工厂结果；否则让原始场景创建路径继续执行。
            ///     </para>
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

                if (!TryInvokeGodotFactory(
                        __instance,
                        nameof(IModEventBackgroundPackedSceneFactory.TryCreateBackgroundPackedScene),
                        factory.TryCreateBackgroundPackedScene,
                        out var created))
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Integrates the event-VFX factory availability flag with <c>EventModel.HasVfx</c>.</para>
        ///     <para xml:lang="zh-CN">将事件视觉特效工厂的可用标志接入 <c>EventModel.HasVfx</c>。</para>
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
            ///     <para xml:lang="en">Supplies <see langword="true" /> when the factory reports custom event VFX.</para>
            ///     <para xml:lang="zh-CN">工厂报告提供自定义事件视觉特效时，返回 <see langword="true" />。</para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref bool __result)
            {
                if (__instance is not IModEventVfxFactory factory ||
                    !TryGetFactoryFlag(
                        __instance,
                        nameof(IModEventVfxFactory.SuppliesCustomEventVfx),
                        () => factory.SuppliesCustomEventVfx))
                    return true;

                __result = true;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Integrates event-VFX factories with <see cref="EventModel.CreateVfx" />.</para>
        ///     <para xml:lang="zh-CN">将事件视觉特效工厂接入 <see cref="EventModel.CreateVfx" />。</para>
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
            ///     <para xml:lang="en">Uses a valid factory result only when the factory reports custom event VFX.</para>
            ///     <para xml:lang="zh-CN">仅当工厂报告提供自定义事件视觉特效时，才使用其有效结果。</para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(EventModel __instance, ref Node2D __result)
            {
                if (__instance is not IModEventVfxFactory factory ||
                    !TryGetFactoryFlag(
                        __instance,
                        nameof(IModEventVfxFactory.SuppliesCustomEventVfx),
                        () => factory.SuppliesCustomEventVfx))
                    return true;

                if (!TryInvokeGodotFactory(
                        __instance,
                        nameof(IModEventVfxFactory.TryCreateEventVfx),
                        factory.TryCreateEventVfx,
                        out var created))
                    return true;

                __result = created;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Integrates orb sprite factories and mod scene conversion with
        ///         <see cref="OrbModel.CreateSprite" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">将充能球精灵工厂和模组场景转换接入 <see cref="OrbModel.CreateSprite" />。</para>
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
            ///     <para xml:lang="en">
            ///         Uses a valid sprite-factory result first. For an orb asset override, it next attempts to instantiate the
            ///         configured sprite path as a <see cref="Node2D" /> before allowing the original path.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         首先使用有效的精灵工厂结果。对于充能球资源覆盖，接着尝试将配置的精灵路径实例化为
            ///         <see cref="Node2D" />，失败后才让原始路径继续执行。
            ///     </para>
            /// </summary>
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(OrbModel __instance, ref Node2D __result)
            {
                if (__instance is IModOrbSpriteFactory spriteFactory)
                    if (TryInvokeGodotFactory(
                            __instance,
                            nameof(IModOrbSpriteFactory.TryCreateOrbSprite),
                            spriteFactory.TryCreateOrbSprite,
                            out var fromFactory))
                    {
                        __result = fromFactory;
                        return false;
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

                Node2D node2D;
                try
                {
                    node2D = RitsuGodotNodeFactories.CreateFromScene<Node2D>(
                        scene,
                        PackedScene.GenEditState.Disabled);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Godot] Failed to instantiate {__instance.GetType().Name}.{nameof(IModOrbAssetOverrides.CustomVisualsScenePath)} '{path}' as {nameof(Node2D)}: {ex.Message}. Falling back.");
                    return true;
                }

                if (!GodotObject.IsInstanceValid(node2D))
                {
                    ContentAssetOverridePatchHelper.LogLoadFailure(
                        __instance,
                        nameof(IModOrbAssetOverrides.CustomVisualsScenePath),
                        path,
                        nameof(Node2D));
                    return true;
                }

                if (node2D.GetNodeOrNull("SpineSkeleton") is { } spineNode)
                    new MegaSprite(spineNode).GetAnimationState().SetAnimation("idle_loop");

                __result = node2D;
                return false;
            }
        }
    }
}
