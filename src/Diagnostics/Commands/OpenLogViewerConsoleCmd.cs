using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Diagnostics.Logging;

namespace STS2RitsuLib.Diagnostics.Commands
{
    /// <summary>
    ///     Opens the local RitsuLib debug log viewer in the system browser.
    ///     在系统浏览器中打开本机 RitsuLib 调试日志查看器。
    /// </summary>
    public sealed class OpenLogViewerConsoleCmd : AbstractConsoleCmd
    {
        /// <inheritdoc />
        public override string CmdName => "openlogviewer";

        /// <inheritdoc />
        public override string Args => "";

        /// <inheritdoc />
        public override string Description => "Opens the local RitsuLib debug log viewer in a browser.";

        /// <inheritdoc />
        public override bool IsNetworked => false;

        /// <inheritdoc />
        public override CmdResult Process(Player? issuingPlayer, string[] args)
        {
            if (args.Length > 0)
                return new(false, "Usage: openlogviewer");

            var result = RitsuDebugLogPipeline.TryOpenViewerInBrowser();
            return new(result.Success, result.Message);
        }
    }
}
