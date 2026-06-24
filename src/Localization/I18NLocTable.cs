using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization
{
    internal sealed class I18NLocTable(string name, I18N i18N) : LocTable(name, [])
    {
        internal string Name { get; } = name;

        internal I18N I18N { get; } = i18N;
    }
}
