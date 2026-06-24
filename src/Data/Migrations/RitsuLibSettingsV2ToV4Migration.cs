using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV2ToV4Migration : IMigration
    {
        public int FromVersion => 2;

        public int ToVersion => 4;

        public bool Migrate(JsonObject data)
        {
            return true;
        }
    }
}
