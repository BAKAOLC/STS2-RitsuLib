import {computed, nextTick, onMounted, onUnmounted, ref, watch} from "vue";
import type {ConnectionState, LogRecord, NoiseRule, Status, ThemeMode} from "../logTypes";
import {type ColumnId, defaultNoiseRules, logLevels, storagePrefix} from "../viewerConfig";

export function useLogViewer() {
    const params = new URLSearchParams(location.search);
    const token = params.get("token") ?? "";

    const records = ref<LogRecord[]>([]);
    const status = ref<Status | null>(null);
    const connection = ref<ConnectionState>("connecting");
    const paused = ref(false);
    const follow = ref(true);
    const keyword = ref(readString("keyword", ""));
    const regexSearch = ref(readBoolean("regexSearch", false));
    const customNoiseText = ref("");
    const selectedLevels = ref(readStringSet("selectedLevels", new Set<string>(logLevels), new Set(logLevels)));
    const selectedSources = ref(readStringSet("selectedSources", new Set<string>()));
    const selectedRecord = ref<LogRecord | null>(null);
    const selectedIds = ref(new Set<string>());
    const lastSelectedKey = ref<string | null>(null);
    const payloadOpen = ref(false);
    const themeMode = ref<ThemeMode>(
        readEnum("theme", ["dark", "light"], window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark")
    );
    const visibleColumns = ref(readStringSet<ColumnId>(
        "visibleColumns",
        new Set(["time", "level", "origin", "message"]),
        new Set(["time", "level", "origin", "message", "id"])
    ));
    const clearOnNewSession = ref(readBoolean("clearOnNewSession", true));
    const noiseRules = ref<NoiseRule[]>(readNoiseRules());
    let activeSessionId: string | null = null;
    let events: EventSource | null = null;
    let statusTimer: number | null = null;
    let recordKeys = new Set<string>();

    const api = (path: string) => {
        const join = path.includes("?") ? "&" : "?";
        return `${path}${join}token=${encodeURIComponent(token)}`;
    };

    const sources = computed(() => summarize("source"));
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
    const selectedRecords = computed(() => filteredRecords.value.filter((record) => selectedIds.value.has(recordKey(record))));
    const areAllFilteredSelected = computed(() => {
        return filteredRecords.value.length > 0 && filteredRecords.value.every((record) => selectedIds.value.has(recordKey(record)));
    });
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
    const activeFilterCount = computed(() => {
        let count = 0;
        if (selectedSources.value.size > 0) count++;
        if (selectedLevels.value.size !== logLevels.length) count++;
        count += noiseRules.value.filter((rule) => rule.enabled).length;
        return count;
    });

    onMounted(async () => {
        await loadStatus();
        await loadHistory();
        connectEvents();
        statusTimer = window.setInterval(loadStatus, 2000);
    });

    onUnmounted(() => {
        events?.close();
        events = null;
        if (statusTimer != null) {
            window.clearInterval(statusTimer);
            statusTimer = null;
        }
    });

    watch(themeMode, (value) => saveValue("theme", value));
    watch(keyword, (value) => saveValue("keyword", value));
    watch(regexSearch, (value) => saveValue("regexSearch", value ? "1" : "0"));
    watch(selectedLevels, (value) => saveStringSet("selectedLevels", value));
    watch(selectedSources, (value) => saveStringSet("selectedSources", value));
    watch(visibleColumns, (value) => saveStringSet("visibleColumns", value));
    watch(noiseRules, (value) => saveValue("noiseRules", JSON.stringify(value)), {deep: true});
    watch(clearOnNewSession, (value) => saveValue("clearOnNewSession", value ? "1" : "0"));

    async function loadHistory() {
        replaceRecords(await fetchHistory(activeSessionId));
    }

    async function loadStatus() {
        try {
            const response = await fetch(api("/api/status"));
            applySessionStatus(await response.json() as Status);
        } catch {
            status.value = null;
        }
    }

    function connectEvents() {
        events?.close();
        events = new EventSource(api("/api/events"));
        events.onopen = () => {
            connection.value = "connected";
        };
        events.onerror = () => {
            connection.value = "reconnecting";
        };
        events.addEventListener("session", (event) => {
            applySessionStatus(JSON.parse((event as MessageEvent).data) as Status);
        });
        events.addEventListener("log", (event) => {
            if (paused.value)
                return;

            const record = JSON.parse((event as MessageEvent).data) as LogRecord;
            appendRecord(record, activeSessionId);
        });
    }

    function applySessionStatus(nextStatus: Status) {
        status.value = nextStatus;
        const nextSessionId = nextStatus.sessionId ?? null;
        if (!nextSessionId)
            return;

        if (!activeSessionId) {
            activeSessionId = nextSessionId;
            return;
        }

        if (activeSessionId === nextSessionId)
            return;

        activeSessionId = nextSessionId;
        selectedRecord.value = null;
        payloadOpen.value = false;
        clearSelection();
        if (clearOnNewSession.value) {
            records.value = [];
            recordKeys.clear();
        }

        void replaceCurrentSessionHistory(nextSessionId);
    }

    async function fetchHistory(sessionId: string | null) {
        const response = await fetch(api("/api/history?limit=10000"));
        const history = await response.json() as LogRecord[];
        return history.map((record) => ({
            ...record,
            sessionId: record.sessionId ?? sessionId ?? undefined
        }));
    }

    async function replaceCurrentSessionHistory(sessionId: string) {
        try {
            const history = await fetchHistory(sessionId);
            const currentSessionEvents = records.value.filter((record) => record.sessionId === sessionId);
            const otherSessionRecords = clearOnNewSession.value
                ? []
                : records.value.filter((record) => record.sessionId !== sessionId);
            replaceRecords(mergeRecords([...otherSessionRecords, ...history], currentSessionEvents));
            await nextTick();
        } catch {
            status.value = null;
        }
    }

    function appendRecord(record: LogRecord, sessionId: string | null) {
        const next = {
            ...record,
            sessionId: record.sessionId ?? sessionId ?? undefined
        };
        const key = recordKey(next);
        if (recordKeys.has(key))
            return;

        recordKeys.add(key);
        records.value.push(next);
        if (records.value.length > 20000)
            replaceRecords(trimRecordList(records.value));
    }

    function mergeRecords(base: LogRecord[], incoming: LogRecord[]) {
        const seen = new Set(base.map(recordKey));
        const result = [...base];
        for (const record of incoming) {
            const key = recordKey(record);
            if (seen.has(key))
                continue;

            seen.add(key);
            result.push(record);
        }
        return result;
    }

    function trimRecordList(items: LogRecord[]) {
        return items.length > 20000 ? items.slice(items.length - 20000) : items;
    }

    function replaceRecords(items: LogRecord[]) {
        records.value = trimRecordList(items);
        recordKeys = new Set(records.value.map(recordKey));
    }

    function summarize(field: "source") {
        const counts = new Map<string, number>();
        for (const record of records.value) {
            const value = record[field];
            if (!value) continue;
            counts.set(value, (counts.get(value) ?? 0) + 1);
        }
        return [...counts.entries()]
            .map(([name, count]) => ({name, count}))
            .sort((a, b) => b.count - a.count || a.name.localeCompare(b.name));
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
        selectedLevels.value = toggleSetItem(selectedLevels.value, level);
    }

    function toggleSource(value: string) {
        selectedSources.value = toggleSetItem(selectedSources.value, value);
    }

    function toggleColumn(column: ColumnId) {
        const next = new Set(visibleColumns.value);
        next.has(column) ? next.delete(column) : next.add(column);
        if (next.size === 0) next.add("message");
        visibleColumns.value = next;
    }

    function clearFilters() {
        selectedSources.value = new Set();
        selectedLevels.value = new Set(logLevels);
    }

    function addNoiseRule() {
        const pattern = customNoiseText.value.trim();
        if (!pattern)
            return;

        if (!noiseRules.value.some((rule) => rule.pattern.toLowerCase() === pattern.toLowerCase()))
            noiseRules.value.push({pattern, enabled: true, tipKey: "tipAddNoiseRule"});
        customNoiseText.value = "";
    }

    function clearView() {
        records.value = [];
        recordKeys.clear();
        selectedRecord.value = null;
        clearSelection();
    }

    function clearSelection() {
        selectedIds.value = new Set();
        lastSelectedKey.value = null;
    }

    function handleRecordClick(record: LogRecord, event: MouseEvent) {
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

        const key = recordKey(record);
        selectedIds.value = new Set([key]);
        lastSelectedKey.value = key;
    }

    function toggleRecordSelection(record: LogRecord) {
        const key = recordKey(record);
        selectedIds.value = toggleSetItem(selectedIds.value, key);
        selectedRecord.value = record;
        lastSelectedKey.value = key;
    }

    function handleRecordSelection(record: LogRecord, event: MouseEvent) {
        selectedRecord.value = record;
        if (event.shiftKey) {
            selectRange(record);
            return;
        }

        toggleRecordSelection(record);
    }

    function toggleFilteredSelection() {
        if (areAllFilteredSelected.value) {
            const visibleKeys = new Set(filteredRecords.value.map(recordKey));
            selectedIds.value = new Set([...selectedIds.value].filter((key) => !visibleKeys.has(key)));
            return;
        }

        const next = new Set(selectedIds.value);
        for (const record of filteredRecords.value)
            next.add(recordKey(record));
        selectedIds.value = next;
    }

    function selectRange(record: LogRecord) {
        const visible = filteredRecords.value;
        const lastKey = lastSelectedKey.value;
        const lastIndex = lastKey == null ? -1 : visible.findIndex((item) => recordKey(item) === lastKey);
        const currentIndex = visible.findIndex((item) => recordKey(item) === recordKey(record));
        if (lastIndex < 0 || currentIndex < 0) {
            toggleRecordSelection(record);
            return;
        }

        const [start, end] = lastIndex < currentIndex ? [lastIndex, currentIndex] : [currentIndex, lastIndex];
        const next = new Set(selectedIds.value);
        for (const item of visible.slice(start, end + 1))
            next.add(recordKey(item));
        selectedIds.value = next;
        selectedRecord.value = record;
    }

    async function copySelectedMessages(formatRecordLine: (record: LogRecord) => string) {
        await navigator.clipboard.writeText(selectedRecords.value.map(formatRecordLine).join("\n"));
    }

    async function copySelectedJsonl() {
        await navigator.clipboard.writeText(selectedRecords.value.map((record) => JSON.stringify(record)).join("\n"));
    }

    async function copyRecordJson() {
        if (selectedRecord.value)
            await navigator.clipboard.writeText(JSON.stringify(selectedRecord.value, null, 2));
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

    function recordKey(record: LogRecord) {
        return `${record.sessionId ?? "unknown"}:${record.id}`;
    }

    function readEnum<T extends string>(key: string, values: readonly T[], fallback: T) {
        const value = localStorage.getItem(storagePrefix + key);
        return values.includes(value as T) ? (value as T) : fallback;
    }

    function readBoolean(key: string, fallback: boolean) {
        const value = localStorage.getItem(storagePrefix + key);
        if (value == null)
            return fallback;
        return value === "1" || value.toLowerCase() === "true";
    }

    function readString(key: string, fallback: string) {
        return localStorage.getItem(storagePrefix + key) ?? fallback;
    }

    function readStringSet<T extends string>(key: string, fallback: Set<T>, allowed?: Set<string>) {
        try {
            const raw = localStorage.getItem(storagePrefix + key);
            if (!raw)
                return new Set(fallback);

            const parsed = JSON.parse(raw) as unknown;
            if (!Array.isArray(parsed))
                return new Set(fallback);

            const values = parsed
                .filter((value): value is T => typeof value === "string" && (!allowed || allowed.has(value)));
            return new Set(values.length > 0 ? values : [...fallback]);
        } catch {
            return new Set(fallback);
        }
    }

    function saveValue(key: string, value: string) {
        localStorage.setItem(storagePrefix + key, value);
    }

    function saveStringSet(key: string, value: Set<string>) {
        saveValue(key, JSON.stringify([...value]));
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

    function toggleSetItem<T>(set: Set<T>, value: T) {
        const next = new Set(set);
        next.has(value) ? next.delete(value) : next.add(value);
        return next;
    }

    return {
        records,
        status,
        connection,
        paused,
        follow,
        keyword,
        regexSearch,
        customNoiseText,
        selectedLevels,
        selectedSources,
        selectedRecord,
        selectedIds,
        payloadOpen,
        themeMode,
        visibleColumns,
        clearOnNewSession,
        noiseRules,
        sources,
        filteredRecords,
        selectedRecords,
        areAllFilteredSelected,
        selectedRecordJson,
        levelCounts,
        suppressedCount,
        activeFilterCount,
        toggleLevel,
        toggleSource,
        toggleColumn,
        clearFilters,
        addNoiseRule,
        clearView,
        clearSelection,
        handleRecordClick,
        handleRecordSelection,
        toggleRecordSelection,
        toggleFilteredSelection,
        copySelectedMessages,
        copySelectedJsonl,
        copyRecordJson,
        exportJsonl,
        recordKey
    };
}
