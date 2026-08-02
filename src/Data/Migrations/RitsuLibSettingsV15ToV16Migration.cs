using System.Text.Json.Nodes;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV15ToV16Migration : IMigration
    {
        public int FromVersion => 15;

        public int ToVersion => 16;

        public bool Migrate(JsonObject data)
        {
            data["developer_tools_enabled"] = false;
            data["developer_tools_allow_client_requests"] = false;
            data["settings_open_hotkey"] = RitsuLibSettings.DefaultSettingsOpenHotkey;
            data["debug_tools_open_hotkey"] = RitsuLibSettings.DefaultDebugToolsOpenHotkey;
            return true;
        }
    }
}
