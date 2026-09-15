using System.IO.Enumeration;
using Godot;
using STS2RitsuLib.Ui.Shell.Theme;
using Environment = System.Environment;

namespace STS2RitsuLib.Ui.Files
{
    public sealed partial class RitsuFileDialog
    {
        private string ResolveInitialDirectory()
        {
            var candidates = new[]
            {
                _options.InitialDirectory,
                RitsuFileDialogPreferences.Current.LastDirectories.GetValueOrDefault(_options.StateKey),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.CurrentDirectory,
            };
            return candidates.First(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))!;
        }

        private void Navigate(string path, int? historyPosition = null)
        {
            if (_closing)
                return;
            try
            {
                path = Path.GetFullPath(path,
                    string.IsNullOrEmpty(_directory) ? Environment.CurrentDirectory : _directory);
                var oldCancellation = _listingCancellation;
                oldCancellation?.Cancel();
                if (_listing != null)
                    _ = _listing.ContinueWith(_ => oldCancellation?.Dispose(), TaskScheduler.Default);
                else
                    oldCancellation?.Dispose();
                _listingCancellation = new();
                var token = _listingCancellation.Token;
                _listingDirectory = path;
                _listingHistoryPosition = historyPosition;
                _loading = true;
                _status.Text = Text("loading");
                _accept.Disabled = true;
                _files.MouseFilter = MouseFilterEnum.Ignore;
                _thumbnails.ChangeDirectory();
                _listing = Task.Run(() => ReadDirectory(path, token), token);
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                ShowError(exception.Message);
            }
        }

        private static RitsuFileEntry[] ReadDirectory(string path, CancellationToken cancellation)
        {
            var entries = new List<RitsuFileEntry>();
            foreach (var item in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    var attributes = item.Attributes;
                    var directory = (attributes & FileAttributes.Directory) != 0;
                    entries.Add(new(item.FullName, item.Name, directory,
                        (attributes & FileAttributes.Hidden) != 0 || item.Name.StartsWith('.'),
                        item is FileInfo file ? file.Length : 0, item.LastWriteTimeUtc.Ticks));
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return [.. entries];
        }

        private void CompleteListing()
        {
            if (_listing is not { IsCompleted: true } completed)
                return;
            _listing = null;
            _loading = false;
            _files.MouseFilter = MouseFilterEnum.Stop;
            _listingCancellation?.Dispose();
            _listingCancellation = null;
            try
            {
                _entries = completed.GetAwaiter().GetResult();
                _directory = _listingDirectory;
                _path.Text = _directory;
                if (_listingHistoryPosition is { } position)
                    _historyPosition = position;
                else if (_historyPosition < 0 ||
                         !RitsuFileDialogPreferences.PathComparer.Equals(_history[_historyPosition], _directory))
                {
                    if (_historyPosition + 1 < _history.Count)
                        _history.RemoveRange(_historyPosition + 1, _history.Count - _historyPosition - 1);
                    _history.Add(_directory);
                    if (_history.Count > 128)
                        _history.RemoveAt(0);
                    _historyPosition = _history.Count - 1;
                }

                RitsuFileDialogPreferences.Current.Remember(_options.StateKey, _directory);
                _drives.SetValue(Path.GetPathRoot(_directory) ?? "");
                PopulateFiles();
                RefreshSidebar();
                _back.Disabled = _historyPosition <= 0;
                _forward.Disabled = _historyPosition + 1 >= _history.Count;
            }
            catch (OperationCanceledException)
            {
                RefreshAccept();
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                _path.Text = _directory;
                ShowError(exception.Message);
                RefreshAccept();
            }
        }

        private void PopulateFiles()
        {
            if (_closing)
                return;
            var preferences = RitsuFileDialogPreferences.Current;
            var selected = _files.GetSelectedItems().Where(index => index < _visibleEntries.Length)
                .Select(index => _visibleEntries[index].Path).ToHashSet(RitsuFileDialogPreferences.PathComparer);
            var filter = preferences.ShowNameFilter ? _nameFilter.Text : "";
            _visibleEntries =
            [
                .. _entries.Where(entry =>
                    (preferences.ShowHidden || !entry.Hidden) &&
                    (entry.IsDirectory ||
                     (_options.Mode != FileDialog.FileModeEnum.OpenDir && MatchesFilter(entry.Name))) &&
                    (filter.Length == 0 || entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))),
            ];
            Array.Sort(_visibleEntries, CompareEntries);
            _files.Clear();
            _files.ResetCursor();
            foreach (var entry in _visibleEntries)
            {
                var index = _files.AddItem(entry.Name, entry.IsDirectory ? _folderIcon : _fileIcon);
                _files.SetItemIconModulate(index, RitsuShellTheme.Current.Text.LabelSecondary);
                _files.SetItemTooltip(index, entry.Path);
                if (selected.Contains(entry.Path))
                    _files.Select(index, false);
            }

            _status.Text = _loading ? Text("loading") :
                _visibleEntries.Length == 0 ? Text("empty") :
                string.Format(Text("items"), _visibleEntries.Length);
            RefreshAccept();
            Callable.From(RebuildFocusGraph).CallDeferred();
        }

