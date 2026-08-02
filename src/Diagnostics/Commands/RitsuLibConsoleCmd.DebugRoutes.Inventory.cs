using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Diagnostics.DebugTools;

namespace STS2RitsuLib.Diagnostics.Commands
{
    public sealed partial class RitsuLibConsoleCmd
    {
        private static DebugConsoleGroup DebugRelicGroup()
        {
            return DebugConsoleGroup.Branch(
                "relic",
                [
                    InventoryAction("add", true, true),
                    InventoryAction("remove", true, false),
                    DebugConsoleAction.Create(
                        "clear",
                        static (_, player, args) => ProcessClearInventory(
                            player!,
                            args,
                            RitsuDebugInventoryKind.Relics),
                        static (command, player, args) => command.CompleteOptionalPlayer(player, args)),
                ]);
        }

        private static DebugConsoleGroup DebugPotionGroup()
        {
            return DebugConsoleGroup.Branch(
                "potion",
                [
                    InventoryAction("add", false, true),
                    DebugConsoleAction.Create(
                        "discard",
                        static (_, player, args) => ProcessDiscardPotion(player!, args),
                        static (command, player, args) => command.CompleteDiscardPotion(player, args)),
                    DebugConsoleAction.Create(
                        "discard-all",
                        static (_, player, args) => ProcessClearInventory(
                            player!,
                            args,
                            RitsuDebugInventoryKind.Potions),
                        static (command, player, args) => command.CompleteOptionalPlayer(player, args)),
                ]);
        }

        private static CmdResult ProcessClearInventory(
            Player issuingPlayer,
            string[] args,
            RitsuDebugInventoryKind kind)
        {
            if (args.Length is < 3 or > 4)
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 3, out var target, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugInventoryActions.SubmitClearInventory(
                issuingPlayer,
                target,
                kind));
        }

        private static CmdResult ProcessDiscardPotion(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 4 or > 5)
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 4, out var target, out var error))
                return DebugFailure(error);
            if (!int.TryParse(args[3], out var slotIndex) || slotIndex < 0 || slotIndex >= target.MaxPotionCount)
                return DebugFailure(
                    "inventory.potionSlotRange",
                    "Potion slot must be between 0 and {0}.",
                    target.MaxPotionCount - 1);
            var potion = target.GetPotionAtSlotIndex(slotIndex);
            if (potion == null)
                return DebugFailure(
                    "inventory.potionSlotEmpty",
                    "Potion slot {0} is empty.",
                    slotIndex);
            return ToCmdResult(RitsuDebugInventoryActions.SubmitDiscardPotion(
                issuingPlayer,
                target,
                slotIndex,
                potion.Id.ToString()));
        }

        private CompletionResult CompleteDiscardPotion(Player? player, string[] args)
        {
            if (args.Length == 4)
            {
                var count = player?.MaxPotionCount ?? 0;
                return CompleteCurrentArgument(
                    Enumerable.Range(0, count).Select(static index => index.ToString()),
                    args);
            }

            return args.Length == 5
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }
    }
}
