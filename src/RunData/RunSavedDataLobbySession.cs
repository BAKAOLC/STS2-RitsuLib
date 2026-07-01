namespace STS2RitsuLib.RunData
{
    internal sealed class RunSavedDataLobbySession
    {
        private readonly Dictionary<RunSavedDataSlotKey, Dictionary<ulong, object>> _playerValues = [];
        private readonly Dictionary<RunSavedDataSlotKey, object> _runValues = [];

        public bool TryGetRun(RunSavedDataSlotKey key, out object? value)
        {
            return _runValues.TryGetValue(key, out value);
        }

        public void SetRun(RunSavedDataSlotKey key, object value)
        {
            _runValues[key] = value;
        }

        public bool RemoveRun(RunSavedDataSlotKey key)
        {
            return _runValues.Remove(key);
        }

        public bool TryGetPlayer(RunSavedDataSlotKey key, ulong netId, out object? value)
        {
            value = null;
            return _playerValues.TryGetValue(key, out var players) && players.TryGetValue(netId, out value);
        }

        public bool HasPlayers(RunSavedDataSlotKey key)
        {
            return _playerValues.TryGetValue(key, out var players) && players.Count > 0;
        }

        public void SetPlayer(RunSavedDataSlotKey key, ulong netId, object value)
        {
            if (!_playerValues.TryGetValue(key, out var players))
            {
                players = [];
                _playerValues[key] = players;
            }

            players[netId] = value;
        }

        public bool RemovePlayer(RunSavedDataSlotKey key, ulong netId)
        {
            if (!_playerValues.TryGetValue(key, out var players))
                return false;

            var removed = players.Remove(netId);
            if (players.Count == 0)
                _playerValues.Remove(key);
            return removed;
        }

        public bool RemovePlayer(ulong netId)
        {
            var removed = false;
            foreach (var key in _playerValues.Keys.ToArray())
            {
                var players = _playerValues[key];
                removed |= players.Remove(netId);
                if (players.Count == 0)
                    _playerValues.Remove(key);
            }

            return removed;
        }

        public bool RemovePlayersExcept(HashSet<ulong> activeNetIds)
        {
            var removed = false;
            foreach (var key in _playerValues.Keys.ToArray())
            {
                var players = _playerValues[key];
                removed = players.Keys.ToArray().Where(netId => !activeNetIds.Contains(netId))
                    .Aggregate(removed, (current, netId) => current | players.Remove(netId));

                if (players.Count == 0)
                    _playerValues.Remove(key);
            }

            return removed;
        }

        public IEnumerable<KeyValuePair<RunSavedDataSlotKey, object>> RunEntries()
        {
            return _runValues;
        }

        public IEnumerable<KeyValuePair<RunSavedDataSlotKey, Dictionary<ulong, object>>> PlayerEntries()
        {
            return _playerValues;
        }

        public void Clear()
        {
            _runValues.Clear();
            _playerValues.Clear();
        }
    }
}
