using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV0Or1ToV2Migration : IMigration
    {
        public int FromVersion => 0;

        public int ToVersion => 2;

        public bool Migrate(JsonObject data)
        {
            data["debug_compat_loc_table"] = true;
            data["debug_compat_unlock_epoch"] = true;
            data["debug_compat_ancient_architect"] = true;
            return true;
        }
    }
}
