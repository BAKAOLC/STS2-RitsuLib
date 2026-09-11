using Godot;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Data;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib
{
    internal interface IRitsuModuleHost
    {
        StringName ConfirmAction { get; }
        StringName CancelCardPlayAction { get; }
        bool IsUsingDirectionalNavigation { get; }
        bool IsUsingController { get; }
        IDisposable SubscribeLifecycle(ILifecycleObserver observer, bool replayCurrentState);

        IDisposable SubscribeLifecycle<TEvent>(Action<TEvent> handler, bool replayCurrentState)
            where TEvent : IFrameworkLifecycleEvent;

        void PublishLifecycleEvent<TEvent>(TEvent evt, string phase) where TEvent : IFrameworkLifecycleEvent;
        void EnsureProfileServicesInitialized();
        int RegisterModDataProviders();
        void PushModDataProviders();
        void MirrorModDataFile(string path);
    }

    internal static class RitsuModuleRuntime
    {
        private static IRitsuModuleHost? _host;

        internal static Logger Logger { get; } = CreateLogger(Const.ModId);

        internal static IRitsuModuleHost Host => Volatile.Read(ref _host) ??
                                                 throw new InvalidOperationException(
                                                     "The RitsuLib runtime has not attached its shared module services.");

        internal static void Attach(IRitsuModuleHost host)
        {
            ArgumentNullException.ThrowIfNull(host);
            var existing = Interlocked.CompareExchange(ref _host, host, null);
            if (existing != null && !ReferenceEquals(existing, host))
                throw new InvalidOperationException("RitsuLib shared services already belong to another runtime.");
        }

        internal static Logger CreateLogger(string modId, LogType logType = LogType.Generic)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId, logType);
        }

        internal static ModDataStore GetDataStore(string modId)
        {
            return ModDataStore.For(modId);
        }

        internal static IDisposable BeginModDataRegistration(string modId, bool initializeProfileIfReady = true)
        {
            return GetDataStore(modId).BeginRegistrationScope(initializeProfileIfReady);
        }

        internal static IDisposable SubscribeLifecycle(ILifecycleObserver observer, bool replayCurrentState = true)
        {
            return Host.SubscribeLifecycle(observer, replayCurrentState);
        }

        internal static IDisposable SubscribeLifecycle<TEvent>(Action<TEvent> handler, bool replayCurrentState = true)
            where TEvent : IFrameworkLifecycleEvent
        {
            return Host.SubscribeLifecycle(handler, replayCurrentState);
        }

        internal static void PublishLifecycleEvent<TEvent>(TEvent evt, string phase)
            where TEvent : IFrameworkLifecycleEvent
        {
            Host.PublishLifecycleEvent(evt, phase);
        }

        internal static void EnsureProfileServicesInitialized()
        {
            Host.EnsureProfileServicesInitialized();
        }

        internal static int RegisterModDataProviders()
        {
            return Host.RegisterModDataProviders();
        }

        internal static void PushModDataProviders()
        {
            Host.PushModDataProviders();
        }

        internal static void MirrorModDataFile(string path)
        {
            Host.MirrorModDataFile(path);
        }

        internal static void ErrorNoTrace(Logger logger, string text)
        {
            ArgumentNullException.ThrowIfNull(logger);
            if (!logger.WillLog(LogLevel.Error))
                return;
            var formattedText = logger.Context != null ? $"[{logger.Context}] {text}" : text;
            GD.PrintErr($"[ERROR] {formattedText}");
        }
    }
}
