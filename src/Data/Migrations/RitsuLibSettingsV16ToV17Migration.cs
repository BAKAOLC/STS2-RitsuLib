using System.Text.Json.Nodes;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV16ToV17Migration : IMigration
    {
        public int FromVersion => 16;

        public int ToVersion => 17;

        public bool Migrate(JsonObject data)
        {
            data["creature_picker_hotkey"] = RitsuLibSettings.DefaultCreaturePickerHotkey;
            return true;
        }
    }
}
