using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Ui.RichTextEffects.Patches
{
    internal sealed class MegaRichTextLabelReadyModRichTextEffectPatch : IPatchMethod
    {
        public static string PatchId => "mega_rich_text_label_ready_mod_rich_text_effects";

        public static bool IsCritical => false;

        public static string Description => "Install registered mod rich text effects into MegaRichTextLabel";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel._Ready))];
        }

        public static void Postfix(MegaRichTextLabel __instance)
        {
            ModRichTextEffectRegistry.InstallInto(__instance);
        }
    }

    internal sealed class MegaRichTextLabelSetTextAutoSizeModRichTextEffectPatch : IPatchMethod
    {
        public static string PatchId => "mega_rich_text_label_set_text_auto_size_mod_rich_text_effects";

        public static bool IsCritical => false;

        public static string Description =>
            "Install registered mod rich text effects after MegaRichTextLabel text updates";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel.SetTextAutoSize), [typeof(string)]),
            ];
        }

        public static void Postfix(MegaRichTextLabel __instance)
        {
            ModRichTextEffectRegistry.InstallInto(__instance);
        }
    }

    internal sealed class MegaRichTextLabelEditorPostSaveModRichTextEffectPatch : IPatchMethod
    {
        public static string PatchId => "mega_rich_text_label_editor_post_save_mod_rich_text_effects";

        public static bool IsCritical => false;

        public static string Description =>
            "Restore registered mod rich text effects after editor scene saves";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel._Notification), [typeof(int)]),
            ];
        }

        public static void Postfix(MegaRichTextLabel __instance, int what)
        {
            if (what == Node.NotificationEditorPostSave)
                ModRichTextEffectRegistry.InstallInto(__instance);
        }
    }
}
