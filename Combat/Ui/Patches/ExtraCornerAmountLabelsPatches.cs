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
    public sealed class NPowerExtraCornerAmountLabelsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "npower_extra_corner_amount_labels";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description =>
            "Render extra IPowerExtraIconAmountLabelsProvider badges on NPower (independent anchors per slot)";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPower), "RefreshAmount")];
        }

        /// <summary>
        ///     Harmony postfix for <c>NPower.RefreshAmount</c>.
        ///     <c>NPower.RefreshAmount</c> 的 Harmony 后置补丁。
        /// </summary>
        public static void Postfix(NPower __instance)
        {
            ExtraCornerAmountLabelsRuntime.SyncPower(__instance);
        }
    }

    /// <summary>
    ///     Clears extra corner labels when the combat power node exits the tree.
    ///     当战斗能力节点退出树时清除额外角落标签。
    /// </summary>
    public sealed class NPowerExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "npower_extra_corner_amount_labels_exit_tree";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description => "Dispose Ritsu extra amount dock when NPower exits tree";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPower), "_ExitTree")];
        }

        /// <summary>
        ///     Harmony postfix for <c>NPower._ExitTree</c>.
        ///     <c>NPower._ExitTree</c> 的 Harmony 后置补丁。
        /// </summary>
        public static void Postfix(NPower __instance)
        {
            ExtraCornerAmountLabelsRuntime.ClearPower(__instance);
        }
    }

    /// <summary>
    ///     Keeps extra corner labels in sync with <see cref="NRelicInventoryHolder" />.
    ///     使额外角落标签与 <see cref="NRelicInventoryHolder" /> 保持同步。
    /// </summary>
    public sealed class NRelicInventoryHolderExtraCornerAmountLabelsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "nrelic_inventory_holder_extra_corner_amount_labels";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description =>
            "Render extra IRelicExtraIconAmountLabelsProvider badges on NRelicInventoryHolder (per-slot anchors)";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicInventoryHolder), "RefreshAmount")];
        }

        /// <summary>
        ///     Harmony postfix for <c>NRelicInventoryHolder.RefreshAmount</c>.
        ///     <c>NRelicInventoryHolder.RefreshAmount</c> 的 Harmony 后置补丁。
        /// </summary>
        public static void Postfix(NRelicInventoryHolder __instance)
        {
            ExtraCornerAmountLabelsRuntime.SyncRelic(__instance);
        }
    }

    /// <summary>
    ///     Clears relic extra labels when the holder exits the tree.
    ///     当持有者退出树时清除遗物额外标签。
    /// </summary>
    public sealed class NRelicInventoryHolderExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "nrelic_inventory_holder_extra_corner_amount_labels_exit_tree";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description => "Dispose Ritsu extra amount dock when relic holder exits tree";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicInventoryHolder), "_ExitTree")];
        }

        /// <summary>
        ///     Harmony postfix for <c>NRelicInventoryHolder._ExitTree</c>.
        ///     <c>NRelicInventoryHolder._ExitTree</c> 的 Harmony 后置补丁。
        /// </summary>
        public static void Postfix(NRelicInventoryHolder __instance)
        {
            ExtraCornerAmountLabelsRuntime.ClearRelic(__instance);
        }
    }

    /// <summary>
    ///     Keeps extra corner labels in sync with <see cref="NIntent" />.
    ///     使额外角落标签与 <see cref="NIntent" /> 保持同步。
    /// </summary>
    public sealed class NIntentExtraCornerAmountLabelsPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "nintent_extra_corner_amount_labels";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description =>
            "Render extra IIntentExtraCornerAmountLabelsProvider badges on NIntent (per-slot anchors)";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NIntent), "UpdateVisuals")];
        }

        /// <summary>
        ///     Harmony postfix for <c>NIntent.UpdateVisuals</c>.
        ///     <c>NIntent.UpdateVisuals</c> 的 Harmony 后置补丁。
        /// </summary>
        public static void Postfix(NIntent __instance)
        {
            ExtraCornerAmountLabelsRuntime.SyncIntent(__instance);
        }
    }

    /// <summary>
    ///     Clears intent extra labels when the intent node exits the tree.
    ///     当意图节点退出树时清除意图额外标签。
    /// </summary>
    public sealed class NIntentExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "nintent_extra_corner_amount_labels_exit_tree";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description => "Dispose Ritsu extra intent dock when NIntent exits tree";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NIntent), "_ExitTree")];
        }

        /// <summary>
        ///     Harmony postfix for <c>NIntent._ExitTree</c>.
        ///     <c>NIntent._ExitTree</c> 的 Harmony 后置补丁。
        /// </summary>
        public static void Postfix(NIntent __instance)
        {
            ExtraCornerAmountLabelsRuntime.ClearIntent(__instance);
        }
    }
}
