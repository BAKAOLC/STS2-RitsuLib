using System.Text.Json;
using System.Text.Json.Serialization;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugStatePresetApplyMode
    {
        Add,
        Replace,
    }

    internal sealed class RitsuDebugStatePresetCard
    {
        [JsonPropertyName("id")]
        public string CardId { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; } = 1;

        [JsonPropertyName("upgrade")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int UpgradeLevels { get; set; }

        [JsonPropertyName("cost")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BaseCost { get; set; }

        [JsonPropertyName("replay")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ReplayCount { get; set; }

        [JsonPropertyName("vars")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, int>? DynamicVars { get; set; }

        [JsonPropertyName("exhaust")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Exhaust { get; set; }

        [JsonPropertyName("ethereal")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Ethereal { get; set; }

        [JsonPropertyName("unplayable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Unplayable { get; set; }

        [JsonPropertyName("enchantment")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EnchantmentId { get; set; }

        [JsonPropertyName("enchantment_amount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? EnchantmentAmount { get; set; }

        internal RitsuDebugStatePresetCard Clone()
        {
            return new()
            {
                CardId = CardId,
                Count = Count,
                UpgradeLevels = UpgradeLevels,
                BaseCost = BaseCost,
                ReplayCount = ReplayCount,
                DynamicVars = DynamicVars == null ? null : new(DynamicVars, StringComparer.Ordinal),
                Exhaust = Exhaust,
                Ethereal = Ethereal,
                Unplayable = Unplayable,
                EnchantmentId = EnchantmentId,
                EnchantmentAmount = EnchantmentAmount,
            };
        }

        internal RitsuDebugCardActions.CardStatePayload ToCardState()
        {
            return new(
                BaseCost,
                ReplayCount,
                DynamicVars,
                Exhaust,
                Ethereal,
                Unplayable,
                EnchantmentId,
                EnchantmentAmount);
        }
    }

    internal sealed class RitsuDebugStatePresetCardPile
    {
        [JsonPropertyName("pile")]
        public string Pile { get; set; } = string.Empty;

        [JsonPropertyName("mode")]
        public RitsuDebugStatePresetApplyMode ApplyMode { get; set; }

        [JsonPropertyName("cards")]
        public List<RitsuDebugStatePresetCard> Cards { get; set; } = [];

        internal RitsuDebugStatePresetCardPile Clone()
        {
            return new()
            {
                Pile = Pile,
                ApplyMode = ApplyMode,
                Cards = Cards.Select(static card => card.Clone()).ToList(),
            };
        }
    }

    internal sealed class RitsuDebugStatePresetInventory
    {
        [JsonPropertyName("mode")]
        public RitsuDebugStatePresetApplyMode ApplyMode { get; set; }

        [JsonPropertyName("items")]
        public List<string> ModelIds { get; set; } = [];

        internal RitsuDebugStatePresetInventory Clone()
        {
            return new() { ApplyMode = ApplyMode, ModelIds = [.. ModelIds] };
        }
    }

    internal sealed class RitsuDebugStatePresetPotion
    {
        [JsonPropertyName("id")]
        public string PotionId { get; set; } = string.Empty;

        [JsonPropertyName("slot")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SlotIndex { get; set; }

        internal RitsuDebugStatePresetPotion Clone()
        {
            return new() { PotionId = PotionId, SlotIndex = SlotIndex };
        }
    }

    internal sealed class RitsuDebugStatePresetPotions
    {
        [JsonPropertyName("mode")]
        public RitsuDebugStatePresetApplyMode ApplyMode { get; set; }

        [JsonPropertyName("items")]
        public List<RitsuDebugStatePresetPotion> Items { get; set; } = [];

        internal RitsuDebugStatePresetPotions Clone()
        {
            return new()
            {
                ApplyMode = ApplyMode,
                Items = Items.Select(static potion => potion.Clone()).ToList(),
            };
        }
    }

    internal sealed class RitsuDebugStatePresetPower
    {
        [JsonPropertyName("id")]
        public string PowerId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public int Amount { get; set; } = 1;

        internal RitsuDebugStatePresetPower Clone()
        {
            return new() { PowerId = PowerId, Amount = Amount };
        }
    }

    internal sealed class RitsuDebugStatePresetPowers
    {
        [JsonPropertyName("mode")]
        public RitsuDebugStatePresetApplyMode ApplyMode { get; set; }

        [JsonPropertyName("items")]
        public List<RitsuDebugStatePresetPower> Items { get; set; } = [];

        internal RitsuDebugStatePresetPowers Clone()
        {
            return new()
            {
                ApplyMode = ApplyMode,
                Items = Items.Select(static power => power.Clone()).ToList(),
            };
        }
    }

    internal sealed class RitsuDebugStatePresetPlayer
    {
        [JsonPropertyName("gold")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Gold { get; set; }

        [JsonPropertyName("current_hp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CurrentHp { get; set; }

        [JsonPropertyName("max_hp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxHp { get; set; }

        [JsonPropertyName("max_energy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxEnergy { get; set; }

        [JsonPropertyName("potion_slots")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PotionSlots { get; set; }

        [JsonPropertyName("energy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Energy { get; set; }

        [JsonPropertyName("stars")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Stars { get; set; }

        [JsonPropertyName("block")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Block { get; set; }

        internal bool HasAnyValue => Gold.HasValue || CurrentHp.HasValue || MaxHp.HasValue ||
                                     MaxEnergy.HasValue || PotionSlots.HasValue || Energy.HasValue ||
                                     Stars.HasValue || Block.HasValue;

        internal RitsuDebugStatePresetPlayer Clone()
        {
            return new()
            {
                Gold = Gold,
                CurrentHp = CurrentHp,
                MaxHp = MaxHp,
                MaxEnergy = MaxEnergy,
                PotionSlots = PotionSlots,
                Energy = Energy,
                Stars = Stars,
                Block = Block,
            };
        }
    }

    internal sealed class RitsuDebugStatePreset
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("card_piles")]
        public List<RitsuDebugStatePresetCardPile> CardPiles { get; set; } = [];

        [JsonPropertyName("relics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RitsuDebugStatePresetInventory? Relics { get; set; }

        [JsonPropertyName("potions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RitsuDebugStatePresetPotions? Potions { get; set; }

        [JsonPropertyName("powers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RitsuDebugStatePresetPowers? Powers { get; set; }

        [JsonPropertyName("player")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RitsuDebugStatePresetPlayer? Player { get; set; }

        internal bool HasAnyContent => CardPiles.Count > 0 || Relics != null || Potions != null ||
                                       Powers != null || Player is { HasAnyValue: true };

        internal RitsuDebugStatePreset Clone(bool assignNewId = false)
        {
            return new()
            {
                Id = assignNewId ? Guid.NewGuid().ToString("N") : Id,
                Name = Name,
                CardPiles = CardPiles.Select(static pile => pile.Clone()).ToList(),
                Relics = Relics?.Clone(),
                Potions = Potions?.Clone(),
                Powers = Powers?.Clone(),
                Player = Player?.Clone(),
            };
        }
    }

    internal sealed class RitsuDebugStatePresetCollection
    {
        internal const int CurrentSchemaVersion = 1;

        [JsonPropertyName("schema_version")]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonPropertyName("presets")]
        public List<RitsuDebugStatePreset> Presets { get; set; } = [];
    }

    internal static class RitsuDebugStatePresetStore
    {
        internal const int MaximumPresetCount = 128;
        internal const int MaximumNameLength = 80;
        internal const int MaximumCardsPerPile = 100;
        internal const int MaximumRelics = 128;
        internal const int MaximumPowers = 128;
        internal const string DataKey = "debug-state-presets";
        internal const string FileName = "debug_state_presets.json";

        private static readonly JsonSerializerOptions ExportOptions = new()
        {
            PropertyNameCaseInsensitive = false,
            WriteIndented = false,
        };
        private static readonly ModDataStore Store = ModDataStore.For(Const.ModId);

        internal static IReadOnlyList<RitsuDebugStatePreset> GetSnapshot()
        {
            RitsuLibSettingsStore.Initialize();
            var data = Store.Get<RitsuDebugStatePresetCollection>(DataKey);
            return Array.AsReadOnly((data.Presets ?? [])
                .Where(IsReadable)
                .Take(MaximumPresetCount)
                .Select(static preset => preset.Clone())
                .ToArray());
        }

        internal static bool TrySave(RitsuDebugStatePreset preset, out RitsuDebugActionFeedback feedback)
        {
            ArgumentNullException.ThrowIfNull(preset);
            var check = RitsuDebugStatePresetActions.ValidateStoredPreset(preset);
            if (!check.Success)
            {
                feedback = check.Feedback;
                return false;
            }

            RitsuLibSettingsStore.Initialize();
            var data = Store.Get<RitsuDebugStatePresetCollection>(DataKey);
            data.Presets ??= [];
            var index = data.Presets.FindIndex(candidate =>
                candidate != null && string.Equals(candidate.Id, preset.Id, StringComparison.OrdinalIgnoreCase));
            if (data.Presets.Any(candidate =>
                    IsReadable(candidate) &&
                    !string.Equals(candidate.Id, preset.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.Name, preset.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.duplicateName",
                    "A preset named '{0}' already exists.",
                    preset.Name.Trim());
                return false;
            }

            if (index < 0 && data.Presets.Count(IsReadable) >= MaximumPresetCount)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.countLimit",
                    "At most {0} presets can be stored.",
                    MaximumPresetCount);
                return false;
            }

            var stored = preset.Clone();
            stored.Name = stored.Name.Trim();
            if (index < 0)
                data.Presets.Add(stored);
            else
                data.Presets[index] = stored;
            data.SchemaVersion = RitsuDebugStatePresetCollection.CurrentSchemaVersion;
            Store.Save(DataKey);
            feedback = default;
            return true;
        }

        internal static bool TryDelete(string presetId)
        {
            if (!IsValidId(presetId))
                return false;
            RitsuLibSettingsStore.Initialize();
            var data = Store.Get<RitsuDebugStatePresetCollection>(DataKey);
            data.Presets ??= [];
            var removed = data.Presets.RemoveAll(candidate =>
                candidate != null && string.Equals(candidate.Id, presetId, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
                Store.Save(DataKey);
            return removed;
        }

        internal static string Export(RitsuDebugStatePreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            return JsonSerializer.Serialize(preset, ExportOptions);
        }

        internal static bool TryImport(string json, out RitsuDebugStatePreset preset,
            out RitsuDebugActionFeedback feedback)
        {
            preset = null!;
            if (string.IsNullOrWhiteSpace(json) || json.Length > RitsuDebugStatePresetActions.MaximumImportCharacters)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.importInvalid",
                    "The imported preset is empty, malformed, or too large.");
                return false;
            }

            try
            {
                preset = JsonSerializer.Deserialize<RitsuDebugStatePreset>(json, ExportOptions)!;
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.importInvalid",
                    "The imported preset is empty, malformed, or too large.");
                return false;
            }

            if (preset == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.importInvalid",
                    "The imported preset is empty, malformed, or too large.");
                return false;
            }

            preset.Id = Guid.NewGuid().ToString("N");
            var check = RitsuDebugStatePresetActions.ValidateStoredPreset(preset);
            if (!check.Success)
            {
                feedback = check.Feedback;
                return false;
            }

            feedback = default;
            return true;
        }

        private static bool IsReadable(RitsuDebugStatePreset? preset)
        {
            return preset != null && IsValidId(preset.Id) &&
                   !string.IsNullOrWhiteSpace(preset.Name) && preset.Name.Length <= MaximumNameLength &&
                   preset.CardPiles != null && preset.CardPiles.All(static pile =>
                       pile?.Cards != null && pile.Cards.All(static card => card != null)) &&
                   (preset.Relics == null || preset.Relics.ModelIds != null &&
                       preset.Relics.ModelIds.All(static id => id != null)) &&
                   (preset.Potions == null || preset.Potions.Items != null &&
                       preset.Potions.Items.All(static potion => potion != null)) &&
                   (preset.Powers == null || preset.Powers.Items != null &&
                       preset.Powers.Items.All(static power => power != null)) && preset.HasAnyContent;
        }

        private static bool IsValidId(string? id)
        {
            return id is { Length: 32 } && Guid.TryParseExact(id, "N", out _);
        }
    }
}
