import {createI18n} from "vue-i18n";
import enUS from "./locales/en-US.json";
import zhCN from "./locales/zh-CN.json";

export type ViewerLocale = "zh-CN" | "en-US";

export const viewerLocales: readonly ViewerLocale[] = ["zh-CN", "en-US"];

export function resolveViewerLocale(value: string | null | undefined): ViewerLocale {
    if (value === "zh" || value === "zh-CN")
        return "zh-CN";
    if (value === "en" || value === "en-US")
        return "en-US";

    return navigator.language.toLowerCase().startsWith("zh") ? "zh-CN" : "en-US";
}

export const i18n = createI18n({
    legacy: false,
    locale: resolveViewerLocale(localStorage.getItem("ritsulib-log-viewer:locale")),
    fallbackLocale: "en-US",
    messages: {
        "en-US": enUS,
        "zh-CN": zhCN
    }
});
