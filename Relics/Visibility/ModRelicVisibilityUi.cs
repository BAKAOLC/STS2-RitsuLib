using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace STS2RitsuLib.Relics.Visibility
{
    internal static class ModRelicVisibilityUi
    {
        private static readonly AccessTools.FieldRef<NRelicInventory, Player?> PlayerRef =
            AccessTools.FieldRefAccess<NRelicInventory, Player?>("_player");

        private static readonly AccessTools.FieldRef<NRelicInventory, List<NRelicInventoryHolder>> RelicNodesRef =
            AccessTools.FieldRefAccess<NRelicInventory, List<NRelicInventoryHolder>>("_relicNodes");

        private static readonly InventoryAddDelegate? AddRelic =
            AccessTools.Method(
                    typeof(NRelicInventory),
                    "Add",
                    [typeof(RelicModel), typeof(bool), typeof(int)])
                ?.CreateDelegate<InventoryAddDelegate>();

        private static readonly Action<NRelicInventory>? UpdateNavigation =
            AccessTools.Method(typeof(NRelicInventory), "UpdateNavigation")
                ?.CreateDelegate<Action<NRelicInventory>>();

        public static bool Contains(NRelicInventory inventory, RelicModel relic)
        {
            return RelicNodesRef(inventory).Any(holder => holder.Relic.Model == relic);
        }

        public static bool Refresh(NRelicInventory? inventory)
        {
            if (inventory == null || AddRelic == null)
                return false;

            var player = PlayerRef(inventory);
            if (player == null)
                return false;

            var nodes = RelicNodesRef(inventory);
            var changed = RemoveHiddenNodes(inventory, nodes);
            var visibleIndex = 0;

            foreach (var relic in player.Relics)
            {
                if (!ModRelicVisibilityRegistry.IsVisible(relic))
                    continue;

                var holder = nodes.FirstOrDefault(node => node.Relic.Model == relic);
                if (holder == null)
                {
                    AddRelic(inventory, relic, true, -1);
                    holder = nodes.FirstOrDefault(node => node.Relic.Model == relic);
                    changed = true;
                }

                if (holder != null)
                    changed |= MoveToVisibleIndex(inventory, nodes, holder, visibleIndex);

                visibleIndex++;
            }

            if (!changed)
                return false;

            inventory.EmitSignal(NRelicInventory.SignalName.RelicsChanged);
            UpdateNavigation?.Invoke(inventory);
            return true;
        }

        private static bool RemoveHiddenNodes(
            NRelicInventory inventory,
            List<NRelicInventoryHolder> nodes)
        {
            var changed = false;
            foreach (var holder in nodes.ToArray())
            {
                if (ModRelicVisibilityRegistry.IsVisible(holder.Relic.Model))
                    continue;

                nodes.Remove(holder);
                inventory.RemoveChild(holder);
                holder.QueueFree();
                changed = true;
            }

            return changed;
        }

        private static bool MoveToVisibleIndex(
            NRelicInventory inventory,
            List<NRelicInventoryHolder> nodes,
            NRelicInventoryHolder holder,
            int visibleIndex)
        {
            var currentIndex = nodes.IndexOf(holder);
            if (currentIndex == visibleIndex)
                return false;

            if (currentIndex >= 0)
                nodes.RemoveAt(currentIndex);

            var targetIndex = Math.Clamp(visibleIndex, 0, nodes.Count);
            nodes.Insert(targetIndex, holder);
            inventory.MoveChild(holder, targetIndex);
            return true;
        }

        private delegate void InventoryAddDelegate(
            NRelicInventory inventory,
            RelicModel relic,
            bool startsShown,
            int index);
    }
}
