using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Visibility.Patches
{
    internal sealed class NRelicInventoryAddVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_inventory_add";
        public static bool IsCritical => false;
        public static string Description => "Skip hidden relics in the local relic inventory";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(NRelicInventory),
                    "Add",
                    [typeof(RelicModel), typeof(bool), typeof(int)]),
            ];
        }

        public static bool Prefix(RelicModel relic, ref int index)
        {
            if (!ModRelicVisibilityRegistry.IsVisible(relic))
                return false;

            if (index >= 0)
                index = ModRelicVisibilityRegistry.GetVisibleIndex(relic, index);

            return true;
        }
    }

    internal sealed class NRelicInventoryRemoveVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_inventory_remove";
        public static bool IsCritical => false;
        public static string Description => "Skip hidden relic removal from the local relic inventory";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicInventory), "Remove", [typeof(RelicModel)])];
        }

        public static bool Prefix(NRelicInventory __instance, RelicModel relic)
        {
            return ModRelicVisibilityRegistry.IsVisible(relic) ||
                   ModRelicVisibilityUi.Contains(__instance, relic);
        }
    }

    internal sealed class NRelicInventoryAnimateVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_inventory_animate";
        public static bool IsCritical => false;
        public static string Description => "Skip local relic animations for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(NRelicInventory),
                    nameof(NRelicInventory.AnimateRelic),
                    [typeof(RelicModel), typeof(Vector2?), typeof(Vector2?)]),
            ];
        }

        public static bool Prefix(RelicModel relic)
        {
            return ModRelicVisibilityRegistry.IsVisible(relic);
        }
    }

    internal sealed class RelicModelHoverTipsVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_hover_tips";
        public static bool IsCritical => false;
        public static string Description => "Suppress hover tips for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicModel), nameof(RelicModel.HoverTips), null, MethodType.Getter)];
        }

        public static bool Prefix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (ModRelicVisibilityRegistry.IsVisible(__instance))
                return true;

            __result = [];
            return false;
        }
    }

    internal sealed class RelicModelHoverTipsExcludingRelicVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_extra_hover_tips";
        public static bool IsCritical => false;
        public static string Description => "Suppress extra hover tips for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(RelicModel),
                    nameof(RelicModel.HoverTipsExcludingRelic),
                    null,
                    MethodType.Getter),
            ];
        }

        public static bool Prefix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (ModRelicVisibilityRegistry.IsVisible(__instance))
                return true;

            __result = [];
            return false;
        }
    }

    internal sealed class NRelicBasicHolderVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_basic_holder";
        public static bool IsCritical => false;
        public static string Description => "Hide basic relic holders for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicBasicHolder), nameof(NRelicBasicHolder.Create))];
        }

        public static void Postfix(RelicModel relic, NRelicBasicHolder? __result)
        {
            if (__result == null || ModRelicVisibilityRegistry.IsVisible(relic))
                return;

            __result.Visible = false;
            __result.MouseFilter = Control.MouseFilterEnum.Ignore;
            __result.FocusMode = Control.FocusModeEnum.None;
            __result.FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Disabled;
        }
    }

    internal sealed class NMultiplayerPlayerStateRelicObtainedVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_remote_obtained_animation";
        public static bool IsCritical => false;
        public static string Description => "Skip remote obtained animations for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerPlayerState), "AnimateRelicObtained", [typeof(RelicModel)])];
        }

        public static bool Prefix(RelicModel relic, ref Task __result)
        {
            if (ModRelicVisibilityRegistry.IsVisible(relic))
                return true;

            __result = Task.CompletedTask;
            return false;
        }
    }

    internal sealed class NMultiplayerPlayerStateRelicRemovedVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_remote_removed_animation";
        public static bool IsCritical => false;
        public static string Description => "Skip remote removed animations for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerPlayerState), "AnimateRelicRemoved", [typeof(RelicModel)])];
        }

        public static bool Prefix(RelicModel relic, ref Task __result)
        {
            if (ModRelicVisibilityRegistry.IsVisible(relic))
                return true;

            __result = Task.CompletedTask;
            return false;
        }
    }

    internal sealed class NMultiplayerPlayerExpandedStateRelicClickVisibilityPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMultiplayerPlayerExpandedState, Control> RelicContainer =
            AccessTools.FieldRefAccess<NMultiplayerPlayerExpandedState, Control>("_relicContainer");

        public static string PatchId => "ritsulib_relic_visibility_expanded_relic_click";
        public static bool IsCritical => false;
        public static string Description => "Filter hidden relics from multiplayer expanded relic inspection";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerPlayerExpandedState), "OnRelicClicked", [typeof(NRelic)])];
        }

        public static bool Prefix(NMultiplayerPlayerExpandedState __instance, NRelic node)
        {
            if (!ModRelicVisibilityRegistry.IsVisible(node.Model))
                return false;

            var relics = (from holder in RelicContainer(__instance).GetChildren().OfType<NRelicBasicHolder>()
                where ModRelicVisibilityRegistry.IsVisible(holder.Relic.Model)
                select holder.Relic.Model).ToList();

            NGame.Instance!.GetInspectRelicScreen().Open(relics, node.Model);
            return false;
        }
    }

    internal sealed class NMultiplayerPlayerExpandedStateReadyVisibilityPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMultiplayerPlayerExpandedState, Control> RelicContainer =
            AccessTools.FieldRefAccess<NMultiplayerPlayerExpandedState, Control>("_relicContainer");

        private static readonly Action<NMultiplayerPlayerExpandedState>? UpdateNavigation =
            AccessTools.Method(typeof(NMultiplayerPlayerExpandedState), "UpdateNavigation")
                ?.CreateDelegate<Action<NMultiplayerPlayerExpandedState>>();

        public static string PatchId => "ritsulib_relic_visibility_expanded_ready";
        public static bool IsCritical => false;
        public static string Description => "Remove hidden relic holders from multiplayer expanded state";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerPlayerExpandedState), nameof(NMultiplayerPlayerExpandedState._Ready))];
        }

        public static void Postfix(NMultiplayerPlayerExpandedState __instance)
        {
            var container = RelicContainer(__instance);
            var removed = false;
            foreach (var holder in container.GetChildren().OfType<NRelicBasicHolder>().ToArray())
            {
                if (ModRelicVisibilityRegistry.IsVisible(holder.Relic.Model))
                    continue;

                container.RemoveChild(holder);
                holder.QueueFree();
                removed = true;
            }

            if (removed)
                UpdateNavigation?.Invoke(__instance);
        }
    }

    internal sealed class HoveredModelTrackerRelicVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_relic_visibility_hovered_model";
        public static bool IsCritical => false;
        public static string Description => "Suppress multiplayer hover sync for hidden relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(HoveredModelTracker), nameof(HoveredModelTracker.OnLocalRelicHovered))];
        }

        public static bool Prefix(RelicModel relicModel)
        {
            return ModRelicVisibilityRegistry.IsVisible(relicModel);
        }
    }
}
