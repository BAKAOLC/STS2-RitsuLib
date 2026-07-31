using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Ui.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Refreshes extra badges after <see cref="NPower" /> refreshes its amount.</para>
    ///     <para xml:lang="zh-CN">在 <see cref="NPower" /> 刷新数量后同步额外角标。</para>
    /// </summary>
    internal sealed class NPowerExtraCornerAmountLabelsPatch : IPatchMethod
    {
        public static string PatchId => "npower_extra_corner_amount_labels";
        public static bool IsCritical => false;

        public static string Description =>
            "Render extra power badges on NPower with independent per-entry anchors";

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
    ///     <para xml:lang="en">Releases extra power badges and subscriptions when the node exits the scene tree.</para>
    ///     <para xml:lang="zh-CN">能力节点退出场景树时，释放额外角标并取消事件订阅。</para>
    /// </summary>
    internal sealed class NPowerExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        public static string PatchId => "npower_extra_corner_amount_labels_exit_tree";
        public static bool IsCritical => false;
        public static string Description => "Release extra power badges when NPower exits the scene tree";

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
    ///     <para xml:lang="en">
    ///         Refreshes extra badges after <see cref="NRelicInventoryHolder" /> refreshes its amount.
    ///     </para>
    ///     <para xml:lang="zh-CN">在 <see cref="NRelicInventoryHolder" /> 刷新数量后同步额外角标。</para>
    /// </summary>
    internal sealed class NRelicInventoryHolderExtraCornerAmountLabelsPatch : IPatchMethod
    {
        public static string PatchId => "nrelic_inventory_holder_extra_corner_amount_labels";
        public static bool IsCritical => false;

        public static string Description =>
            "Render extra relic badges on NRelicInventoryHolder with independent per-entry anchors";

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
    ///     <para xml:lang="en">Releases extra relic badges and subscriptions when the holder exits the scene tree.</para>
    ///     <para xml:lang="zh-CN">遗物容器退出场景树时，释放额外角标并取消事件订阅。</para>
    /// </summary>
    internal sealed class NRelicInventoryHolderExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        public static string PatchId => "nrelic_inventory_holder_extra_corner_amount_labels_exit_tree";
        public static bool IsCritical => false;
        public static string Description => "Release extra relic badges when the holder exits the scene tree";

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
    ///     <para xml:lang="en">Refreshes extra badges after <see cref="NIntent" /> updates its visuals.</para>
    ///     <para xml:lang="zh-CN">在 <see cref="NIntent" /> 更新显示后同步额外角标。</para>
    /// </summary>
    internal sealed class NIntentExtraCornerAmountLabelsPatch : IPatchMethod
    {
        public static string PatchId => "nintent_extra_corner_amount_labels";
        public static bool IsCritical => false;

        public static string Description =>
            "Render extra intent badges on NIntent with independent per-entry anchors";

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
    ///     <para xml:lang="en">Releases extra intent badges and subscriptions when the node exits the scene tree.</para>
    ///     <para xml:lang="zh-CN">意图节点退出场景树时，释放额外角标并取消事件订阅。</para>
    /// </summary>
    internal sealed class NIntentExtraCornerAmountLabelsExitTreePatch : IPatchMethod
    {
        public static string PatchId => "nintent_extra_corner_amount_labels_exit_tree";
        public static bool IsCritical => false;
        public static string Description => "Release extra intent badges when NIntent exits the scene tree";

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
