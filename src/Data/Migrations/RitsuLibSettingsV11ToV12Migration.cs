using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV11ToV12Migration : IMigration
    {
        public int FromVersion => 11;

        public int ToVersion => 12;

        public bool Migrate(JsonObject data)
        {
            data["main_menu_mod_settings_button_enabled"] ??= true;
            return true;
        }
    }
}
