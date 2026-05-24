using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV9ToV10Migration : IMigration
    {
        public int FromVersion => 9;

        public int ToVersion => 10;

        public bool Migrate(JsonObject data)
        {
            data["mod_source_hover_tips_include_non_details"] ??= false;
            return true;
        }
    }
}