        private int CompareEntries(RitsuFileEntry left, RitsuFileEntry right)
        {
            if (left.IsDirectory != right.IsDirectory)
                return left.IsDirectory ? -1 : 1;
            var order = RitsuFileDialogPreferences.Current.SortOrder;
            var primary = order switch
            {
                2 or 3 => StringComparer.OrdinalIgnoreCase.Compare(Path.GetExtension(left.Name),
                    Path.GetExtension(right.Name)),
                4 or 5 => right.Modified.CompareTo(left.Modified),
                _ => CompareNames(left.Name, right.Name),
            };
            if ((order & 1) != 0)
                primary = -primary;
            return primary != 0 ? primary : CompareNames(left.Name, right.Name);
        }

        private static int CompareNames(string left, string right)
        {
            var a = 0;
            var b = 0;
            while (a < left.Length && b < right.Length)
            {
                if (char.IsAsciiDigit(left[a]) && char.IsAsciiDigit(right[b]))
                {
                    var aEnd = a;
                    var bEnd = b;
                    while (aEnd < left.Length && char.IsAsciiDigit(left[aEnd])) aEnd++;
                    while (bEnd < right.Length && char.IsAsciiDigit(right[bEnd])) bEnd++;
                    var aStart = a;
                    var bStart = b;
                    while (aStart < aEnd - 1 && left[aStart] == '0') aStart++;
                    while (bStart < bEnd - 1 && right[bStart] == '0') bStart++;
                    var comparison = (aEnd - aStart).CompareTo(bEnd - bStart);
                    if (comparison == 0)
                        comparison = left.AsSpan(aStart, aEnd - aStart)
                            .SequenceCompareTo(right.AsSpan(bStart, bEnd - bStart));
                    if (comparison != 0)
                        return comparison;
                    a = aEnd;
                    b = bEnd;
                    continue;
                }

                var character = char.ToUpperInvariant(left[a++]).CompareTo(char.ToUpperInvariant(right[b++]));
                if (character != 0)
                    return character;
            }

            var remaining = (left.Length - a).CompareTo(right.Length - b);
            return remaining != 0 ? remaining : StringComparer.Ordinal.Compare(left, right);
        }

        private IEnumerable<string> CurrentPatterns()
        {
            return _filterIndex == -1 ? _filterPatterns.SelectMany(patterns => patterns) :
                _filterIndex >= 0 ? _filterPatterns[_filterIndex] : ["*"];
        }

