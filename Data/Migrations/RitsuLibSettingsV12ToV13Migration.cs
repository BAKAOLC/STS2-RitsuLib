using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV12ToV13Migration : IMigration
    {
        public int FromVersion => 12;

        public int ToVersion => 13;

        public bool Migrate(JsonObject data)
        {
            data["update_check_interval_minutes"] ??= 60d;
            data["update_check_skip_in_combat"] ??= true;
            return true;
        }
    }
}
