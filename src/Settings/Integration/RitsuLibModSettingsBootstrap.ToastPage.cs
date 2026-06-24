namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterToastSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-850)
                    .WithTitle(T("ritsulib.page.toast.title", "Toast notifications"))
                    .WithDescription(T("ritsulib.page.toast.description",
                        "Configure stack placement, queue limits, and animation for global toast notifications."))
                    .AddSection("toast", section => section
                        .WithTitle(T("ritsulib.section.toast.title", "Toast notifications"))
                        .WithDescription(T("ritsulib.section.toast.description",
                            "Configure stack placement, queue limits, and animation for global toast notifications."))
                        .AddParagraph(
                            "toast_anchor_offset_guide",
                            T("ritsulib.toast.anchorOffset.guide",
                                "Anchor picks where the newest toast starts. Offsets shift that anchor in screen pixels before viewport clamping."))
                        .AddToggle(
                            "toast_enabled",
                            T("ritsulib.toast.enabled.label", "Enable toast notifications"),
                            ui.ToastEnabled,
                            T("ritsulib.toast.enabled.description",
                                "Global switch for non-blocking toast notifications."))
                        .AddChoice(
                            "toast_anchor",
                            T("ritsulib.toast.anchor.label", "Toast position"),
                            ui.ToastAnchor,
                            [
                                new("topleft", T("ritsulib.toast.anchor.topleft", "Top Left")),
                                new("topcenter", T("ritsulib.toast.anchor.topcenter", "Top Center")),
                                new("topright", T("ritsulib.toast.anchor.topright", "Top Right")),
                                new("middleleft", T("ritsulib.toast.anchor.middleleft", "Middle Left")),
                                new("middlecenter", T("ritsulib.toast.anchor.middlecenter", "Middle Center")),
                                new("middleright", T("ritsulib.toast.anchor.middleright", "Middle Right")),
                                new("bottomleft", T("ritsulib.toast.anchor.bottomleft", "Bottom Left")),
                                new("bottomcenter", T("ritsulib.toast.anchor.bottomcenter", "Bottom Center")),
                                new("bottomright", T("ritsulib.toast.anchor.bottomright", "Bottom Right")),
                            ],
                            T("ritsulib.toast.anchor.description",
                                "Select where the newest toast is anchored before stack expansion."),
                            ModSettingsChoicePresentation.Dropdown)
                        .AddSlider(
                            "toast_offset_x",
                            T("ritsulib.toast.offsetX.label", "Horizontal offset"),
                            ui.ToastOffsetX,
                            -600d,
                            600d,
                            1d,
                            value => value.ToString("0"),
                            T("ritsulib.toast.offsetX.description",
                                "Shift the anchor on X before clamping. Negative moves left, positive moves right."))
                        .AddSlider(
                            "toast_offset_y",
                            T("ritsulib.toast.offsetY.label", "Vertical offset"),
                            ui.ToastOffsetY,
                            -450d,
                            450d,
                            1d,
                            value => value.ToString("0"),
                            T("ritsulib.toast.offsetY.description",
                                "Shift the anchor on Y before clamping. Negative moves up, positive moves down."))
                        .AddIntSlider(
                            "toast_max_visible",
                            T("ritsulib.toast.maxVisible.label", "Max visible toasts"),
                            ui.ToastMaxVisible,
                            1,
                            8,
                            1,
                            value => value.ToString(),
                            T("ritsulib.toast.maxVisible.description",
                                "Maximum toasts shown at once. Extra items queue and appear in order."))
                        .AddSlider(
                            "toast_duration_seconds",
                            T("ritsulib.toast.duration.label", "Default duration (seconds)"),
                            ui.ToastDurationSeconds,
                            0.5d,
                            30d,
                            0.25d,
                            value => value.ToString("0.##"),
                            T("ritsulib.toast.duration.description",
                                "Default display duration for toasts without per-request overrides."))
                        .AddChoice(
                            "toast_animation",
                            T("ritsulib.toast.animation.label", "Animation preset"),
                            ui.ToastAnimation,
                            [
                                new("fade", T("ritsulib.toast.animation.fade", "Fade")),
                                new("fadeslide", T("ritsulib.toast.animation.fadeslide", "Fade + Slide")),
                                new("fadescale", T("ritsulib.toast.animation.fadescale", "Fade + Scale")),
                            ],
                            T("ritsulib.toast.animation.description", "Applies to enter/exit animation of new toasts."),
                            ModSettingsChoicePresentation.Dropdown)),
                "toast");
        }
    }
}
