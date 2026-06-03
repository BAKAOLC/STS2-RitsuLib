import type {NoiseRule} from "./logTypes";

export const storagePrefix = "ritsulib-log-viewer:";

export const logLevels = ["VERYDEBUG", "LOAD", "DEBUG", "INFO", "WARN", "ERROR"] as const;

export const columnOptions = [
    {id: "time", labelKey: "colTime", tipKey: "tipColTime"},
    {id: "level", labelKey: "colLevel", tipKey: "tipColLevel"},
    {id: "origin", labelKey: "colOrigin", tipKey: "tipColOrigin"},
    {id: "message", labelKey: "colMessage", tipKey: "tipColMessage"},
    {id: "id", labelKey: "colId", tipKey: "tipColId"}
] as const;

export type ColumnId = typeof columnOptions[number]["id"];

export const defaultNoiseRules: NoiseRule[] = [
    {pattern: "AtlasResourceLoader: Missing sprite", enabled: true, tipKey: "noiseAtlas"},
    {pattern: "Asset not cached:", enabled: true, tipKey: "noiseAssetCache"},
    {pattern: "[Assets] Missing resource path", enabled: true, tipKey: "noiseMissingResource"},
    {pattern: "Found mod manifest file", enabled: true, tipKey: "noiseManifestScan"},
    {pattern: "missing the 'id' field", enabled: true, tipKey: "noiseMissingId"},
    {pattern: "warmup job failed", enabled: true, tipKey: "noiseWarmup"},
    {pattern: "Limiting background FPS", enabled: true, tipKey: "noiseBackgroundFps"},
    {pattern: "Restored foreground FPS", enabled: true, tipKey: "noiseForegroundFps"}
];

export function levelLabel(level: string) {
    return level === "VERYDEBUG" ? "VDB" : level;
}
