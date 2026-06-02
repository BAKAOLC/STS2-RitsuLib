<script lang="ts" setup>
import {useVirtualizer} from "@tanstack/vue-virtual";
import {type ComponentPublicInstance, computed, nextTick, onMounted, ref, watch} from "vue";

type LogRecord = {
  id: number;
  timestamp: string;
  severityText: string;
  severityNumber: number;
  body: string;
  source?: string;
  category?: string;
  loggerName?: string;
  attributes?: Record<string, unknown>;
  resource?: Record<string, unknown>;
  scope?: Record<string, unknown>;
};

type Status = {
  enabled: boolean;
  url?: string;
  bufferCount: number;
  bufferCapacity: number;
  queueDepth: number;
  queueCapacity: number;
  dropped: number;
};

type ThemeMode = "dark" | "light";
type Locale = "zh" | "en";
type ColumnWidths = Record<string, number>;
type ConnectionState = "connecting" | "connected" | "reconnecting";
type NoiseRule = {
  pattern: string;
  enabled: boolean;
  tipKey?: string;
};

const storagePrefix = "ritsulib-log-viewer:";
const params = new URLSearchParams(location.search);
const token = params.get("token") ?? "";

const levels = ["VERYDEBUG", "LOAD", "DEBUG", "INFO", "WARN", "ERROR"];
const columnOptions = [
  {id: "time", labelKey: "colTime", tipKey: "tipColTime"},
  {id: "level", labelKey: "colLevel", tipKey: "tipColLevel"},
  {id: "origin", labelKey: "colOrigin", tipKey: "tipColOrigin"},
  {id: "message", labelKey: "colMessage", tipKey: "tipColMessage"},
  {id: "id", labelKey: "colId", tipKey: "tipColId"}
];
const columnDefaults: ColumnWidths = {
  id: 66,
  time: 90,
  level: 82,
  origin: 240,
  message: 720
};
const columnMinimums: ColumnWidths = {
  id: 52,
  time: 72,
  level: 72,
  origin: 130,
  message: 240
};
const records = ref<LogRecord[]>([]);
const status = ref<Status | null>(null);
const connection = ref<ConnectionState>("connecting");
const paused = ref(false);
const follow = ref(true);
const keyword = ref("");
const regexSearch = ref(false);
const customNoiseText = ref("");
const selectedLevels = ref(new Set(levels));
const selectedSources = ref(new Set<string>());
const selectedRecord = ref<LogRecord | null>(null);
const selectedIds = ref(new Set<number>());
const lastSelectedId = ref<number | null>(null);
const logList = ref<HTMLElement | null>(null);
const logScrollLeft = ref(0);
const payloadOpen = ref(false);
const themeMode = ref<ThemeMode>(
    readEnum("theme", ["dark", "light"], window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark")
);
const locale = ref<Locale>(readEnum("locale", ["zh", "en"], navigator.language.toLowerCase().startsWith("zh") ? "zh" : "en"));
const visibleColumns = ref(new Set(["time", "level", "origin", "message"]));
const columnWidths = ref<ColumnWidths>(readColumnWidths());
let programmaticScroll = false;

const defaultNoiseRules: NoiseRule[] = [
  {pattern: "AtlasResourceLoader: Missing sprite", enabled: true, tipKey: "noiseAtlas"},
  {pattern: "Asset not cached:", enabled: true, tipKey: "noiseAssetCache"},
  {pattern: "[Assets] Missing resource path", enabled: true, tipKey: "noiseMissingResource"},
  {pattern: "Found mod manifest file", enabled: true, tipKey: "noiseManifestScan"},
  {pattern: "missing the 'id' field", enabled: true, tipKey: "noiseMissingId"},
  {pattern: "warmup job failed", enabled: true, tipKey: "noiseWarmup"},
  {pattern: "Limiting background FPS", enabled: true, tipKey: "noiseBackgroundFps"},
  {pattern: "Restored foreground FPS", enabled: true, tipKey: "noiseForegroundFps"}
];
const noiseRules = ref<NoiseRule[]>(readNoiseRules());

const messages = {
  zh: {
    appTitle: "RitsuLib 日志",
    search: "搜索",
    searchAndFilters: "搜索和筛选",
    searchPlaceholder: "消息、来源或分类",
    connecting: "连接中",
    connected: "已连接",
    reconnecting: "重连中",
    visible: "可见",
    total: "总计",
    suppressed: "已降噪",
    pause: "暂停",
    resume: "继续",
    following: "跟随中",
    followOff: "已停止跟随",
    dark: "深色",
    light: "浅色",
    language: "English",
    filters: "筛选",
    reset: "重置",
    regexMode: "正则",
    addNoiseRule: "添加降噪规则",
    deleteNoiseRule: "删除",
    noisePattern: "要隐藏的文本片段",
    levels: "等级",
    sources: "来源",
    columns: "列",
    noise: "降噪",
    enabled: "启用",
    liveStream: "实时日志",
    selected: "已选择",
    copyLines: "复制行",
    copyJsonl: "复制 JSONL",
    copyJson: "复制 JSON",
    payload: "Payload",
    clearSelection: "清除选择",
    export: "导出",
    clearView: "清空视图",
    close: "关闭",
    colId: "ID",
    colTime: "时间",
    colLevel: "等级",
    colOrigin: "来源",
    colMessage: "消息",
    game: "游戏",
    sourceId: "来源 ID",
    tipPause: "暂停或继续接收新日志。暂停不会清除已有日志。",
    tipFollow: "保持在底部时自动滚动；手动上翻会自动关闭，回到底部后恢复。",
    tipTheme: "切换浅色/深色主题。",
    tipLanguage: "切换中文/英文界面。",
    tipSearch: "搜索会同时匹配日志正文、来源和分类。",
    tipRegex: "切换为正则搜索。关闭时使用普通关键词包含匹配。",
    tipAddNoiseRule: "把这段文本加入降噪规则，之后匹配的日志会被隐藏。",
    tipDeleteNoiseRule: "删除这条降噪规则。",
    tipCopyLines: "复制选中日志的可读文本行。",
    tipCopyJsonl: "复制选中日志的 JSONL，每行一条记录。",
    tipCopyJson: "复制当前焦点日志的完整 JSON。",
    tipPayload: "按需查看当前焦点日志的完整 Payload。",
    tipClearSelection: "取消所有选中行。",
    tipExport: "导出当前筛选结果为 JSONL。",
    tipClearView: "只清空当前浏览器视图，不影响游戏日志源。",
    tipColId: "日志管道分配的递增 ID。",
    tipColTime: "日志进入管道的本地时间。",
    tipColLevel: "OpenTelemetry severity text / 游戏日志等级。",
    tipColOrigin: "优先显示 Mod 名称，下面保留分类或来源 id。",
    tipColMessage: "日志正文。长日志会换行展开。",
    tipSelect: "选择此行。Shift 单击范围选择，Ctrl/Cmd 单击追加选择。",
    tipSource: "按此来源筛选。显示名称来自 mod manifest，括号内为原始 id。",
    tipColumnResize: "拖动调整列宽。",
    noiseAtlas: "隐藏图集缺失 sprite 的常见开发噪音。",
    noiseAssetCache: "隐藏资源未缓存/预加载缺失的重复提示。",
    noiseMissingResource: "隐藏缺失资源路径提示。",
    noiseManifestScan: "隐藏扫描 mod manifest 文件的普通日志。",
    noiseMissingId: "隐藏非 manifest JSON 被误扫时的缺少 id 提示。",
    noiseWarmup: "隐藏资源 warmup 失败的重复提示。",
    noiseBackgroundFps: "隐藏窗口失焦时限制后台 FPS 的提示。",
    noiseForegroundFps: "隐藏窗口回到前台恢复 FPS 的提示。"
  },
  en: {
    appTitle: "RitsuLib Logs",
    search: "Search",
    searchAndFilters: "Search & Filters",
    searchPlaceholder: "Message, source, or category",
    connecting: "Connecting",
    connected: "Connected",
    reconnecting: "Reconnecting",
    visible: "visible",
    total: "total",
    suppressed: "suppressed",
    pause: "Pause",
    resume: "Resume",
    following: "Following",
    followOff: "Follow off",
    dark: "Dark",
    light: "Light",
    language: "中文",
    filters: "Filters",
    reset: "Reset",
    regexMode: "Regex",
    addNoiseRule: "Add noise rule",
    deleteNoiseRule: "Delete",
    noisePattern: "Text fragment to hide",
    levels: "Levels",
    sources: "Sources",
    columns: "Columns",
    noise: "Noise",
    enabled: "enabled",
    liveStream: "Live stream",
    selected: "selected",
    copyLines: "Copy lines",
    copyJsonl: "Copy JSONL",
    copyJson: "Copy JSON",
    payload: "Payload",
    clearSelection: "Clear selection",
    export: "Export",
    clearView: "Clear view",
    close: "Close",
    colId: "Id",
    colTime: "Time",
    colLevel: "Level",
    colOrigin: "Origin",
    colMessage: "Message",
    game: "game",
    sourceId: "Source id",
    tipPause: "Pause or resume incoming log events. Existing rows stay visible.",
    tipFollow: "Auto-scroll while at bottom. Manual scrolling disables it until you return to the bottom.",
    tipTheme: "Switch light/dark theme.",
    tipLanguage: "Switch Chinese/English UI.",
    tipSearch: "Search matches message, source, and category.",
    tipRegex: "Toggle regex search. When off, search uses plain contains matching.",
    tipAddNoiseRule: "Add this text as a noise rule; matching rows will be hidden.",
    tipDeleteNoiseRule: "Delete this noise rule.",
    tipCopyLines: "Copy selected rows as readable text.",
    tipCopyJsonl: "Copy selected records as JSONL.",
    tipCopyJson: "Copy the focused row as formatted JSON.",
    tipPayload: "Open the full payload for the focused row only when needed.",
    tipClearSelection: "Clear all selected rows.",
    tipExport: "Export current filtered rows as JSONL.",
    tipClearView: "Clear only this browser view, not the game log source.",
    tipColId: "Incremental id assigned by the log pipeline.",
    tipColTime: "Local time when the record entered the pipeline.",
    tipColLevel: "OpenTelemetry severity text / game log level.",
    tipColOrigin: "Prefers mod display name; keeps category or source id as secondary text.",
    tipColMessage: "Expanded log body. Long logs wrap.",
    tipSelect: "Select this row. Shift-click selects a range, Ctrl/Cmd-click toggles.",
    tipSource: "Filter by this source. Display names come from mod manifests; raw ids stay available.",
    tipColumnResize: "Drag to resize this column.",
    noiseAtlas: "Hide common missing atlas sprite noise.",
    noiseAssetCache: "Hide repeated asset cache/preload miss messages.",
    noiseMissingResource: "Hide missing resource path messages.",
    noiseManifestScan: "Hide normal mod manifest scanning logs.",
    noiseMissingId: "Hide missing id messages from non-manifest JSON scans.",
    noiseWarmup: "Hide repeated resource warmup failures.",
    noiseBackgroundFps: "Hide background FPS limit messages.",
    noiseForegroundFps: "Hide foreground FPS restore messages."
  }
} as const;

const api = (path: string) => {
  const join = path.includes("?") ? "&" : "?";
  return `${path}${join}token=${encodeURIComponent(token)}`;
};

const sources = computed(() => summarize("source"));
const t = computed(() => messages[locale.value]);
const filteredRecords = computed(() => {
  const searchText = keyword.value.trim();
  const needle = searchText.toLowerCase();
  const regex = regexSearch.value ? compileRegex(searchText) : null;

  return records.value.filter((record) => {
    const source = record.source ?? "";
    const category = record.category ?? "";
    const haystack = `${record.body ?? ""} ${source} ${category}`;
    const lower = haystack.toLowerCase();

    if (!selectedLevels.value.has(record.severityText)) return false;
    if (selectedSources.value.size > 0 && !selectedSources.value.has(source)) return false;
    if (searchText && regexSearch.value && !regex) return false;
    if (needle && !regexSearch.value && !lower.includes(needle)) return false;
    if (regex && !regex.test(haystack)) return false;
    if (noiseRules.value.some((rule) => rule.enabled && lower.includes(rule.pattern.toLowerCase()))) return false;
    return true;
  });
});
const visibleRecords = computed(() => filteredRecords.value);
const rowVirtualizer = useVirtualizer(computed(() => ({
  count: visibleRecords.value.length,
  getScrollElement: () => logList.value,
  estimateSize: (index) => estimateRecordHeight(visibleRecords.value[index]),
  getItemKey: (index) => visibleRecords.value[index]?.id ?? index,
  overscan: 14,
  useAnimationFrameWithResizeObserver: true
})));
const virtualRows = computed(() => {
  return rowVirtualizer.value.getVirtualItems()
      .map((item) => ({
        item,
        record: visibleRecords.value[item.index]
      }))
      .filter((row): row is { item: typeof row.item; record: LogRecord } => Boolean(row.record));
});
const selectedRecords = computed(() => visibleRecords.value.filter((record) => selectedIds.value.has(record.id)));
const selectedRecordJson = computed(() => selectedRecord.value ? JSON.stringify(selectedRecord.value, null, 2) : "");

const levelCounts = computed(() => {
  const counts = new Map<string, number>();
  for (const record of records.value)
    counts.set(record.severityText, (counts.get(record.severityText) ?? 0) + 1);
  return counts;
});

const suppressedCount = computed(() => {
  let count = 0;
  for (const record of records.value) {
    const lower = `${record.body ?? ""} ${record.source ?? ""} ${record.category ?? ""}`.toLowerCase();
    if (noiseRules.value.some((rule) => rule.enabled && lower.includes(rule.pattern.toLowerCase())))
      count++;
  }
  return count;
});

const enabledNoiseRules = computed(() => noiseRules.value.filter((rule) => rule.enabled).length);

const visibleDataColumns = computed(() => {
  return ["id", "time", "level", "origin", "message"]
      .filter((column) => visibleColumns.value.has(column));
});

const gridTemplate = computed(() => {
  const tracks = ["32px"];
  for (const [index, column] of visibleDataColumns.value.entries()) {
    const width = columnWidths.value[column] ?? columnDefaults[column] ?? 120;
    tracks.push(index === visibleDataColumns.value.length - 1 ? `minmax(${width}px, 1fr)` : `${width}px`);
  }
  return tracks.join(" ");
});

const tableWidth = computed(() => {
  const selectColumnWidth = 32;
  const horizontalPadding = 25;
  const gapWidth = 9 * visibleDataColumns.value.length;
  const dataWidth = visibleDataColumns.value
      .reduce((total, column) => total + (columnWidths.value[column] ?? columnDefaults[column] ?? 120), 0);
  return `max(100%, ${selectColumnWidth + horizontalPadding + gapWidth + dataWidth}px)`;
});

onMounted(async () => {
  await loadHistory();
  connectEvents();
  await loadStatus();
  setInterval(loadStatus, 2000);
});

watch(themeMode, (value) => saveValue("theme", value));
watch(locale, (value) => saveValue("locale", value));
watch(columnWidths, (value) => saveValue("columnWidths", JSON.stringify(value)), {deep: true});
watch(noiseRules, (value) => saveValue("noiseRules", JSON.stringify(value)), {deep: true});
watch(() => visibleRecords.value.length, async () => {
  await nextTick();
  if (follow.value)
    scrollToBottom();
});

async function loadHistory() {
  const response = await fetch(api("/api/history?limit=10000"));
  records.value = await response.json();
  await nextTick();
  scrollToBottom();
}

async function loadStatus() {
  try {
    const response = await fetch(api("/api/status"));
    status.value = await response.json();
  } catch {
    status.value = null;
  }
}

function connectEvents() {
  const events = new EventSource(api("/api/events"));
  events.onopen = () => {
    connection.value = "connected";
  };
  events.onerror = () => {
    connection.value = "reconnecting";
  };
  events.addEventListener("log", (event) => {
    const record = JSON.parse((event as MessageEvent).data) as LogRecord;
    records.value.push(record);
    if (records.value.length > 20000)
      records.value.splice(0, records.value.length - 20000);
    if (!paused.value && follow.value)
      nextTick().then(scrollToBottom);
  });
}

function handleLogScroll() {
  if (logList.value)
    logScrollLeft.value = logList.value.scrollLeft;

  if (programmaticScroll)
    return;

  follow.value = isAtBottom();
}

function summarize(field: "source") {
  const counts = new Map<string, number>();
  const displayNames = new Map<string, string>();
  for (const record of records.value) {
    const value = record[field];
    if (!value) continue;
    counts.set(value, (counts.get(value) ?? 0) + 1);
    displayNames.set(value, originName(record));
  }
  return [...counts.entries()]
      .map(([name, count]) => ({name, displayName: displayNames.get(name) ?? name, count}))
      .sort((a, b) => b.count - a.count || a.displayName.localeCompare(b.displayName));
}

function compileRegex(text: string) {
  const trimmed = text.trim();
  if (!trimmed) return null;
  try {
    return new RegExp(trimmed, "i");
  } catch {
    return null;
  }
}

function toggleLevel(level: string) {
  const next = new Set(selectedLevels.value);
  next.has(level) ? next.delete(level) : next.add(level);
  selectedLevels.value = next;
}

function toggleColumn(column: string) {
  const next = new Set(visibleColumns.value);
  next.has(column) ? next.delete(column) : next.add(column);
  if (next.size === 0) next.add("message");
  visibleColumns.value = next;
}

function resizeColumn(column: string, event: PointerEvent) {
  event.preventDefault();
  const startX = event.clientX;
  const startWidth = columnWidths.value[column] ?? columnDefaults[column] ?? 120;
  const minWidth = columnMinimums[column] ?? 80;

  const move = (moveEvent: PointerEvent) => {
    columnWidths.value = {
      ...columnWidths.value,
      [column]: Math.max(minWidth, startWidth + moveEvent.clientX - startX)
    };
  };
  const up = () => {
    document.removeEventListener("pointermove", move);
    document.removeEventListener("pointerup", up);
  };

  document.addEventListener("pointermove", move);
  document.addEventListener("pointerup", up);
}

function toggleSource(value: string) {
  selectedSources.value = toggleSetItem(selectedSources.value, value);
}

function toggleSetItem(set: Set<string>, value: string) {
  const next = new Set(set);
  next.has(value) ? next.delete(value) : next.add(value);
  return next;
}

function clearFilters() {
  keyword.value = "";
  regexSearch.value = false;
  selectedSources.value = new Set();
  selectedLevels.value = new Set(levels);
}

function addNoiseRule() {
  const pattern = customNoiseText.value.trim();
  if (!pattern)
    return;

  if (!noiseRules.value.some((rule) => rule.pattern.toLowerCase() === pattern.toLowerCase()))
    noiseRules.value.push({pattern, enabled: true, tipKey: "tipAddNoiseRule"});

  customNoiseText.value = "";
}

function removeNoiseRule(pattern: string) {
  noiseRules.value = noiseRules.value.filter((rule) => rule.pattern !== pattern);
}

function clearView() {
  records.value = [];
  selectedRecord.value = null;
  clearSelection();
}

function clearSelection() {
  selectedIds.value = new Set();
  lastSelectedId.value = null;
}

function handleRowClick(record: LogRecord, event: MouseEvent) {
  selectedRecord.value = record;
  const target = event.target as HTMLElement | null;
  if (target?.closest("button,input")) return;
  if (window.getSelection()?.toString()) return;

  if (event.shiftKey) {
    selectRange(record);
    return;
  }

  if (event.ctrlKey || event.metaKey) {
    toggleRecordSelection(record);
    return;
  }

  selectedIds.value = new Set([record.id]);
  lastSelectedId.value = record.id;
}

function toggleRecordSelection(record: LogRecord) {
  const next = new Set(selectedIds.value);
  next.has(record.id) ? next.delete(record.id) : next.add(record.id);
  selectedIds.value = next;
  selectedRecord.value = record;
  lastSelectedId.value = record.id;
}

function selectRange(record: LogRecord) {
  const visible = visibleRecords.value;
  const lastId = lastSelectedId.value;
  const lastIndex = lastId == null ? -1 : visible.findIndex((item) => item.id === lastId);
  const currentIndex = visible.findIndex((item) => item.id === record.id);
  if (lastIndex < 0 || currentIndex < 0) {
    toggleRecordSelection(record);
    return;
  }

  const [start, end] = lastIndex < currentIndex ? [lastIndex, currentIndex] : [currentIndex, lastIndex];
  const next = new Set(selectedIds.value);
  for (const item of visible.slice(start, end + 1))
    next.add(item.id);
  selectedIds.value = next;
  selectedRecord.value = record;
}

async function copySelectedMessages() {
  await navigator.clipboard.writeText(selectedRecords.value.map(formatRecordLine).join("\n"));
}

async function copySelectedJsonl() {
  await navigator.clipboard.writeText(selectedRecords.value.map((record) => JSON.stringify(record)).join("\n"));
}

async function copyRecordJson() {
  if (selectedRecord.value)
    await navigator.clipboard.writeText(JSON.stringify(selectedRecord.value, null, 2));
}

function openPayload() {
  if (selectedRecord.value)
    payloadOpen.value = true;
}

function exportJsonl() {
  const data = filteredRecords.value.map((record) => JSON.stringify(record)).join("\n");
  const blob = new Blob([data], {type: "application/x-ndjson"});
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = "ritsulib-debug-logs.jsonl";
  link.click();
  URL.revokeObjectURL(link.href);
}

function formatRecordLine(record: LogRecord) {
  const origin = record.source ? ` [${originName(record)}${record.category ? `/${record.category}` : ""}]` : "";
  return `${formatTime(record.timestamp)} ${record.severityText}${origin} ${record.body}`;
}

function isMultilineRecord(record: LogRecord) {
  return record.body.includes("\n") || record.body.length > 140;
}

function estimateRecordHeight(record: LogRecord) {
  if (!record)
    return 42;

  if (!isMultilineRecord(record))
    return 42;

  const lineCount = record.body.split("\n").length;
  const wrappedLines = Math.floor(record.body.length / 160);
  return Math.min(260, 46 + Math.max(lineCount - 1, wrappedLines) * 18);
}

function measureVirtualElement(element: Element | ComponentPublicInstance | null) {
  if (element instanceof Element)
    rowVirtualizer.value.measureElement(element);
}

function scrollToBottom() {
  if (visibleRecords.value.length === 0)
    return;

  programmaticScroll = true;
  scrollToBottomNow();
  requestAnimationFrame(() => {
    scrollToBottomNow();
    requestAnimationFrame(() => {
      scrollToBottomNow();
      programmaticScroll = false;
      follow.value = true;
    });
  });
}

function scrollToBottomNow() {
  rowVirtualizer.value.scrollToIndex(visibleRecords.value.length - 1, {align: "end"});
  const el = logList.value;
  if (el)
    el.scrollTop = el.scrollHeight;
}

function isAtBottom() {
  const el = logList.value;
  if (!el) return true;
  return el.scrollHeight - el.scrollTop - el.clientHeight <= 8;
}

function formatTime(timestamp: string) {
  return new Date(timestamp).toLocaleTimeString();
}

function toggleTheme() {
  themeMode.value = themeMode.value === "dark" ? "light" : "dark";
}

function toggleFollow() {
  if (follow.value) {
    follow.value = false;
    return;
  }

  follow.value = true;
  requestAnimationFrame(scrollToBottom);
}

function toggleLocale() {
  locale.value = locale.value === "zh" ? "en" : "zh";
}

function tt(key: string) {
  return (t.value as Record<string, string>)[key] ?? key;
}

function originName(record: LogRecord) {
  return attributeString(record, "ritsulib.log.source_display_name") ??
      attributeString(record, "mod.display_name") ??
      attributeString(record, "mod.name") ??
      record.source ??
      t.value.game;
}

function originSubtitle(record: LogRecord) {
  const source = record.source ?? "";
  if (record.category && source)
    return `${record.category} · ${source}`;

  return record.category || source;
}

function originTooltip(record: LogRecord) {
  const parts = [`${t.value.colOrigin}: ${originName(record)}`];
  if (record.source)
    parts.push(`${t.value.sourceId}: ${record.source}`);
  if (record.category)
    parts.push(`${t.value.colMessage}: ${record.category}`);
  return parts.join("\n");
}

function attributeString(record: LogRecord, key: string) {
  const value = record.attributes?.[key];
  return typeof value === "string" && value.trim() ? value : null;
}

function readEnum<T extends string>(key: string, values: readonly T[], fallback: T) {
  const value = localStorage.getItem(storagePrefix + key);
  return values.includes(value as T) ? (value as T) : fallback;
}

function saveValue(key: string, value: string) {
  localStorage.setItem(storagePrefix + key, value);
}

function readColumnWidths() {
  try {
    const raw = localStorage.getItem(storagePrefix + "columnWidths");
    if (!raw) return {...columnDefaults};
    const parsed = JSON.parse(raw) as ColumnWidths;
    return Object.fromEntries(
        Object.entries(columnDefaults).map(([key, fallback]) => [
          key,
          Math.max(columnMinimums[key], Number(parsed[key]) || fallback)
        ])
    ) as ColumnWidths;
  } catch {
    return {...columnDefaults};
  }
}

function readNoiseRules() {
  try {
    const raw = localStorage.getItem(storagePrefix + "noiseRules");
    if (!raw) return [...defaultNoiseRules];
    const parsed = JSON.parse(raw) as NoiseRule[];
    const valid = parsed.filter((rule) => typeof rule.pattern === "string" && rule.pattern.trim());
    return valid.length > 0 ? valid : [...defaultNoiseRules];
  } catch {
    return [...defaultNoiseRules];
  }
}
</script>

<template>
  <div :data-theme="themeMode" class="shell">
    <header class="appbar">
      <div class="brand">
        <h1>{{ t.appTitle }}</h1>
        <span :class="['connection', connection]">{{ t[connection] }}</span>
      </div>
      <div class="runtime">
        <span>{{ filteredRecords.length }} {{ t.visible }}</span>
        <span>{{ records.length }} {{ t.total }}</span>
        <span>{{ suppressedCount }} {{ t.suppressed }}</span>
      </div>
      <div class="primary-actions">
        <button v-tooltip="t.tipPause" @click="paused = !paused">{{ paused ? t.resume : t.pause }}</button>
        <button v-tooltip="t.tipFollow" @click="toggleFollow">{{ follow ? t.following : t.followOff }}</button>
        <button v-tooltip="t.tipTheme" @click="toggleTheme">{{ themeMode === "dark" ? t.light : t.dark }}</button>
        <button v-tooltip="t.tipLanguage" @click="toggleLocale">{{ t.language }}</button>
      </div>
    </header>

    <main>
      <aside class="filters">
        <section class="filter-block">
          <div class="section-title">{{ t.levels }}</div>
          <div class="level-grid">
            <button
                v-for="level in levels"
                :key="level"
                :class="['level-chip', level, { active: selectedLevels.has(level) }]"
                @click="toggleLevel(level)"
            >
              <span>{{ level }}</span>
              <strong>{{ levelCounts.get(level) ?? 0 }}</strong>
            </button>
          </div>
        </section>

        <section v-if="sources.length > 0" class="filter-block">
          <div class="section-title">{{ t.sources }}</div>
          <button
              v-for="source in sources"
              :key="source.name"
              v-tooltip="`${t.tipSource}\n${source.name}`"
              :class="['facet', { active: selectedSources.has(source.name) }]"
              @click="toggleSource(source.name)"
          >
            <span>{{ source.displayName }}</span>
            <strong>{{ source.count }}</strong>
          </button>
        </section>

        <section class="filter-block">
          <div class="section-title">{{ t.columns }}</div>
          <div class="column-grid">
            <button
                v-for="column in columnOptions"
                :key="column.id"
                v-tooltip="tt(column.tipKey)"
                :class="['column-chip', { active: visibleColumns.has(column.id) }]"
                @click="toggleColumn(column.id)"
            >
              {{ tt(column.labelKey) }}
            </button>
          </div>
        </section>

        <section class="filter-block">
          <div class="section-title">
            <span>{{ t.noise }}</span>
            <span>{{ enabledNoiseRules }}/{{ noiseRules.length }} {{ t.enabled }}</span>
          </div>
          <div class="noise-add">
            <input
                v-model="customNoiseText"
                v-tooltip="t.tipAddNoiseRule"
                :placeholder="t.noisePattern"
                type="text"
                @keydown.enter="addNoiseRule"
            />
            <button v-tooltip="t.tipAddNoiseRule" @click="addNoiseRule">{{ t.addNoiseRule }}</button>
          </div>
          <div
              v-for="rule in noiseRules"
              :key="rule.pattern"
              class="rule-row"
          >
            <button
                v-tooltip="tt(rule.tipKey ?? 'tipAddNoiseRule')"
                :class="['rule', { active: rule.enabled }]"
                @click="rule.enabled = !rule.enabled"
            >
              <span>{{ rule.pattern }}</span>
            </button>
            <button
                v-tooltip="t.tipDeleteNoiseRule"
                class="rule-delete"
                @click.stop="removeNoiseRule(rule.pattern)"
            >
              {{ t.deleteNoiseRule }}
            </button>
          </div>
        </section>
      </aside>

      <section class="log-stage">
        <div class="stage-toolbar">
          <div>
            <strong>{{ t.liveStream }}</strong>
            <span>{{ visibleRecords.length }} {{ t.visible }} · {{ selectedRecords.length }} {{ t.selected }}</span>
          </div>
          <div class="stage-actions">
            <button v-tooltip="t.tipCopyLines" :disabled="selectedRecords.length === 0" @click="copySelectedMessages">
              {{ t.copyLines }}
            </button>
            <button v-tooltip="t.tipCopyJsonl" :disabled="selectedRecords.length === 0" @click="copySelectedJsonl">
              {{ t.copyJsonl }}
            </button>
            <button v-tooltip="t.tipCopyJson" :disabled="!selectedRecord" @click="copyRecordJson">{{
                t.copyJson
              }}
            </button>
            <button v-tooltip="t.tipPayload" :disabled="!selectedRecord" @click="openPayload">{{ t.payload }}</button>
            <button v-tooltip="t.tipClearSelection" :disabled="selectedRecords.length === 0" @click="clearSelection">
              {{ t.clearSelection }}
            </button>
            <button v-tooltip="t.tipExport" @click="exportJsonl">{{ t.export }}</button>
            <button v-tooltip="t.tipClearView" @click="clearView">{{ t.clearView }}</button>
          </div>
        </div>

        <div class="search-toolbar">
          <input v-model="keyword" v-tooltip="t.tipSearch" :placeholder="t.searchPlaceholder" type="search"/>
          <label v-tooltip="t.tipRegex" class="check-option">
            <input v-model="regexSearch" type="checkbox"/>
            <span>{{ t.regexMode }}</span>
          </label>
          <button class="ghost" @click="clearFilters">{{ t.reset }}</button>
        </div>

        <div class="log-table">
          <div class="log-header-viewport">
            <div
                :style="{ gridTemplateColumns: gridTemplate, transform: `translateX(${-logScrollLeft}px)`, width: tableWidth }"
                class="log-header-row"
            >
              <span class="select-heading"></span>
              <span v-if="visibleColumns.has('id')" class="column-heading">
                {{ t.colId }}
                <span v-tooltip="t.tipColumnResize" class="resize-handle"
                      @pointerdown="resizeColumn('id', $event)"></span>
              </span>
              <span v-if="visibleColumns.has('time')" class="column-heading">
                {{ t.colTime }}
                <span v-tooltip="t.tipColumnResize" class="resize-handle"
                      @pointerdown="resizeColumn('time', $event)"></span>
              </span>
              <span v-if="visibleColumns.has('level')" class="column-heading">
                {{ t.colLevel }}
                <span v-tooltip="t.tipColumnResize" class="resize-handle"
                      @pointerdown="resizeColumn('level', $event)"></span>
              </span>
              <span v-if="visibleColumns.has('origin')" class="column-heading">
                {{ t.colOrigin }}
                <span v-tooltip="t.tipColumnResize" class="resize-handle"
                      @pointerdown="resizeColumn('origin', $event)"></span>
              </span>
              <span v-if="visibleColumns.has('message')" class="column-heading">
                {{ t.colMessage }}
                <span v-tooltip="t.tipColumnResize" class="resize-handle"
                      @pointerdown="resizeColumn('message', $event)"></span>
              </span>
            </div>
          </div>
          <div ref="logList" class="log-list" @scroll="handleLogScroll">
            <div :style="{ height: `${rowVirtualizer.getTotalSize()}px`, width: tableWidth }" class="virtual-pad">
              <div class="virtual-window">
                <div
                    v-for="{ item, record } in virtualRows"
                    :key="String(item.key)"
                    :ref="measureVirtualElement"
                    :class="[
                  'log-row',
                  record.severityText,
                  {
                    focused: selectedRecord?.id === record.id,
                selected: selectedIds.has(record.id),
                multiline: isMultilineRecord(record)
              }
            ]"
                    :data-index="item.index"
                    :style="{ gridTemplateColumns: gridTemplate, transform: `translateY(${item.start}px)` }"
                    @click="handleRowClick(record, $event)"
                >
                  <label v-tooltip="t.tipSelect" class="select-cell">
                    <input
                        :checked="selectedIds.has(record.id)"
                        type="checkbox"
                        @change="toggleRecordSelection(record)"
                        @click.stop
                    />
                  </label>
                  <span v-if="visibleColumns.has('id')" class="id">#{{ record.id }}</span>
                  <span v-if="visibleColumns.has('time')" class="time">{{ formatTime(record.timestamp) }}</span>
                  <span v-if="visibleColumns.has('level')" class="severity">{{ record.severityText }}</span>
                  <span v-if="visibleColumns.has('origin')" v-tooltip="originTooltip(record)" class="origin">
                  <span class="source">{{ originName(record) }}</span>
                  <span v-if="originSubtitle(record)" class="category">{{ originSubtitle(record) }}</span>
                </span>
                  <span v-if="visibleColumns.has('message')" class="message">{{ record.body }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </main>

    <div v-if="payloadOpen && selectedRecord" class="payload-backdrop" @click.self="payloadOpen = false">
      <section class="payload-panel">
        <div class="payload-head">
          <div>
            <strong>{{ selectedRecord.severityText }}</strong>
            <span>{{ originName(selectedRecord) }}</span>
          </div>
          <div class="payload-actions">
            <button @click="copyRecordJson">{{ t.copyJson }}</button>
            <button @click="payloadOpen = false">{{ t.close }}</button>
          </div>
        </div>
        <pre class="payload-message">{{ selectedRecord.body }}</pre>
        <pre>{{ selectedRecordJson }}</pre>
      </section>
    </div>
  </div>
</template>
