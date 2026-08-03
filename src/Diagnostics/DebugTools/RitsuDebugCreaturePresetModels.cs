using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal sealed class RitsuDebugCreaturePreset
    {
        [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("monster_id")] public string MonsterId { get; set; } = string.Empty;

        [JsonPropertyName("current_hp")] public int CurrentHp { get; set; }

        [JsonPropertyName("max_hp")] public int MaxHp { get; set; }

        [JsonPropertyName("block")] public int Block { get; set; }

        [JsonPropertyName("powers")] public List<RitsuDebugStatePresetPower> Powers { get; set; } = [];

        internal RitsuDebugCreaturePreset Clone(bool assignNewId = false)
        {
            return new()
            {
                Id = assignNewId ? Guid.NewGuid().ToString("N") : Id,
                Name = Name,
                MonsterId = MonsterId,
                CurrentHp = CurrentHp,
                MaxHp = MaxHp,
                Block = Block,
                Powers = [.. Powers.Select(static power => power.Clone())],
            };
        }
    }

    internal sealed class RitsuDebugCreaturePresetCollection
    {
        internal const int CurrentSchemaVersion = 1;

        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonPropertyName("presets")] public List<RitsuDebugCreaturePreset> Presets { get; set; } = [];
    }

    internal static class RitsuDebugCreaturePresetStore
    {
        internal const int MaximumPresetCount = 64;
        internal const int MaximumNameLength = 80;
        internal const string DataKey = "debug-creature-presets";
        internal const string FileName = "debug_creature_presets.json";

        private static readonly ModDataStore Store = ModDataStore.For(Const.ModId);

        internal static IReadOnlyList<RitsuDebugCreaturePreset> GetSnapshot()
        {
            RitsuLibSettingsStore.Initialize();
            var data = Store.Get<RitsuDebugCreaturePresetCollection>(DataKey);
            return Array.AsReadOnly<RitsuDebugCreaturePreset>(
            [
                .. (data.Presets ?? [])
                .Where(IsReadable)
                .Take(MaximumPresetCount)
                .Select(static preset => preset.Clone()),
            ]);
        }

        internal static bool TryCapture(
            Creature creature,
            string name,
            out RitsuDebugCreaturePreset preset,
            out RitsuDebugActionFeedback feedback)
        {
            ArgumentNullException.ThrowIfNull(creature);
            preset = null!;
            if (creature.IsPlayer || creature.Monster == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "creaturePreset.monsterRequired",
                    "Only enemies backed by a monster model can be saved as a creature preset.");
                return false;
            }

            preset = new()
            {
                Name = name.Trim(),
                MonsterId = creature.Monster.Id.ToString(),
                CurrentHp = creature.CurrentHp,
                MaxHp = creature.MaxHp,
                Block = creature.Block,
                Powers =
                [
                    .. creature.Powers.Select(static power => new RitsuDebugStatePresetPower
                    {
                        PowerId = power.Id.ToString(),
                        Amount = power.Amount,
                    }),
                ],
            };
            var check = RitsuDebugCreaturePresetActions.ValidateStoredPreset(preset);
            if (check.Success)
            {
                feedback = default;
                return true;
            }

            preset = null!;
            feedback = check.Feedback;
            return false;
        }

        internal static bool TrySave(RitsuDebugCreaturePreset preset, out RitsuDebugActionFeedback feedback)
        {
            ArgumentNullException.ThrowIfNull(preset);
            var check = RitsuDebugCreaturePresetActions.ValidateStoredPreset(preset);
            if (!check.Success)
            {
                feedback = check.Feedback;
                return false;
            }

            RitsuLibSettingsStore.Initialize();
            var data = Store.Get<RitsuDebugCreaturePresetCollection>(DataKey);
            data.Presets ??= [];
            var index = data.Presets.FindIndex(candidate =>
                candidate != null && string.Equals(candidate.Id, preset.Id, StringComparison.OrdinalIgnoreCase));
            if (data.Presets.Any(candidate =>
                    IsReadable(candidate) &&
                    !string.Equals(candidate.Id, preset.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(candidate.Name, preset.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "creaturePreset.duplicateName",
                    "A creature preset named '{0}' already exists.",
                    preset.Name.Trim());
                return false;
            }

            if (index < 0 && data.Presets.Count(IsReadable) >= MaximumPresetCount)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "creaturePreset.countLimit",
                    "At most {0} creature presets can be stored.",
                    MaximumPresetCount);
                return false;
            }

            var stored = preset.Clone();
            stored.Name = stored.Name.Trim();
            if (index < 0)
                data.Presets.Add(stored);
            else
                data.Presets[index] = stored;
            data.SchemaVersion = RitsuDebugCreaturePresetCollection.CurrentSchemaVersion;
            Store.Save(DataKey);
            feedback = default;
            return true;
        }

        internal static bool TryDelete(string presetId)
        {
            if (!IsValidId(presetId))
                return false;
            RitsuLibSettingsStore.Initialize();
            var data = Store.Get<RitsuDebugCreaturePresetCollection>(DataKey);
            data.Presets ??= [];
            var removed = data.Presets.RemoveAll(candidate =>
                candidate != null && string.Equals(candidate.Id, presetId, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
                Store.Save(DataKey);
            return removed;
        }

        private static bool IsReadable(RitsuDebugCreaturePreset? preset)
        {
            return preset != null && IsValidId(preset.Id) &&
                   !string.IsNullOrWhiteSpace(preset.Name) && preset.Name.Length <= MaximumNameLength &&
                   !string.IsNullOrWhiteSpace(preset.MonsterId) && preset.Powers != null &&
                   preset.Powers.All(static power => power != null);
        }

        private static bool IsValidId(string? id)
        {
            return id is { Length: 32 } && Guid.TryParseExact(id, "N", out _);
        }
    }
}
