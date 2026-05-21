using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    internal static class ContentSourceHoverTipPatchHelper
    {
        internal static void Append(string info, ref HoverTip tip) => tip.Description = $"[purple]{info}[/purple]\n{tip.Description}";

        internal static void Append(AbstractModel model, ref IEnumerable<IHoverTip> result)
        {
            if (model is IContentSourceSupplier supplier)
            {
                result = [new HoverTip(ContentSourceHoverTipFactory.GetTitle(), supplier.ContentSource), .. result];
                return;
            }

            if (!ContentSourceHoverTipFactory.TryCreate(model, out var tip))
                return;

            result = [tip, .. result];
        }
    }

    internal sealed class ContentSourceKeywordHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "content_source_keyword_hover_tip";

        public static string Description => "Add content source hover tip to keyword hover tips, if available from ContentSourceHoverTipFactory";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [new(typeof(HoverTipFactory), "FromKeyword")];

        // ReSharper disable once InconsistentNaming
        public static void Postfix(CardKeyword keyword, ref IHoverTip __result)
        // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled())
                return;

            var info = ContentSourceHoverTipFactory.ResolveKeyword(keyword);
            if (info.Id == "Vanilla")
                return;

            if (__result is HoverTip tip)
            {
                ContentSourceHoverTipPatchHelper.Append(info.Format(), ref tip);
                __result = tip;
            }
        }
    }

    internal sealed class ContentSourceModelHoverTipPatch : IPatchMethod
    {
        public static string PatchId => "content_source_model_hover_tip";

        public static string Description => "Add content source hover tip to all models that have a source registered in ContentSourceHoverTipFactory";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [
                new(typeof(PotionModel), "HoverTip", MethodType.Getter),
                new(typeof(PowerModel), "DumbHoverTip", MethodType.Getter),
                new(typeof(RelicModel), "HoverTip", MethodType.Getter),
                new(typeof(OrbModel), "DumbHoverTip", MethodType.Getter),
                new(typeof(EnchantmentModel), "HoverTip", MethodType.Getter),
                new(typeof(AfflictionModel), "HoverTip", MethodType.Getter),
            ];

        // ReSharper disable once InconsistentNaming
        public static void Postfix(AbstractModel __instance, ref HoverTip __result)
        // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled())
                return;

            if (__instance is IContentSourceSupplier supplier)
            {
                ContentSourceHoverTipPatchHelper.Append(supplier.ContentSource, ref __result);
                return;
            }

            var info = ContentSourceHoverTipFactory.Resolve(__instance.GetType());
            if (info.Id == "Vanilla")
                return;

            ContentSourceHoverTipPatchHelper.Append(info.Format(), ref __result);
        }
    }

    internal sealed class ContentSourceNHoverTipSetShowPatch : IPatchMethod
    {
        public static string PatchId => "nhover_tip_set_inspect_screen_show_source";

        public static string Description =>
            "Show content source in NInspectCardScreen and NInspectRelicScreen, if available from ContentSourceHoverTipFactory";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHoverTipSet), "CreateAndShow", [typeof(Control), typeof(IEnumerable<IHoverTip>), typeof(HoverTipAlignment)])];
        }

        private static readonly FieldInfo? CardsField = AccessTools.Field(typeof(NInspectCardScreen), "_cards");
        private static readonly FieldInfo? CardsIndexField = AccessTools.Field(typeof(NInspectCardScreen), "_index");
        private static readonly FieldInfo? RelicsField = AccessTools.Field(typeof(NInspectRelicScreen), "_relics");
        private static readonly FieldInfo? RelicsIndexField = AccessTools.Field(typeof(NInspectRelicScreen), "_index");

        private static void AppendTip<T>(FieldInfo? listField, FieldInfo? indexField, Control screen, ref IEnumerable<IHoverTip> hoverTips) where T : AbstractModel
        {
            if (listField == null || indexField == null)
                return;

            var index = (int)indexField.GetValue(screen)!;
            if (listField.GetValue(screen) is not IList<T> list || index < 0 || index >= list.Count)
                return;

            if (list[index] is not AbstractModel model)
                return;

            ContentSourceHoverTipPatchHelper.Append(model, ref hoverTips);
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(Control owner, ref IEnumerable<IHoverTip> hoverTips)
        // ReSharper restore once InconsistentNaming
        {
            if (!RitsuLibSettingsStore.IsModSourceHoverTipsEnabled())
                return;

            if (owner is NInspectCardScreen cardScreen)
            {
                AppendTip<CardModel>(CardsField, CardsIndexField, cardScreen, ref hoverTips);
            }

            if (owner is NInspectRelicScreen relicScreen)
            {
                AppendTip<RelicModel>(RelicsField, RelicsIndexField, relicScreen, ref hoverTips);
            }
        }
    }
}