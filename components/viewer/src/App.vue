<script lang="ts" setup>
import {onBeforeUnmount, onMounted, ref, watch} from "vue";
import {useI18n} from "vue-i18n";
import AppToolbar from "./components/AppToolbar.vue";
import FilterBar from "./components/FilterBar.vue";
import LogList from "./components/LogList.vue";
import PayloadDialog from "./components/PayloadDialog.vue";
import RecordActions from "./components/RecordActions.vue";
import SearchBar from "./components/SearchBar.vue";
import {useLogViewer} from "./composables/useLogViewer";
import type {LogRecord, NoiseRule} from "./logTypes";
import {storagePrefix} from "./viewerConfig";

const {t} = useI18n();
const viewer = useLogViewer();
const filtersOpen = ref(readFiltersOpen());
const searchBar = ref<InstanceType<typeof SearchBar> | null>(null);

onMounted(() => {
  window.addEventListener("keydown", handleGlobalKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener("keydown", handleGlobalKeydown);
});

watch(filtersOpen, (value) => {
  localStorage.setItem(storagePrefix + "filtersOpen", value ? "1" : "0");
});

function toggleFollow() {
  viewer.follow.value = !viewer.follow.value;
}

function openPayload(record?: LogRecord) {
  if (record)
    viewer.selectedRecord.value = record;

  if (viewer.selectedRecord.value)
    viewer.payloadOpen.value = true;
}

function toggleNoiseRule(rule: NoiseRule) {
  rule.enabled = !rule.enabled;
}

function resetSearch() {
  viewer.keyword.value = "";
  viewer.regexSearch.value = false;
}

function handleGlobalKeydown(event: KeyboardEvent) {
  const target = event.target as HTMLElement | null;
  const editing = target?.closest("input, textarea, select, [contenteditable='true']");
  const findShortcut = (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "f";
  const slashShortcut = !editing && !event.ctrlKey && !event.metaKey && !event.altKey && event.key === "/";
  if (!findShortcut && !slashShortcut)
    return;

  event.preventDefault();
  searchBar.value?.focusSearch();
}

function readFiltersOpen() {
  const saved = localStorage.getItem(storagePrefix + "filtersOpen");
  if (saved != null)
    return saved === "1" || saved.toLowerCase() === "true";

  return window.matchMedia("(min-width: 900px)").matches;
}

function originName(record: LogRecord) {
  return record.source || t("game");
}

function formatRecordLine(record: LogRecord) {
  const origin = record.source ? ` [${originName(record)}${record.category ? `/${record.category}` : ""}]` : "";
  return `${formatTime(record.timestamp)} ${record.severityText}${origin} ${record.body}`;
}

function formatTime(timestamp: string) {
  return new Date(timestamp).toLocaleTimeString();
}
</script>

<template>
  <div :data-theme="viewer.themeMode.value" class="shell">
    <AppToolbar
        v-model:paused="viewer.paused.value"
        v-model:theme-mode="viewer.themeMode.value"
        :connection="viewer.connection.value"
        :filtered-count="viewer.filteredRecords.value.length"
        :filters-open="filtersOpen"
        :follow="viewer.follow.value"
        :status="viewer.status.value"
        :suppressed-count="viewer.suppressedCount.value"
        :total-count="viewer.records.value.length"
        @toggle-filters="filtersOpen = !filtersOpen"
        @toggle-follow="toggleFollow"
    />

    <SearchBar
        ref="searchBar"
        v-model:keyword="viewer.keyword.value"
        v-model:regex-search="viewer.regexSearch.value"
        :filtered-count="viewer.filteredRecords.value.length"
        :total-count="viewer.records.value.length"
        @reset="resetSearch"
    />

    <div :class="{closed: !filtersOpen}" class="workspace">
      <FilterBar
          v-model:clear-on-new-session="viewer.clearOnNewSession.value"
          v-model:custom-noise-text="viewer.customNoiseText.value"
          :active-filter-count="viewer.activeFilterCount.value"
          :level-counts="viewer.levelCounts.value"
          :noise-rules="viewer.noiseRules.value"
          :selected-levels="viewer.selectedLevels.value"
          :selected-sources="viewer.selectedSources.value"
          :sources="viewer.sources.value"
          :visible-columns="viewer.visibleColumns.value"
          @close="filtersOpen = false"
          @add-noise="viewer.addNoiseRule"
          @clear-filters="viewer.clearFilters"
          @toggle-column="viewer.toggleColumn"
          @toggle-level="viewer.toggleLevel"
          @toggle-noise="toggleNoiseRule"
          @toggle-source="viewer.toggleSource"
      />

      <section class="main-stage">
        <RecordActions
            :has-focused-record="Boolean(viewer.selectedRecord.value)"
            :selected-count="viewer.selectedRecords.value.length"
            @export="viewer.exportJsonl"
            @clear-selection="viewer.clearSelection"
            @clear-view="viewer.clearView"
            @copy-json="viewer.copyRecordJson"
            @copy-jsonl="viewer.copySelectedJsonl"
            @copy-lines="viewer.copySelectedMessages(formatRecordLine)"
            @open-payload="openPayload"
        />

        <LogList
            v-model:follow="viewer.follow.value"
            :appended-record-keys="viewer.appendedRecordKeys.value"
            :all-selected="viewer.areAllFilteredSelected.value"
            :record-key="viewer.recordKey"
            :records="viewer.filteredRecords.value"
            :selected-ids="viewer.selectedIds.value"
            :selected-record="viewer.selectedRecord.value"
            :visible-columns="viewer.visibleColumns.value"
            @open-payload="openPayload"
            @row-click="viewer.handleRecordClick"
            @toggle-all="viewer.toggleFilteredSelection"
            @toggle-selection="viewer.handleRecordSelection"
        />
      </section>
    </div>

    <PayloadDialog
        v-if="viewer.payloadOpen.value && viewer.selectedRecord.value"
        :record="viewer.selectedRecord.value"
        :record-json="viewer.selectedRecordJson.value"
        @close="viewer.payloadOpen.value = false"
        @copy="viewer.copyRecordJson"
    />
  </div>
</template>
