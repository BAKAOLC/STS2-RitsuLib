using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Converts character-select background scenes through <see cref="RitsuGodotNodeFactories" /> so ordinary
    ///     <see cref="Node" /> / <see cref="CanvasItem" /> roots can be safely wrapped as <see cref="Control" />;
    ///     <see cref="Texture2D" /> paths become full-cover background controls.
    ///     通过 <see cref="RitsuGodotNodeFactories" /> 转换角色选择背景场景，使普通 <see cref="Node" /> /
    ///     <see cref="CanvasItem" /> 根节点可以安全包装为 <see cref="Control" />；<see cref="Texture2D" /> 路径会成为全覆盖背景
    ///     control。
    /// </summary>
    internal class CharacterSelectBackgroundRuntimeFactoryPatch : IPatchMethod
    {
        private static readonly MethodInfo? InstantiateControlMethod = ResolveInstantiateControlMethod();

        private static readonly MethodInfo? PreloadManagerCacheGetter =
            AccessTools.DeclaredPropertyGetter(typeof(PreloadManager), nameof(PreloadManager.Cache));

        private static readonly MethodInfo? GetSceneMethod =
            AccessTools.DeclaredMethod(typeof(AssetCache), nameof(AssetCache.GetScene), [typeof(string)]);

        private static readonly MethodInfo? CharacterSelectBgGetter =
            AccessTools.DeclaredPropertyGetter(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg));

        private static readonly MethodInfo CreateControlFromCharacterSelectBgPathMethod =
            AccessTools.DeclaredMethod(typeof(CharacterSelectBackgroundRuntimeFactoryPatch),
                nameof(CreateControlFromCharacterSelectBgPath));

        public static string PatchId => "character_select_background_runtime_factory";

        public static string Description =>
            "Convert character-select background paths to full-cover Control backgrounds via RitsuGodotNodeFactories";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCharacterSelectScreen), "OnLocalCharacterChangedForRandom", [typeof(CharacterModel)]),
                new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter),
                    [typeof(NCharacterSelectButton), typeof(CharacterModel)]),
                new(typeof(NMultiplayerLoadGameScreen), "AfterMultiplayerStarted"),
            ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const string operation = "[Godot] Character select background runtime factory";
            var rewriter = HarmonyIlRewriter.From(instructions);
            if (PreloadManagerCacheGetter == null ||
                GetSceneMethod == null ||
                CharacterSelectBgGetter == null ||
                InstantiateControlMethod == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{operation} could not resolve required reflection handles; patch skipped.");
                return rewriter.InstructionsChecked(operation);
            }

            var matches = FindCharacterSelectBgSceneLoadMatches(rewriter.Code);
            if (matches.Count == 0)
            {
                if (!rewriter.Contains(instruction =>
                        HarmonyIl.IsCallTo(instruction, CreateControlFromCharacterSelectBgPathMethod)))
                    RitsuLibFramework.Logger.Warn(
                        $"{operation} found no character-select background scene load to replace.");

                return rewriter.InstructionsChecked(operation);
            }

            foreach (var match in matches.OrderByDescending(static match => match.Index))
                rewriter.Replace(match, BuildCharacterSelectBgReplacement(rewriter.Code, match));

            return rewriter.InstructionsChecked(operation);
        }

        private static IReadOnlyList<HarmonyIlMatch> FindCharacterSelectBgSceneLoadMatches(
            IReadOnlyList<CodeInstruction> code)
        {
            var matches = new List<HarmonyIlMatch>();
            matches.AddRange(CharacterSelectBgSceneLoadPattern(true).FindAll(code));
            matches.AddRange(CharacterSelectBgSceneLoadPattern(false).FindAll(code));
            return matches.OrderBy(static match => match.Index).ToArray();
        }

        private static HarmonyIlPattern CharacterSelectBgSceneLoadPattern(bool includeConvI8)
        {
            var sharedPrefix = new[]
            {
                HarmonyIl.IsCall(PreloadManagerCacheGetter),
                HarmonyIl.OneOf(HarmonyIl.IsLdarg(), HarmonyIl.IsLdloc()),
                HarmonyIl.IsCall(CharacterSelectBgGetter),
                HarmonyIl.IsCall(GetSceneMethod),
                HarmonyIl.IsLdcI4((int)PackedScene.GenEditState.Disabled),
            };

            return includeConvI8
                ? HarmonyIlPattern.Sequence(
                    [.. sharedPrefix, HarmonyIl.Is(OpCodes.Conv_I8), HarmonyIl.IsCall(InstantiateControlMethod)])
                : HarmonyIlPattern.Sequence([.. sharedPrefix, HarmonyIl.IsCall(InstantiateControlMethod)]);
        }

        private static IReadOnlyList<CodeInstruction> BuildCharacterSelectBgReplacement(
            IReadOnlyList<CodeInstruction> code,
            HarmonyIlMatch match)
        {
            return
            [
                code[match.Index + 1].Clone(),
                code[match.Index + 2].Clone(),
                HarmonyIl.Call(CreateControlFromCharacterSelectBgPathMethod),
            ];
        }

        private static Control CreateControlFromCharacterSelectBgPath(string path)
        {
            try
            {
                var scene = ContentAssetOverridePatchHelper.ResolveScene(path);
                if (scene != null)
                {
                    var control = RitsuGodotNodeFactories.CreateFromScene<Control>(
                        scene,
                        PackedScene.GenEditState.Disabled);
                    EnsureUnframedControlCoversParent(control);
                    return control;
                }

                var texture = ContentAssetOverridePatchHelper.ResolveTexture2D(path);
                if (texture != null)
                    return CreateFullCoverTextureRect(texture);

                RitsuLibFramework.Logger.Warn(
                    $"[Godot] Character select background '{path}' did not resolve as {nameof(PackedScene)} or {nameof(Texture2D)}. Using an empty background.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Godot] Failed to create character select background from '{path}': {ex.Message}. Using an empty background.");
            }

            return CreateEmptyFullRectControl("CharacterSelectBgFallback");
        }

        private static void EnsureUnframedControlCoversParent(Control control)
        {
            if (control.AnchorLeft != 0f ||
                control.AnchorTop != 0f ||
                control.AnchorRight != 0f ||
                control.AnchorBottom != 0f ||
                control.OffsetLeft != 0f ||
                control.OffsetTop != 0f ||
                control.OffsetRight != 0f ||
                control.OffsetBottom != 0f ||
                control.Size != Vector2.Zero)
                return;

            ApplyFullRect(control);
        }

        private static TextureRect CreateFullCoverTextureRect(Texture2D texture)
        {
            var rect = new TextureRect
            {
                Name = StableTextureRectNodeName(texture.ResourcePath, "CharacterSelectBg"),
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ClipContents = true,
            };
            ApplyFullRect(rect);
            return rect;
        }

        private static Control CreateEmptyFullRectControl(string name)
        {
            var control = new Control
            {
                Name = name,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            ApplyFullRect(control);
            return control;
        }

        private static void ApplyFullRect(Control control)
        {
            control.AnchorLeft = 0f;
            control.AnchorTop = 0f;
            control.AnchorRight = 1f;
            control.AnchorBottom = 1f;
            control.OffsetLeft = 0f;
            control.OffsetTop = 0f;
            control.OffsetRight = 0f;
            control.OffsetBottom = 0f;
            control.GrowHorizontal = Control.GrowDirection.Both;
            control.GrowVertical = Control.GrowDirection.Both;
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

        private static MethodInfo? ResolveInstantiateControlMethod()
        {
            return typeof(PackedScene)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(static method => method.Name == nameof(PackedScene.Instantiate) &&
                                        method is { IsGenericMethodDefinition: true } &&
                                        method.GetGenericArguments().Length == 1)
                .Where(static method =>
                {
                    var parameters = method.GetParameters();
                    return parameters.Length == 1 &&
                           parameters[0].ParameterType == typeof(PackedScene.GenEditState);
                })
                .Select(static method => method.MakeGenericMethod(typeof(Control)))
                .FirstOrDefault();
        }
    }
}
