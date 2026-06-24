using System.Text.Json.Nodes;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV13ToV14Migration : IMigration
    {
        private const double PreviousDefaultToastDurationSeconds = 3.5d;

        public int FromVersion => 13;

        public int ToVersion => 14;

        public bool Migrate(JsonObject data)
        {
            if (data["toast_duration_seconds"] is not JsonValue value ||
                (value.TryGetValue<double>(out var seconds) &&
                 Math.Abs(seconds - PreviousDefaultToastDurationSeconds) < 0.000_001d))
                data["toast_duration_seconds"] = RitsuLibSettings.DefaultToastDurationSeconds;

            return true;
        }
    }
}
