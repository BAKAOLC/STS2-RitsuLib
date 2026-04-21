using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV4ToV5Migration : IMigration
    {
        public int FromVersion => 4;

        public int ToVersion => 5;

        public bool Migrate(JsonObject data)
        {
            data["self_check_output_folder_path"] ??= "user://ritsulib_self_check";
            data["self_check_on_first_main_menu"] ??= false;
            return true;
        }
    }
}
