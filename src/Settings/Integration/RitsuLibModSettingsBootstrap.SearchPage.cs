using Godot;
using STS2RitsuLib.Search;
using STS2RitsuLib.Search.Pinyin;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterSearchSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-985)
                    .WithTitle(T("ritsulib.page.searchExtensions.title", "Search providers"))
                    .WithDescription(T("ritsulib.page.searchExtensions.description",
                        "Choose optional local-search transliteration providers. Each built-in provider manages its own data and behavior."))
                    .AddSection("built_in_search_providers", section => section
                        .WithTitle(T("ritsulib.searchExtensions.builtIn.title", "Built-in providers"))
                        .WithDescription(T("ritsulib.searchExtensions.builtIn.description",
                            "Open a provider to initialize it and manage its settings."))
                        .AddButton(
                            "pinyin_provider_open",
                            T("ritsulib.searchProviders.pinyin.name", "Mandarin pinyin"),
                            ModSettingsText.Dynamic(() => PinyinSearchDataManager.Data == null
                                ? L("ritsulib.searchExtensions.pinyin.initialize.button", "Initialize...")
                                : L("button.open", "Open")),
                            OpenOrInitializePinyin,
                            ModSettingsButtonTone.Accent,
                            ModSettingsText.Dynamic(FormatPinyinOverview))
                        .WithEntryEnabledWhen(
                            "pinyin_provider_open",
                            static () => !PinyinSearchDataManager.GetStatus().IsBusy))
                    .AddSection("additional_search_providers", section => section
                        .WithTitle(T("ritsulib.searchExtensions.additional.title", "Additional providers"))
                        .WithDescription(T("ritsulib.searchExtensions.additional.description",
                            "Providers registered by other mods can be enabled independently."))
                        .WithVisibleWhen(HasAdditionalSearchProviders)
                        .AddCustom(
                            "additional_provider_toggles",
                            T("ritsulib.searchExtensions.additional.label", "Registered providers"),
                            CreateAdditionalProviderControls)),
                "search-expansions");

            RegisterPinyinSearchSettingsPage(ui);
        }

        private static void OpenOrInitializePinyin(IModSettingsUiActionHost host)
        {
            if (PinyinSearchDataManager.Data == null)
            {
                RequestPinyinInitialization(host);
                return;
            }

            if (host is ModSettingsUiContext context)
                context.NavigateToPage("search-expansions-pinyin");
        }

        private static bool HasAdditionalSearchProviders()
        {
            return RitsuSearchExpansionRegistry.GetProviderSnapshots().Any(static provider =>
                !string.Equals(provider.Id, PinyinSearchExpansionProvider.ProviderId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static Control CreateAdditionalProviderControls(IModSettingsUiActionHost host)
        {
            var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            container.AddThemeConstantOverride("separation", 8);
            foreach (var provider in RitsuSearchExpansionRegistry.GetProviderSnapshots().Where(static provider =>
                         !string.Equals(provider.Id, PinyinSearchExpansionProvider.ProviderId,
                             StringComparison.OrdinalIgnoreCase)))
            {
                var toggle = ModSettingsUiControlTheming.CreateCompactStateToggle(
                    provider.Enabled,
                    enabled =>
                    {
                        RitsuSearchSettingsStore.SetProviderEnabled(provider.Id, enabled);
                        host.RequestRefresh();
                    });
                container.AddChild(ModSettingsUiControlTheming.CreateCompactToggleField(
                    $"{provider.DisplayName}\n{provider.Id} · {provider.ModId}",
                    toggle));
            }

            return container;
        }

        private static string FormatPinyinOverview()
        {
            var status = PinyinSearchDataManager.GetStatus();
            if (status.IsBusy)
                return L("ritsulib.searchExtensions.pinyin.overview.busy",
                    "Initialization or maintenance is in progress.");
            if (PinyinSearchDataManager.Data == null)
                return L("ritsulib.searchExtensions.pinyin.overview.uninitialized",
                    "Not initialized. Open to review the source and download the required data.");

            return RitsuSearchSettingsStore.IsProviderEnabled(PinyinSearchExpansionProvider.ProviderId, false)
                ? string.Format(
                    L("ritsulib.searchExtensions.pinyin.overview.enabled", "Enabled · Unicode {0}"),
                    status.UnicodeVersion)
                : string.Format(
                    L("ritsulib.searchExtensions.pinyin.overview.disabled", "Ready but disabled · Unicode {0}"),
                    status.UnicodeVersion);
        }
    }
}
