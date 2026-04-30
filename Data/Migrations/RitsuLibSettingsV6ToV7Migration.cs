using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV6ToV7Migration : IMigration
    {
        public int FromVersion => 6;

        public int ToVersion => 7;

        public bool Migrate(JsonObject data)
        {
            data["ui_shell_theme_id"] ??= "default";
            return true;
        }
    }
}
