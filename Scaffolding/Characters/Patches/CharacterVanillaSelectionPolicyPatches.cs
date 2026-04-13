using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Applies <see cref="IModCharacterVanillaSelectionPolicy" /> when vanilla character-select builds visible and
    ///     random-eligible character lists.
    /// </summary>
    public class CharacterVanillaSelectionPolicyPatches : IPatchMethod
    {
        private static readonly MethodInfo? ModelDbAllCharactersGetter =
            AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));

        private static readonly MethodInfo? VisibleCharactersMethod =
            AccessTools.DeclaredMethod(typeof(CharacterVanillaSelectionPolicyPatches), nameof(GetVisibleCharacters));

        private static readonly MethodInfo? RandomEligibleCharactersMethod =
            AccessTools.DeclaredMethod(typeof(CharacterVanillaSelectionPolicyPatches),
                nameof(GetRandomEligibleCharacters));

        /// <inheritdoc />
        public static string PatchId => "character_vanilla_selection_policy";

        /// <inheritdoc />
        public static string Description =>
            "Apply mod character vanilla selection policy to vanilla character-select visibility and random roll";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitCharacterButtons)),
                new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.UpdateRandomCharacterVisibility)),
                new(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init), true),
                new(typeof(NCharacterSelectScreen), "RollRandomCharacter", true),
                new(typeof(StartRunLobby), "BeginRunLocally", true),
            ];
        }

        /// <summary>
        ///     Rewrites direct reads of <see cref="ModelDb.AllCharacters" /> in target methods.
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            if (ModelDbAllCharactersGetter == null)
                return instructions;

            var useVisibleList = __originalMethod.Name == nameof(NCharacterSelectScreen.InitCharacterButtons);
            var replacement = useVisibleList ? VisibleCharactersMethod : RandomEligibleCharactersMethod;
            if (replacement == null)
                return instructions;

            var rewritten = false;
            var list = instructions.ToList();
            for (var index = 0; index < list.Count; index++)
            {
                var code = list[index];
                if (!code.Calls(ModelDbAllCharactersGetter))
                    continue;

                code.opcode = OpCodes.Call;
                code.operand = replacement;
                list[index] = code;
                rewritten = true;
            }

            if (!rewritten)
                RitsuLibFramework.Logger.Debug(
                    $"[CharacterSelection] No ModelDb.AllCharacters call found while patching {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}.");

            return list;
        }

        private static IEnumerable<CharacterModel> GetVisibleCharacters()
        {
            foreach (var character in ModelDb.AllCharacters)
            {
                if (character is IModCharacterVanillaSelectionPolicy { HideFromVanillaCharacterSelect: true })
                    continue;

                yield return character;
            }
        }

        private static IEnumerable<CharacterModel> GetRandomEligibleCharacters()
        {
            foreach (var character in ModelDb.AllCharacters)
            {
                if (character is IModCharacterVanillaSelectionPolicy
                    {
                        AllowInVanillaRandomCharacterSelect: false,
                    })
                    continue;

                yield return character;
            }
        }
    }
}
