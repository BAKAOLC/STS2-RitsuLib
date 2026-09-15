using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib
{
    internal sealed class RitsuLibModuleHost : IRitsuModuleHost, IRitsuSettingsHost
    {
        internal static readonly RitsuLibModuleHost Instance = new();

        public StringName ConfirmAction => Sts2InputRuntime.ConfirmAction;
        public StringName CancelCardPlayAction => Sts2InputRuntime.CancelCardPlayAction;
        public bool IsUsingDirectionalNavigation => Sts2InputRuntime.IsUsingDirectionalNavigation;
        public bool IsUsingController => Sts2InputRuntime.IsUsingController;

        public IDisposable SubscribeLifecycle(ILifecycleObserver observer, bool replayCurrentState)
        {
            return RitsuLibFramework.SubscribeLifecycle(observer, replayCurrentState);
        }

        public IDisposable SubscribeLifecycle<TEvent>(Action<TEvent> handler, bool replayCurrentState)
            where TEvent : IFrameworkLifecycleEvent
        {
            return RitsuLibFramework.SubscribeLifecycle(handler, replayCurrentState);
        }

        public void PublishLifecycleEvent<TEvent>(TEvent evt, string phase)
            where TEvent : IFrameworkLifecycleEvent
        {
            RitsuLibFramework.PublishLifecycleEvent(evt, phase);
        }

        public void EnsureProfileServicesInitialized()
        {
            RitsuLibFramework.EnsureProfileServicesInitialized();
        }

        public int RegisterModDataProviders()
        {
            return ModDataRuntimeInterop.TryRegisterAll();
        }

        public void PushModDataProviders()
        {
            ModDataRuntimeInterop.PushLoadedDataToAllProviders();
        }

        public void MirrorModDataFile(string path)
        {
            ModDataCloudMirror.MirrorLocalFileAfterWriteIfEnabled(path);
        }

        public bool TryOpenSettings(out RitsuModSettingsSubmenu submenu, out string error)
        {
            return RitsuOverlayHostService.TryOpenSettings(out submenu, out error);
        }

        public IEnumerable<Mod> EnumerateModsForManifestLookup()
        {
            return Sts2ModManagerCompat.EnumerateModsForManifestLookup();
        }

        public bool TryGetBestModPresentationInfo(string modId, out RitsuModPresentationInfo? info)
        {
            return Sts2ModManagerCompat.TryGetBestModPresentationInfo(modId, out info);
        }

        public bool TryGetBestModPresentationInfoForAssembly(Assembly assembly, out RitsuModPresentationInfo? info)
        {
            return Sts2ModManagerCompat.TryGetBestModPresentationInfoForAssembly(assembly, out info);
        }

        public string NormalizePublicStem(string value)
        {
            return ModContentRegistry.NormalizePublicStem(value);
        }

        public void EnsureFrameworkPagesRegistered()
        {
            RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
        }

        public void RefreshDynamicPages()
        {
            RitsuLibModSettingsBootstrap.RefreshDynamicPages();
        }
    }
}
