namespace STS2RitsuLib.Ui.Files
{
    public sealed partial class RitsuFileDialog
    {
        private void RefreshSidebar()
        {
            var preferences = RitsuFileDialogPreferences.Current;
            PopulateSidebar(_favorites, preferences.Favorites);
            PopulateSidebar(_recent, preferences.Recent);
            RefreshToggleButtons();
            return;

            void PopulateSidebar(RitsuFileList list, List<string> paths)
            {
                list.Clear();
                list.ResetCursor();
                foreach (var path in paths)
                {
                    var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
                    var index = list.AddItem(string.IsNullOrEmpty(name) ? path : name, _folderIcon);
                    list.SetItemTooltip(index, path);
                    if (RitsuFileDialogPreferences.PathComparer.Equals(path, _directory))
                        list.Select(index);
                }
            }
        }

        private void NavigateFavorite(int index)
        {
            var paths = RitsuFileDialogPreferences.Current.Favorites;
            if ((uint)index < paths.Count)
                Navigate(paths[index]);
        }

        private void NavigateRecent(int index)
        {
            var paths = RitsuFileDialogPreferences.Current.Recent;
            if ((uint)index < paths.Count)
                Navigate(paths[index]);
        }

        private void ToggleFavorite()
        {
            if (string.IsNullOrEmpty(_directory))
                return;
            var preferences = RitsuFileDialogPreferences.Current;
            var index = preferences.Favorites.FindIndex(path =>
                RitsuFileDialogPreferences.PathComparer.Equals(path, _directory));
            if (index >= 0)
                preferences.Favorites.RemoveAt(index);
            else if (preferences.Favorites.Count < 128)
                preferences.Favorites.Add(_directory);
            else
            {
                ShowError(Text("favoritesFull"));
                return;
            }

            preferences.Save();
            RefreshSidebar();
        }

        private void MoveFavorite(int direction)
        {
            var selected = _favorites.GetSelectedItems();
            if (selected.Length == 0)
                return;
            var preferences = RitsuFileDialogPreferences.Current;
            var index = selected[0];
            var next = index + direction;
            if ((uint)next >= preferences.Favorites.Count)
                return;
            (preferences.Favorites[index], preferences.Favorites[next]) =
                (preferences.Favorites[next], preferences.Favorites[index]);
            preferences.Save();
            RefreshSidebar();
            _favorites.Select(next);
        }

        private void ToggleHidden()
        {
            var preferences = RitsuFileDialogPreferences.Current;
            preferences.ShowHidden = !preferences.ShowHidden;
            preferences.Save();
            RefreshToggleButtons();
            PopulateFiles();
        }

        private void SetDisplayMode(bool thumbnails)
        {
            var preferences = RitsuFileDialogPreferences.Current;
            preferences.Thumbnails = thumbnails;
            preferences.Save();
            _files.SetGrid(thumbnails);
            RefreshToggleButtons();
            PopulateFiles();
        }

        private void ToggleNameFilter()
        {
            var preferences = RitsuFileDialogPreferences.Current;
            preferences.ShowNameFilter = !preferences.ShowNameFilter;
            preferences.Save();
            _nameFilterRow.Visible = preferences.ShowNameFilter;
            RefreshToggleButtons();
            PopulateFiles();
            if (preferences.ShowNameFilter)
                _nameFilter.GrabFocus();
            else
                _files.GrabFocus();
        }

        private void RefreshToggleButtons()
        {
            var preferences = RitsuFileDialogPreferences.Current;
            _favorite.SetSelected(preferences.Favorites.Contains(_directory,
                RitsuFileDialogPreferences.PathComparer));
            _hidden.SetSelected(preferences.ShowHidden);
            _grid.SetSelected(preferences.Thumbnails);
            _list.SetSelected(!preferences.Thumbnails);
            _filterToggle.SetSelected(preferences.ShowNameFilter);
            ApplyIconButtonStyles();
        }
    }
}
