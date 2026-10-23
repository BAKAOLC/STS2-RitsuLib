namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsMirrorRegistrationRunner
    {
        private readonly HashSet<string> _pendingProviders = new(StringComparer.Ordinal);
        private bool _running;

        // The caller holds its registration lock throughout this operation.
        internal int Run(string? providerName, Func<string?, int> register)
        {
            if (providerName != null)
                _pendingProviders.Add(providerName);
            if (_running)
                return 0;

            _running = true;
            try
            {
                var attempted = new HashSet<string>(StringComparer.Ordinal);
                var added = providerName == null ? register(null) : 0;
                while (_pendingProviders.Count > 0)
                {
                    var providers = _pendingProviders.ToArray();
                    _pendingProviders.Clear();
                    foreach (var name in providers)
                        if (attempted.Add(name))
                            added += register(name);
                }

                return added;
            }
            finally
            {
                _pendingProviders.Clear();
                _running = false;
            }
        }
    }
}
