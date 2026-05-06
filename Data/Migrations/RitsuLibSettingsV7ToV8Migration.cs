using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV7ToV8Migration : IMigration
    {
        public int FromVersion => 7;

        public int ToVersion => 8;

        public bool Migrate(JsonObject data)
        {
            data["toast_enabled"] ??= true;
            data["toast_anchor"] ??= "topright";
            data["toast_offset_x"] ??= -24d;
            data["toast_offset_y"] ??= 24d;
            data["toast_max_visible"] ??= 3;
            data["toast_duration_seconds"] ??= 3.5d;
            data["toast_animation"] ??= "fadeslide";
            return true;
        }
    }
}
