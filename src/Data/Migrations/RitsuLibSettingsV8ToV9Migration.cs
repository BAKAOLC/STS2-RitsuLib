using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV8ToV9Migration : IMigration
    {
        public int FromVersion => 8;

        public int ToVersion => 9;

        public bool Migrate(JsonObject data)
        {
            data["mod_source_hover_tips_enabled"] ??= false;
            data["mod_source_hover_tips_include_vanilla"] ??= false;
            return true;
        }
    }
}
