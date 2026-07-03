using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV14ToV15Migration : IMigration
    {
        public int FromVersion => 14;

        public int ToVersion => 15;

        public bool Migrate(JsonObject data)
        {
            data["debug_compatibility_mode"] = true;
            data["debug_compat_loc_table"] = true;
            data["debug_compat_unlock_epoch"] = true;
            data["debug_compat_ancient_architect"] = true;
            return true;
        }
    }
}
