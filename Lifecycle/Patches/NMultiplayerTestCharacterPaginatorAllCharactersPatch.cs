using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Debug.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Multiplayer debug test uses a hard-coded array of five vanilla characters. Replace it with
    ///     <see cref="ModelDb.AllCharacters" /> so mod-registered characters appear in the paginator.
    /// </summary>
    public class NMultiplayerTestCharacterPaginatorAllCharactersPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMultiplayerTestCharacterPaginator, CharacterModel[]>
            CharactersRef =
                AccessTools.FieldRefAccess<NMultiplayerTestCharacterPaginator, CharacterModel[]>("_characters");

        private static readonly AccessTools.FieldRef<NPaginator, List<string>> OptionsRef =
            AccessTools.FieldRefAccess<NPaginator, List<string>>("_options");

        private static readonly AccessTools.FieldRef<NPaginator, int> CurrentIndexRef =
            AccessTools.FieldRefAccess<NPaginator, int>("_currentIndex");

        private static readonly AccessTools.FieldRef<NPaginator, MegaLabel> LabelRef =
            AccessTools.FieldRefAccess<NPaginator, MegaLabel>("_label");

        /// <inheritdoc />
        public static string PatchId => "nmultiplayer_test_character_paginator_all_characters";

        /// <inheritdoc />
        public static string Description =>
            "Multiplayer test scene: character paginator lists ModelDb.AllCharacters (vanilla + mod)";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerTestCharacterPaginator), "_Ready")];
        }

        /// <summary>
        ///     Harmony postfix: repopulates paginator options and label from <see cref="ModelDb.AllCharacters" /> after
        ///     <c>_Ready</c>.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Postfix(NMultiplayerTestCharacterPaginator __instance)
        {
            var all = ModelDb.AllCharacters.ToArray();
            if (all.Length == 0)
                return;

            CharactersRef(__instance) = all;

            NPaginator paginator = __instance;
            var options = OptionsRef(paginator);
            options.Clear();
            options.AddRange(all.Select(character => character.Title.GetFormattedText()));

            var idx = Mathf.Clamp(CurrentIndexRef(paginator), 0, options.Count - 1);
            CurrentIndexRef(paginator) = idx;
            LabelRef(paginator).Text = all[idx].Title.GetFormattedText();
        }
    }
}
