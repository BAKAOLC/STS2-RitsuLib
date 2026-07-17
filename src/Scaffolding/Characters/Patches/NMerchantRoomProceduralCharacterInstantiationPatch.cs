using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Replaces only vanilla merchant-character instantiation so RitsuLib procedural or resource overrides can
    ///     supply the node while vanilla keeps ownership of room setup, layout, and ready-time animation startup.
    ///     仅替换原版商人角色实例化，使 RitsuLib 的程序化或资源覆盖可以提供节点，同时由原版继续负责房间初始化、布局和 ready
    ///     阶段的动画启动。
    /// </summary>
    internal class NMerchantRoomProceduralCharacterInstantiationPatch : IPatchMethod
    {
        private static readonly MethodInfo? InstantiateMerchantCharacterMethod = ResolveInstantiateMerchantMethod();

        private static readonly MethodInfo? PreloadManagerCacheGetter =
            AccessTools.DeclaredPropertyGetter(typeof(PreloadManager), nameof(PreloadManager.Cache));

        private static readonly MethodInfo? GetSceneMethod =
            AccessTools.DeclaredMethod(typeof(AssetCache), nameof(AssetCache.GetScene), [typeof(string)]);

        private static readonly MethodInfo? PlayerCharacterGetter =
            AccessTools.DeclaredPropertyGetter(typeof(Player), nameof(Player.Character));

        private static readonly FieldInfo? PlayersField =
            AccessTools.DeclaredField(typeof(NMerchantRoom), "_players");

        private static readonly MethodInfo? PlayerListItemGetter =
            AccessTools.DeclaredPropertyGetter(typeof(List<Player>), "Item");

        private static readonly MethodInfo? MerchantAnimPathGetter =
            AccessTools.DeclaredPropertyGetter(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath));

        private static readonly MethodInfo CreateMerchantCharacterMethod =
            AccessTools.DeclaredMethod(typeof(NMerchantRoomProceduralCharacterInstantiationPatch),
                nameof(CreateMerchantCharacter));

        public static string PatchId => "n_merchant_room_procedural_character_instantiation";

        public static string Description =>
            "Instantiate only RitsuLib-owned merchant character overrides while preserving vanilla room setup";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMerchantRoom), "AfterRoomIsLoaded")];
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const string operation = "[WorldVisuals] Merchant character runtime factory";
            var rewriter = HarmonyIlRewriter.From(instructions);
            if (PreloadManagerCacheGetter == null ||
                GetSceneMethod == null ||
                PlayerCharacterGetter == null ||
                PlayersField == null ||
                PlayerListItemGetter == null ||
                MerchantAnimPathGetter == null ||
                InstantiateMerchantCharacterMethod == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{operation} could not resolve required reflection handles; patch skipped.");
                return rewriter.InstructionsChecked(operation);
            }

            var pattern = HarmonyIlPattern.Sequence(HarmonyIl.IsCall(PreloadManagerCacheGetter), HarmonyIl.IsLdarg(),
                HarmonyIl.IsLdfld(PlayersField), HarmonyIl.IsLdloc(), HarmonyIl.IsCall(PlayerListItemGetter),
                HarmonyIl.IsCall(PlayerCharacterGetter), HarmonyIl.IsCall(MerchantAnimPathGetter),
                HarmonyIl.IsCall(GetSceneMethod), HarmonyIl.IsLdcI4((int)PackedScene.GenEditState.Disabled),
                HarmonyIl.Is(OpCodes.Conv_I8), HarmonyIl.IsCall(InstantiateMerchantCharacterMethod));
            var matches = pattern.FindAll(rewriter.Code);
            if (matches.Count != 1)
            {
                if (!rewriter.Contains(instruction => HarmonyIl.IsCallTo(instruction, CreateMerchantCharacterMethod)))
                    RitsuLibFramework.Logger.Warn(
                        $"{operation} expected one merchant scene instantiation, found {matches.Count}; patch skipped.");

                return rewriter.InstructionsChecked(operation);
            }

            var match = matches[0];
            rewriter.Replace(match,
            [
                rewriter.Code[match.Index + 1].Clone(),
                rewriter.Code[match.Index + 2].Clone(),
                rewriter.Code[match.Index + 3].Clone(),
                rewriter.Code[match.Index + 4].Clone(),
                rewriter.Code[match.Index + 5].Clone(),
                HarmonyIl.Call(CreateMerchantCharacterMethod),
            ]);
            return rewriter.InstructionsChecked(operation);
        }

        internal static bool HasRitsuMerchantVisualOverride(CharacterModel character)
        {
            if (character is IModCharacterAssetOverrides { WorldProceduralVisuals.Merchant: not null })
                return true;

            return CharacterAssetOverridePatchHelper.TryResolveOverridePath(
                character,
                static overrides => overrides.CustomMerchantAnimPath,
                nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath),
                out _);
        }

        internal static NMerchantCharacter CreateMerchantCharacter(CharacterModel character)
        {
            var created = ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(character);
            if (created == null && CharacterAssetOverridePatchHelper.TryResolveOverridePath(
                    character,
                    static overrides => overrides.CustomMerchantAnimPath,
                    nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath),
                    out var overridePath))
                created = CharacterWorldScenePathFactoryHelper.CreateFromSceneOrTexture<NMerchantCharacter>(
                    character,
                    overridePath,
                    nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath),
                    PackedScene.GenEditState.Disabled);

            if (created == null)
                return PreloadManager.Cache.GetScene(character.MerchantAnimPath)
                    .Instantiate<NMerchantCharacter>();

            ModMerchantCharacterVisualPlaybackPatch.RegisterRitsuMerchantVisual(created, character);
            return created;
        }

        private static MethodInfo? ResolveInstantiateMerchantMethod()
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
                .Select(static method => method.MakeGenericMethod(typeof(NMerchantCharacter)))
                .FirstOrDefault();
        }
    }
}
