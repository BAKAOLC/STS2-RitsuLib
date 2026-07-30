using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.CardTags.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds the registered tag IDs declared by a <see cref="ModCardTemplate" /> to its materialized tag
    ///         set.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="ModCardTemplate" /> 声明的已注册标签 ID 添加到其实例化标签集合。
    ///     </para>
    /// </summary>
    internal sealed class CardModelTagsModSeedPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<CardModel, object> SeededCards = new();
        private static readonly object SeededMarker = new();
        public static string PatchId => "ritsulib_card_model_tags_mod_seed";

        public static string Description =>
            "Seed ModCardTemplate.RegisteredCardTagIds into CardModel.Tags after the canonical set is built";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "Tags", MethodType.Getter)];
        }

        public static void Postfix(CardModel __instance, IEnumerable<CardTag> __result)
        {
            if (__instance is not ModCardTemplate template)
                return;

            if (SeededCards.TryGetValue(__instance, out _))
                return;

            if (__result is not HashSet<CardTag> storage)
            {
                SeededCards.Add(__instance, SeededMarker);
                return;
            }

            foreach (var id in template.EnumerateRegisteredCardTagIds())
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (ModCardTagRegistry.TryResolveCardTag(id, out var value))
                    storage.Add(value);
            }

            SeededCards.Add(__instance, SeededMarker);
        }
    }
}
