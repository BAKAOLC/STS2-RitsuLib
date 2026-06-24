using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
    /// <summary>
    ///     When THE_ARCHITECT resolves no <see cref="AncientDialogue" />, vanilla would leave <c>Dialogue</c> null and
    ///     still show PROCEED — but <c>WinRun</c> dereferences <c>Dialogue</c>. A stub with <b>non-empty</b>
    ///     <see cref="AncientDialogue.Lines" /> is unsafe: <c>OnRoomEnter</c> calls <c>ClearCurrentOptions</c> and
    ///     <c>PlayCurrentLine</c> then exits early when <c>LineText</c> was never populated, leaving no buttons. This
    ///     patch injects an <see cref="AncientDialogue" /> with <b>empty</b> lines so vanilla follows the same path as
    ///     <c>Dialogue == null</c> for options/UI while <c>WinRun</c> can read <c>EndAttackers</c>.
    ///     <para />
    ///     Scope: only when debug compatibility <b>master</b> and the <b>Ancient / THE_ARCHITECT</b> sub-setting are
    ///     enabled, and the character type is registered through <see cref="ModContentRegistry" />. Otherwise vanilla
    ///     behavior (possible NRE on PROCEED) applies.
    ///     当 THE_ARCHITECT 未解析出任何 <see cref="AncientDialogue" /> 时，原版会让 <c>Dialogue</c> 保持 null，
    ///     但仍显示 PROCEED；然而 <c>WinRun</c> 会解引用 <c>Dialogue</c>。带 <b>non-empty</b>
    ///     <see cref="AncientDialogue.Lines" /> 的 stub 并不安全：<c>OnRoomEnter</c> 会调用 <c>ClearCurrentOptions</c> 和
    ///     <c>PlayCurrentLine</c>，随后因 <c>LineText</c> 从未填充而提前退出，导致没有按钮。此
    ///     patch 会注入一个 lines 为 <b>empty</b> 的 <see cref="AncientDialogue" />，使原版在 options/UI 上走与
    ///     <c>Dialogue == null</c> 相同的路径，同时 <c>WinRun</c> 可读取 <c>EndAttackers</c>。
    ///     <para />
    ///     作用域：仅当 debug compatibility <b>master</b> 和 <b>Ancient / THE_ARCHITECT</b> 子设置
    ///     启用，且角色类型通过 <see cref="ModContentRegistry" /> 注册时生效。否则使用原版
    ///     行为（PROCEED 上可能发生 NRE）。
    /// </summary>
    internal class TheArchitectLoadDialogueMissingFallbackPatch : IPatchMethod
    {
        public static string PatchId => "the_architect_load_dialogue_missing_fallback";

        public static string Description =>
            "THE_ARCHITECT: requires debug compat master + ancient shim; registry characters only; LoadDialogue null fallback";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(TheArchitect), "LoadDialogue", []),
            ];
        }

        public static void Postfix(TheArchitect __instance)
        {
            var dialogueField = AccessTools.Field(typeof(TheArchitect), "_dialogue");
            if (dialogueField == null || dialogueField.GetValue(__instance) != null)
                return;

            var character = __instance.Owner?.Character;
            if (character == null)
                return;

            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return;

            if (!RitsuLibSettingsStore.IsAncientArchitectCompatEnabled())
                return;

            var characterEntry = character.Id.Entry;
            AncientDialogueMissingWarnings.WarnOnce(
                $"the_architect_dialogue_missing:{characterEntry}",
                "[Ancient] THE_ARCHITECT has no valid dialogue for character '" + characterEntry +
                "'. Continuing without lines; add ancients keys under THE_ARCHITECT.talk." + characterEntry +
                ".0-0.ancient / .char (see RitsuLib Localization & Keywords).");

            var stub = TryCreateEmptyLinesArchitectDialogueStub();
            if (stub == null)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    "[Ancient] THE_ARCHITECT fallback dialogue could not be constructed (reflection); WinRun may still fail.");
                return;
            }

            dialogueField.SetValue(__instance, stub);
        }

        /// <summary>
        ///     Builds an <see cref="AncientDialogue" /> without running its constructor (which requires ≥1 line), with
        ///     <see cref="AncientDialogue.Lines" /> empty and attackers set to <see cref="ArchitectAttackers.None" />.
        ///     构建一个不运行其构造函数（该构造函数要求 >=1 行）的 <see cref="AncientDialogue" />，其中
        ///     <see cref="AncientDialogue.Lines" /> 为空，attackers 设置为 <see cref="ArchitectAttackers.None" />。
        /// </summary>
        private static AncientDialogue? TryCreateEmptyLinesArchitectDialogueStub()
        {
            var t = typeof(AncientDialogue);
            var stub = (AncientDialogue)RuntimeHelpers.GetUninitializedObject(t);

            var linesField = FindLinesBackingField(t);
            if (linesField == null)
                return null;

            linesField.SetValue(stub, Array.Empty<AncientDialogueLine>());

            foreach (var fi in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (fi.FieldType == typeof(ArchitectAttackers))
                    fi.SetValue(stub, ArchitectAttackers.None);

            return stub;
        }

        private static FieldInfo? FindLinesBackingField(Type ancientDialogueType)
        {
            foreach (var fi in ancientDialogueType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (fi.FieldType == typeof(IReadOnlyList<AncientDialogueLine>))
                    return fi;

            return ancientDialogueType.GetField(
                "<Lines>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