        private bool MatchesFilter(string filename)
        {
            return CurrentPatterns().Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, filename));
        }

        private void OnSelection(int index)
        {
            if (_loading || index < 0 || index >= _visibleEntries.Length)
                return;
            var entry = _visibleEntries[index];
            if (!entry.IsDirectory)
                _filename.Text = entry.Name;
            else if (_options.Mode != FileDialog.FileModeEnum.SaveFile)
                _filename.Text = "";
            RefreshAccept();
        }

        private void ActivateEntry(int index, bool controller = false)
        {
            if (_loading || index < 0 || index >= _visibleEntries.Length)
                return;
            var entry = _visibleEntries[index];
            if (entry.IsDirectory)
            {
                if (_options.Mode != FileDialog.FileModeEnum.SaveFile)
                    _filename.Text = "";
                Navigate(entry.Path);
                return;
            }

            switch (_options.Mode)
            {
                case FileDialog.FileModeEnum.OpenDir:
                    return;
                case FileDialog.FileModeEnum.OpenFiles when controller:
                    if (_files.IsSelected(index))
                        _files.Deselect(index);
                    else
                        _files.Select(index, false);
                    OnSelection(index);
                    return;
            }

            _filename.Text = entry.Name;
            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            if (_loading || _closing)
                return;
            try
            {
                var selected = _files.GetSelectedItems().Where(index => index < _visibleEntries.Length)
                    .Select(index => _visibleEntries[index]).ToArray();
                switch (_options.Mode)
                {
                    case FileDialog.FileModeEnum.OpenDir:
                    {
                        var directory = selected.FirstOrDefault(entry => entry.IsDirectory)?.Path ?? _directory;
                        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
                        Finish([directory]);
                        return;
                    }
                    case FileDialog.FileModeEnum.OpenFiles:
                    {
                        var paths = selected.Where(entry => !entry.IsDirectory).Select(entry => entry.Path).ToArray();
                        if (paths.Length == 0) return;
                        foreach (var path in paths)
                            if (!File.Exists(path))
                                throw new FileNotFoundException(Text("missing"), path);
                        Finish(paths);
                        return;
                    }
                    case FileDialog.FileModeEnum.OpenAny when
                        selected.FirstOrDefault()?.IsDirectory == true || string.IsNullOrWhiteSpace(_filename.Text):
                    {
                        var directory = selected.FirstOrDefault()?.Path ?? _directory;
                        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
                        Finish([directory]);
                        return;
                    }
                }

                var filename = _filename.Text.Trim();
                if (filename.Length == 0)
                {
                    ShowError(Text("invalidName"));
                    return;
                }

                var result = Path.GetFullPath(filename, _directory);
                if (Directory.Exists(result))
                {
                    Navigate(result);
                    return;
                }

                if (_options.Mode == FileDialog.FileModeEnum.SaveFile)
                {
                    if (!IsValidLeafName(Path.GetFileName(result)))
                    {
                        ShowError(Text("invalidName"));
                        return;
                    }

                    if (!MatchesFilter(Path.GetFileName(result)))
                    {
                        var pattern = CurrentPatterns().FirstOrDefault();
                        if (pattern == null || !pattern.StartsWith("*.") || pattern[1..].IndexOfAny(['*', '?']) >= 0)
                        {
                            ShowError(Text("invalidExtension"));
                            return;
                        }

                        result += pattern[1..];
                        _filename.Text = Path.GetFileName(result);
                    }

                    if (!Directory.Exists(Path.GetDirectoryName(result)))
                        throw new DirectoryNotFoundException(Path.GetDirectoryName(result));
                    if (File.Exists(result))
                        ShowPrompt(Text("overwriteTitle"), string.Format(Text("overwriteBody"), result),
                            Text("replace"), () => Finish([result]));
                    else
                        Finish([result]);
                }
                else
                {
                    if (!File.Exists(result)) throw new FileNotFoundException(Text("missing"), result);
                    Finish([result]);
                }
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                ShowError(exception.Message);
            }
        }

        private void RefreshAccept()
        {
            _accept.Disabled = _loading || _directory.Length == 0 ||
                               (_options.Mode == FileDialog.FileModeEnum.OpenFiles && _files.GetSelectedItems()
                                   .All(index =>
                                       index >= _visibleEntries.Length || _visibleEntries[index].IsDirectory)) ||
                               (_options.Mode is FileDialog.FileModeEnum.OpenFile or FileDialog.FileModeEnum.SaveFile &&
                                string.IsNullOrWhiteSpace(_filename.Text));
        }

        private void ShowError(string message)
        {
            _status.Text = message;
        }

        private void GoUp()
        {
            if (_directory.Length == 0)
                return;
            var parent = Directory.GetParent(_directory)?.FullName;
            if (parent != null)
                Navigate(parent);
        }

        private void TraverseHistory(int delta)
        {
            var index = _historyPosition + delta;
            if (index >= 0 && index < _history.Count)
                Navigate(_history[index], index);
        }

        private void RefreshThumbnails()
        {
            if (_visibleEntries.Length == 0 || !_files.IsVisibleInTree())
                return;
            var first = Math.Max(0, _files.GetItemAtPosition(new(4, 4)) - 8);
            var last = Math.Min(_visibleEntries.Length - 1,
                _files.GetItemAtPosition(_files.Size - new Vector2(24, 8)) + 8);
            for (var index = first; index <= last; index++)
            {
                if (_thumbnails.Get(_visibleEntries[index]) is not { } texture)
                    continue;
                _files.SetItemIcon(index, texture);
                _files.SetItemIconModulate(index, Colors.White);
            }
        }

        private void ResetThumbnail(string key)
        {
            for (var index = 0; index < _visibleEntries.Length; index++)
                if (RitsuFileThumbnailCache.Key(_visibleEntries[index]) == key)
                {
                    _files.SetItemIcon(index, _fileIcon);
                    _files.SetItemIconModulate(index, RitsuShellTheme.Current.Text.LabelSecondary);
                }
        }
    }
}
