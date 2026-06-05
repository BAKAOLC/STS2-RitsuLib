using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal static class CharacterVanillaSelectionPolicyScope
    {
        [ThreadStatic] private static SelectionScope _currentScope;
        [ThreadStatic] private static int _scopeDepth;

        public static void Enter(MethodBase originalMethod)
        {
            var scope = ResolveScope(originalMethod);
            if (scope == SelectionScope.None)
                return;

            if (_scopeDepth++ == 0)
                _currentScope = scope;
        }

        public static void Exit(MethodBase originalMethod)
        {
            var scope = ResolveScope(originalMethod);
            if (scope == SelectionScope.None || _scopeDepth <= 0)
                return;

            _scopeDepth--;
            if (_scopeDepth == 0)
                _currentScope = SelectionScope.None;
        }

        public static IEnumerable<CharacterModel> Apply(IEnumerable<CharacterModel> source)
        {
            return _currentScope switch
            {
                SelectionScope.Visible => source.Where(character => character is not IModCharacterVanillaSelectionPolicy
                {
                    HideFromVanillaCharacterSelect: true,
                }),
                SelectionScope.RandomEligible => source.Where(character =>
                    character is not IModCharacterVanillaSelectionPolicy
                    {
                        AllowInVanillaRandomCharacterSelect: false,
                    }),
                _ => source,
            };
        }

        private static SelectionScope ResolveScope(MethodBase originalMethod)
        {
            if (originalMethod.DeclaringType == typeof(NCharacterSelectScreen) &&
                originalMethod.Name == nameof(NCharacterSelectScreen.InitCharacterButtons))
                return SelectionScope.Visible;

            if ((originalMethod.DeclaringType == typeof(NCharacterSelectScreen) &&
                 originalMethod.Name == nameof(NCharacterSelectScreen.UpdateRandomCharacterVisibility)) ||
                (originalMethod.DeclaringType == typeof(NCharacterSelectButton) &&
                 originalMethod.Name == nameof(NCharacterSelectButton.Init)) ||
                (originalMethod.DeclaringType == typeof(StartRunLobby) &&
                 originalMethod.Name == "BeginRunLocally"))
                return SelectionScope.RandomEligible;

            return SelectionScope.None;
        }

        private enum SelectionScope
        {
            None,
            Visible,
            RandomEligible,
        }
    }

    /// <summary>
    ///     Maintains selection-policy scope for vanilla character-select flows.
    ///     为原版角色选择流程维护选择策略作用域。
    /// </summary>
    public class CharacterVanillaSelectionPolicyPatches : IPatchMethod
    {
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
                new(typeof(StartRunLobby), "BeginRunLocally", true),
            ];
        }

        /// <summary>
        ///     Enters selection scope for character-list consumers.
        ///     为角色列表消费者进入选择作用域。
        /// </summary>
        public static void Prefix(MethodBase __originalMethod)
        {
            CharacterVanillaSelectionPolicyScope.Enter(__originalMethod);
        }

        /// <summary>
        ///     Ensures scope cleanup even when target method throws.
        ///     即使目标方法抛出，也确保作用域清理。
        /// </summary>
        public static void Finalizer(MethodBase __originalMethod)
        {
            CharacterVanillaSelectionPolicyScope.Exit(__originalMethod);
        }
    }

    /// <summary>
    ///     Applies scoped selection policy to <see cref="ModelDb.AllCharacters" />.
    ///     将带作用域的选择策略应用到 <see cref="ModelDb.AllCharacters" />。
    /// </summary>
    public class CharacterVanillaSelectionPolicyAllCharactersPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_vanilla_selection_policy_all_characters";

        /// <inheritdoc />
        public static string Description => "Filter ModelDb.AllCharacters by current vanilla selection scope";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)];
        }

        /// <summary>
        ///     Filters getter result according to current selection scope.
        ///     根据当前选择作用域过滤 getter 结果。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId, Const.FrameworkContentRegistryHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            __result = CharacterVanillaSelectionPolicyScope.Apply(__result);
        }
    }
}
