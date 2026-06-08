using Godot;

namespace STS2RitsuLib.Settings
{
    internal enum ModSettingsReusableEntryKind
    {
        Toggle,
        TextButton,
    }

    internal sealed class ModSettingsReusableEntryNodePool
    {
        private const int WarmRetainedPerKind = 24;
        private const ulong IdleReleaseMsec = 20_000;

        private readonly Dictionary<ModSettingsReusableEntryKind, Queue<ModSettingsUiFactory.ReusableSettingLine>>
            _buckets = new();

        internal ModSettingsUiFactory.ReusableSettingLine Rent(ModSettingsReusableEntryKind kind)
        {
            Sweep();

            if (!_buckets.TryGetValue(kind, out var bucket)) return new(kind);
            while (bucket.Count > 0)
            {
                var candidate = bucket.Dequeue();
                if (GodotObject.IsInstanceValid(candidate))
                    return candidate;
            }

            return new(kind);
        }

        internal void Return(ModSettingsUiFactory.ReusableSettingLine line)
        {
            if (!GodotObject.IsInstanceValid(line))
                return;

            line.ReleaseForPool();
            Sweep();

            if (!_buckets.TryGetValue(line.Kind, out var bucket))
            {
                bucket = new();
                _buckets[line.Kind] = bucket;
            }

            line.LastUsedMsec = Time.GetTicksMsec();
            bucket.Enqueue(line);
            Sweep();
        }

        internal void Sweep()
        {
            var now = Time.GetTicksMsec();
            foreach (var pair in _buckets)
            {
                var valid = new List<ModSettingsUiFactory.ReusableSettingLine>(pair.Value.Count);
                while (pair.Value.Count > 0)
                {
                    var line = pair.Value.Dequeue();
                    if (!GodotObject.IsInstanceValid(line))
                        continue;

                    valid.Add(line);
                }

                valid.Sort((left, right) => left.LastUsedMsec.CompareTo(right.LastUsedMsec));
                var kept = new Queue<ModSettingsUiFactory.ReusableSettingLine>(valid.Count);
                var idleOverflow = Math.Max(0, valid.Count - WarmRetainedPerKind);
                for (var i = 0; i < valid.Count; i++)
                {
                    var line = valid[i];
                    var idleExpired = now >= line.LastUsedMsec && now - line.LastUsedMsec >= IdleReleaseMsec;
                    if (i < idleOverflow && idleExpired)
                    {
                        line.QueueFree();
                        continue;
                    }

                    kept.Enqueue(line);
                }

                _buckets[pair.Key] = kept;
            }
        }
    }

    internal static partial class ModSettingsUiFactory
    {
        internal sealed partial class ReusableSettingLine : FastSettingLine
        {
            public ReusableSettingLine(ModSettingsReusableEntryKind kind)
                : base(kind switch
                {
                    ModSettingsReusableEntryKind.Toggle => new ModSettingsToggleControl(false, null),
                    ModSettingsReusableEntryKind.TextButton => new ModSettingsTextButton(
                        string.Empty, ModSettingsButtonTone.Normal, null),
                    _ => null,
                })
            {
                Kind = kind;
                Name = $"Reusable{kind}EntryLine";
            }

            public ReusableSettingLine()
            {
            }

            internal ModSettingsReusableEntryKind Kind { get; }

            internal ulong LastUsedMsec { get; set; }
        }
    }
}
