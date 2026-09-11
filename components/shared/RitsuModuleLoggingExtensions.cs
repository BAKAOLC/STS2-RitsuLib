using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Shared
{
    internal static class RitsuModuleLoggingExtensions
    {
        internal static void ErrorNoTrace(this Logger logger, string text)
        {
            RitsuLibFramework.ErrorNoTrace(logger, text);
        }
    }
}
