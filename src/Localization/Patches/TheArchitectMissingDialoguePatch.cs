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
    ///     <para xml:lang="en">
    ///         Prevents THE_ARCHITECT's <c>WinRun</c> path from dereferencing a null dialogue when no
    ///         <see cref="AncientDialogue" /> can be resolved for a registered modded character.
    ///     </para>
    ///     <para xml:lang="en">
    ///         The fallback dialogue has an empty <see cref="AncientDialogue.Lines" /> collection, preserving
    ///         the base game's no-dialogue options and UI path while providing an <c>EndAttackers</c> value for <c>WinRun</c>.
    ///         A non-empty uninitialized dialogue would instead leave the room without buttons.
    ///     </para>
    ///     <para xml:lang="en">
    ///         This patch applies only when both the debug-compatibility master switch and the
    ///         Ancient/THE_ARCHITECT compatibility setting are enabled. Otherwise, the base-game behavior, including a
    ///         possible null-reference exception after PROCEED, remains unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">当已注册的模组角色无法解析出 <see cref="AncientDialogue" /> 时，避免 THE_ARCHITECT 的 <c>WinRun</c> 流程解引用空对话。</para>
    ///     <para xml:lang="zh-CN">
    ///         回退对话的 <see cref="AncientDialogue.Lines" /> 集合为空，因此既能保留原版无对话时的选项和界面流程，又能为 <c>WinRun</c> 提供
    ///         <c>EndAttackers</c> 值。若使用包含未初始化台词的非空对话，房间反而会没有可用按钮。
    ///     </para>
    ///     <para xml:lang="zh-CN">此补丁仅在调试兼容总开关和 Ancient/THE_ARCHITECT 兼容设置同时启用时生效；否则保留原版行为，包括选择 PROCEED 后可能发生的空引用异常。</para>
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
        ///     <para xml:lang="en">
        ///         Creates an <see cref="AncientDialogue" /> without invoking its constructor, which requires at
        ///         least one line, then assigns an empty <see cref="AncientDialogue.Lines" /> collection and
        ///         <see cref="ArchitectAttackers.None" /> to its attacker fields.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在不调用构造函数（其要求至少一行台词）的情况下创建 <see cref="AncientDialogue" />，随后将
        ///         <see cref="AncientDialogue.Lines" /> 设为空集合，并将攻击者字段设为 <see cref="ArchitectAttackers.None" />。
        ///     </para>
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
