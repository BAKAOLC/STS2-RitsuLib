namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorRegistrarBootstrap
    {
        private static readonly Lock BackgroundPrewarmLock = new();

        private static volatile bool _backgroundPrewarmComplete;
        private static ModSettingsMirrorPrewarmSession? _backgroundPrewarmSession;

        public static bool IsBackgroundPrewarmComplete => _backgroundPrewarmComplete;

        internal static void MarkBackgroundPrewarmComplete()
        {
            _backgroundPrewarmComplete = true;
        }

        public static int TryRegisterMirroredPages()
        {
            var added = 0;
            added += BaseLibMirrorSource.TryRegisterMirroredPages();
            added += ModConfigMirrorSource.TryRegisterMirroredPages();
            added += RuntimeInteropMirrorSource.TryRegisterMirroredPages();
            added += RuntimeReflectionMirrorSource.TryRegisterMirroredPages();
            added += BaseLibToRitsuGeneratedMirrorSource.TryRegisterMirroredPages();
            return added;
        }

        public static ModSettingsMirrorPrewarmSession CreatePrewarmSession()
        {
            return new([
                new BackgroundPrewarmWork(() => BaseLibMirrorSource.TryRegisterMirroredPages()),
                new BackgroundPrewarmWork(() => ModConfigMirrorSource.TryRegisterMirroredPages()),
                new BackgroundPrewarmWork(RuntimeInteropMirrorSource.TryRegisterMirroredPages),
                new BackgroundPrewarmWork(RuntimeReflectionMirrorSource.TryRegisterMirroredPages),
                new BackgroundPrewarmWork(() => BaseLibToRitsuGeneratedMirrorSource.TryRegisterMirroredPages()),
            ]);
        }

        public static ModSettingsMirrorPrewarmSession GetOrCreateBackgroundPrewarmSession()
        {
            lock (BackgroundPrewarmLock)
            {
                return _backgroundPrewarmSession ??= CreatePrewarmSession();
            }
        }

        private sealed class BackgroundPrewarmWork(Func<int> register) : IModSettingsMirrorPrewarmWork
        {
            private volatile bool _complete;
            private Exception? _error;
            private bool _reportedFailure;
            private int _result;
            private bool _started;
            private Thread? _thread;

            public bool IsComplete => _complete;

            public int Resume()
            {
                if (!_started)
                    StartWorker();

                if (!_complete)
                    return 0;

                if (_error == null) return _result;
                if (_reportedFailure) return 0;
                _reportedFailure = true;
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Mod settings background prewarm failed: {_error.Message}");

                return 0;
            }

            private void StartWorker()
            {
                _started = true;
                _thread = new(WorkerMain)
                {
                    IsBackground = true,
                    Name = "RitsuLib Mod Settings Prewarm",
                    Priority = ThreadPriority.Lowest,
                };
                _thread.Start();
            }

            private void WorkerMain()
            {
                try
                {
                    try
                    {
                        Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    }
                    catch
                    {
                        // Best effort only.
                    }

                    _result = register();
                }
                catch (Exception ex)
                {
                    _error = ex;
                }
                finally
                {
                    _complete = true;
                }
            }
        }
    }

    internal interface IModSettingsMirrorPrewarmWork
    {
        bool IsComplete { get; }

        int Resume();
    }

    internal sealed class ModSettingsMirrorPrewarmSession(IReadOnlyList<IModSettingsMirrorPrewarmWork> workItems)
    {
        private readonly Lock _sync = new();
        private int _added;
        private int _workIndex;

        public int Added
        {
            get
            {
                lock (_sync)
                {
                    return _added;
                }
            }
        }

        public bool IsComplete
        {
            get
            {
                lock (_sync)
                {
                    return IsCompleteLocked();
                }
            }
        }

        public bool Resume()
        {
            lock (_sync)
            {
                if (IsCompleteLocked())
                    return true;

                while (_workIndex < workItems.Count)
                {
                    var work = workItems[_workIndex];
                    _added += work.Resume();
                    if (!work.IsComplete)
                        return false;

                    _workIndex++;
                }

                ModSettingsMirrorRegistrarBootstrap.MarkBackgroundPrewarmComplete();
                return true;
            }
        }

        private bool IsCompleteLocked()
        {
            return _workIndex >= workItems.Count;
        }
    }
}
