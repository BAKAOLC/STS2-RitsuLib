using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Migrations
{
    internal sealed class RitsuLibSettingsV10ToV11Migration : IMigration
    {
        public int FromVersion => 10;

        public int ToVersion => 11;

        public bool Migrate(JsonObject data)
        {
            data["mod_source_hover_tips_cards"] ??= true;
            data["mod_source_hover_tips_relics"] ??= true;
            data["mod_source_hover_tips_potions"] ??= true;
            data["mod_source_hover_tips_powers"] ??= true;
            data["mod_source_hover_tips_orbs"] ??= true;
            data["mod_source_hover_tips_enchantments"] ??= true;
            data["mod_source_hover_tips_afflictions"] ??= true;
            data["mod_source_hover_tips_keywords"] ??= true;
            data["mod_source_hover_tips_events"] ??= true;
            data["mod_source_hover_tips_creatures"] ??= true;
            data["mod_source_hover_tips_game_terms"] ??= true;
            return true;
        }
    }
}
