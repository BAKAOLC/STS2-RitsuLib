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
    /// </summary>
    public class TheArchitectLoadDialogueMissingFallbackPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "the_architect_load_dialogue_missing_fallback";

        /// <inheritdoc />
        public static string Description =>
            "THE_ARCHITECT: requires debug compat master + ancient shim; registry characters only; LoadDialogue null fallback";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(TheArchitect), "LoadDialogue", []),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     After vanilla <c>LoadDialogue</c>, assign a no-op dialogue when none matched so PROCEED / <c>WinRun</c> is safe.
        /// </summary>
        public static void Postfix(TheArchitect __instance)
            // ReSharper restore InconsistentNaming
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
                RitsuLibFramework.Logger.Error(
                    "[Ancient] THE_ARCHITECT fallback dialogue could not be constructed (reflection); WinRun may still fail.");
                return;
            }

            dialogueField.SetValue(__instance, stub);
        }

        /// <summary>
        ///     Builds an <see cref="AncientDialogue" /> without running its constructor (which requires ≥1 line), with
        ///     <see cref="AncientDialogue.Lines" /> empty and attackers set to <see cref="ArchitectAttackers.None" />.
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
