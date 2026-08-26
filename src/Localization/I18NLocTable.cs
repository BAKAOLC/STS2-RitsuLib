using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization
{
    internal sealed class I18NLocTable : LocTable
    {
        private I18NLocTable(
            string name,
            Dictionary<string, string> local,
            Dictionary<string, string> fallback)
            : base(name, local, fallback.Count == 0 ? null : new LocTable(name, fallback))
        {
        }

        internal static I18NLocTable Create(string name, I18N i18N)
        {
            var (local, fallback) = i18N.CaptureLocTableData();
            return new(name, local, fallback);
        }
    }
}
