using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Ui.Files
{
    internal sealed class RitsuFileDialogPreferences
    {
        private static RitsuFileDialogPreferences? _current;

        public List<string> Favorites { get; set; } = [];
        public List<string> Recent { get; set; } = [];
        public Dictionary<string, string> LastDirectories { get; set; } = new(StringComparer.Ordinal);
        public bool ShowHidden { get; set; }
        public bool Thumbnails { get; set; } = true;
        public bool ShowNameFilter { get; set; }
        public int SortOrder { get; set; }

        internal static RitsuFileDialogPreferences Current
        {
            get
            {
                if (_current != null)
                    return _current;
                var result = FileOperations.ReadJson<RitsuFileDialogPreferences>(StoragePath, logContext: "FileDialog");
                _current = result is { Success: true, Data: not null } ? result.Data : new();
                _current.Favorites = CleanPaths(_current.Favorites, 128);
                _current.Recent = CleanPaths(_current.Recent, 20);
                _current.LastDirectories = (_current.LastDirectories ?? [])
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Key.Length <= 256 &&
                                   !string.IsNullOrWhiteSpace(pair.Value))
                    .TakeLast(128).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                _current.SortOrder = Math.Clamp(_current.SortOrder, 0, 5);
                return _current;
            }
        }

        private static string StoragePath =>
            $"{ProfileManager.GetBasePath(SaveScope.Global, 0)}/file-dialog.json";

        internal void Remember(string stateKey, string directory)
        {
            Recent.RemoveAll(path => PathComparer.Equals(path, directory));
            Recent.Insert(0, directory);
            if (Recent.Count > 20)
                Recent.RemoveRange(20, Recent.Count - 20);
            LastDirectories.Remove(stateKey);
            LastDirectories[stateKey] = directory;
            if (LastDirectories.Count > 128)
                LastDirectories.Remove(LastDirectories.Keys.First());
            Save();
        }

        internal void Save()
        {
            FileOperations.WriteJson(StoragePath, this, logContext: "FileDialog");
        }

        internal static StringComparer PathComparer =>
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private static List<string> CleanPaths(IEnumerable<string>? paths, int maximum)
        {
            return
            [
                .. (paths ?? []).Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(PathComparer).Take(maximum),
            ];
        }
    }
}
