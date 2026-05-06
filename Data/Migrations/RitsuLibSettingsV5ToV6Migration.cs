using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV5ToV6Migration : IMigration
    {
        public int FromVersion => 5;

        public int ToVersion => 6;

        public bool Migrate(JsonObject data)
        {
            data["sync_mod_data_to_steam_cloud"] ??= false;
            return true;
        }
    }
}
