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
                    .WithTitle(T("ritsulib.page.searchExtensions.title", "Search matching"))
                    .WithDescription(T("ritsulib.page.searchExtensions.description",
                        "Enable optional ways to find text, such as Mandarin pinyin."))
                    .AddSection("built_in_search_providers", section => section
                        .WithTitle(T("ritsulib.searchExtensions.builtIn.title", "Built-in options"))
                        .WithDescription(T("ritsulib.searchExtensions.builtIn.description",
                            "Choose an option to set it up or change its settings."))
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
                        .WithTitle(T("ritsulib.searchExtensions.additional.title", "Added by mods"))
                        .WithDescription(T("ritsulib.searchExtensions.additional.description",
                            "Enable additional search options provided by other mods."))
                        .WithVisibleWhen(HasAdditionalSearchProviders)
                        .AddCustom(
                            "additional_provider_toggles",
                            T("ritsulib.searchExtensions.additional.label", "Available options"),
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
                    $"{provider.DisplayName}\n{FormatSearchProviderAudit(provider.Id, provider.ModId)}",
                    toggle));
            }

            return container;
        }

        private static string FormatPinyinOverview()
        {
            var status = PinyinSearchDataManager.GetStatus();
            string summary;
            if (status.IsBusy)
                summary = L("ritsulib.searchExtensions.pinyin.overview.busy",
                    "Working...");
            else if (PinyinSearchDataManager.Data == null)
                summary = L("ritsulib.searchExtensions.pinyin.overview.uninitialized",
                    "Not set up. Select Initialize to use pinyin search.");
            else
                summary = RitsuSearchSettingsStore.IsProviderEnabled(PinyinSearchExpansionProvider.ProviderId, false)
                    ? L("ritsulib.searchExtensions.pinyin.overview.enabled", "Ready and enabled")
                    : L("ritsulib.searchExtensions.pinyin.overview.disabled", "Ready but disabled");

            return $"{summary}\n{FormatSearchProviderAudit(PinyinSearchExpansionProvider.ProviderId, Const.ModId)}";
        }

        private static string FormatSearchProviderAudit(string providerId, string ownerModId)
        {
            return
                $"{L("ritsulib.searchExtensions.audit.providerId", "Provider ID")}: {providerId} · " +
                $"{L("ritsulib.searchExtensions.audit.ownerModId", "Mod ID")}: {ownerModId}";
        }
    }
}
