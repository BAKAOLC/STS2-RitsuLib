using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Ui.Patches
{
    /// <summary>
    ///     Keeps extra corner amount labels in sync with <see cref="NPower" />.
    ///     使额外角落数量标签与 <see cref="NPower" /> 保持同步。
    /// </summary>
    internal sealed class NPowerExtraCornerAmountLabelsPatch : IPatchMethod
    {
        public static string PatchId => "npower_extra_corner_amount_labels";
        public static bool IsCritical => false;

        public static string Description =>
            "Render extra IPowerExtraIconAmountLabelsProvider badges on NPower (independent anchors per slot)";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPower), "RefreshAmount")];
        }

        public static void Postfix(NPower __instance)
        {
            ExtraCornerAmountLabelsRuntime.SyncPower(__instance);
        }
    }

    /// <summary>
    ///     Clears extra corner labels when the combat power node exits the tree.
    ///     当战斗能力节点退出树时清除额外角落标签。
    /// </summary>
    internal sealed class NPowerExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        public static string PatchId => "npower_extra_corner_amount_labels_exit_tree";
        public static bool IsCritical => false;
        public static string Description => "Dispose Ritsu extra amount dock when NPower exits tree";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPower), "_ExitTree")];
        }

        public static void Postfix(NPower __instance)
        {
            ExtraCornerAmountLabelsRuntime.ClearPower(__instance);
        }
    }

    /// <summary>
    ///     Keeps extra corner labels in sync with <see cref="NRelicInventoryHolder" />.
    ///     使额外角落标签与 <see cref="NRelicInventoryHolder" /> 保持同步。
    /// </summary>
    internal sealed class NRelicInventoryHolderExtraCornerAmountLabelsPatch : IPatchMethod
    {
        public static string PatchId => "nrelic_inventory_holder_extra_corner_amount_labels";
        public static bool IsCritical => false;

        public static string Description =>
            "Render extra IRelicExtraIconAmountLabelsProvider badges on NRelicInventoryHolder (per-slot anchors)";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicInventoryHolder), "RefreshAmount")];
        }

        public static void Postfix(NRelicInventoryHolder __instance)
        {
            ExtraCornerAmountLabelsRuntime.SyncRelic(__instance);
        }
    }

    /// <summary>
    ///     Clears relic extra labels when the holder exits the tree.
    ///     当持有者退出树时清除遗物额外标签。
    /// </summary>
    internal sealed class NRelicInventoryHolderExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        public static string PatchId => "nrelic_inventory_holder_extra_corner_amount_labels_exit_tree";
        public static bool IsCritical => false;
        public static string Description => "Dispose Ritsu extra amount dock when relic holder exits tree";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicInventoryHolder), "_ExitTree")];
        }

        public static void Postfix(NRelicInventoryHolder __instance)
        {
            ExtraCornerAmountLabelsRuntime.ClearRelic(__instance);
        }
    }

    /// <summary>
    ///     Keeps extra corner labels in sync with <see cref="NIntent" />.
    ///     使额外角落标签与 <see cref="NIntent" /> 保持同步。
    /// </summary>
    internal sealed class NIntentExtraCornerAmountLabelsPatch : IPatchMethod
    {
        public static string PatchId => "nintent_extra_corner_amount_labels";
        public static bool IsCritical => false;

        public static string Description =>
            "Render extra IIntentExtraCornerAmountLabelsProvider badges on NIntent (per-slot anchors)";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NIntent), "UpdateVisuals")];
        }

        public static void Postfix(NIntent __instance)
        {
            ExtraCornerAmountLabelsRuntime.SyncIntent(__instance);
        }
    }

    /// <summary>
    ///     Clears intent extra labels when the intent node exits the tree.
    ///     当意图节点退出树时清除意图额外标签。
    /// </summary>
    internal sealed class NIntentExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        public static string PatchId => "nintent_extra_corner_amount_labels_exit_tree";
        public static bool IsCritical => false;
        public static string Description => "Dispose Ritsu extra intent dock when NIntent exits tree";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NIntent), "_ExitTree")];
        }

        public static void Postfix(NIntent __instance)
        {
            ExtraCornerAmountLabelsRuntime.ClearIntent(__instance);
        }
    }
}
