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
        [ThreadStatic] private static Stack<SelectionScope>? _scopeStack;

        public static void Enter(MethodBase originalMethod)
        {
            var scope = ResolveScope(originalMethod);
            if (scope == SelectionScope.None)
                return;

            (_scopeStack ??= new()).Push(scope);
        }

        public static void Exit(MethodBase originalMethod)
        {
            var scope = ResolveScope(originalMethod);
            if (scope == SelectionScope.None || _scopeStack is not { Count: > 0 })
                return;

            _scopeStack.Pop();
            if (_scopeStack.Count == 0)
                _scopeStack = null;
        }

        public static IEnumerable<CharacterModel> Apply(IEnumerable<CharacterModel> source)
        {
            return (_scopeStack is { Count: > 0 } ? _scopeStack.Peek() : SelectionScope.None) switch
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
    ///     <para xml:lang="en">
    ///         Maintains the selection-policy scope used by the base game's character visibility and random-selection
    ///         flows.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为游戏本体的角色可见性和随机选择流程维护选择策略作用域。
    ///     </para>
    /// </summary>
    internal class CharacterVanillaSelectionPolicyPatches : IPatchMethod
    {
        public static string PatchId => "character_vanilla_selection_policy";

        public static string Description =>
            "Apply mod character vanilla selection policy to vanilla character-select visibility and random roll";

        public static bool IsCritical => false;

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

        public static void Prefix(MethodBase __originalMethod)
        {
            CharacterVanillaSelectionPolicyScope.Enter(__originalMethod);
        }

        public static void Finalizer(MethodBase __originalMethod)
        {
            CharacterVanillaSelectionPolicyScope.Exit(__originalMethod);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies the active selection-policy scope to <see cref="ModelDb.AllCharacters" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将当前选择策略作用域应用到 <see cref="ModelDb.AllCharacters" />。
    ///     </para>
    /// </summary>
    internal class CharacterVanillaSelectionPolicyAllCharactersPatch : IPatchMethod
    {
        public static string PatchId => "character_vanilla_selection_policy_all_characters";
        public static string Description => "Filter ModelDb.AllCharacters by current vanilla selection scope";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Filters the getter result according to the active selection-policy scope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据当前选择策略作用域过滤属性返回结果。
        ///     </para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId, Const.FrameworkContentRegistryHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            __result = CharacterVanillaSelectionPolicyScope.Apply(__result);
        }
    }
}
