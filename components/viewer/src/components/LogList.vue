<script lang="ts" setup>
import {useVirtualizer} from "@tanstack/vue-virtual";
import {useMediaQuery} from "@vueuse/core";
import {type ComponentPublicInstance, computed, onUnmounted, ref, watch} from "vue";
import {useI18n} from "vue-i18n";
import type {LogRecord, MessageSegment} from "../logTypes";
import type {ColumnId} from "../viewerConfig";

const props = defineProps<{
  records: LogRecord[];
  appendedRecordKeys: string[];
  selectedIds: Set<string>;
  selectedRecord: LogRecord | null;
  visibleColumns: Set<ColumnId>;
  follow: boolean;
  allSelected: boolean;
  recordKey: (record: LogRecord) => string;
}>();

const emit = defineEmits<{
  "update:follow": [value: boolean];
  "row-click": [record: LogRecord, event: MouseEvent];
  "toggle-selection": [record: LogRecord, event: MouseEvent];
  "toggle-all": [];
  "open-payload": [record: LogRecord];
}>();

const {t} = useI18n();
const compact = useMediaQuery("(max-width: 900px)");
const logList = ref<HTMLElement | null>(null);
const scrollTop = ref(0);
const enteringKeys = ref(new Set<string>());
let programmaticScroll = false;
let followScrollFrame: number | null = null;

const gridTemplate = computed(() => {
  if (compact.value)
    return "1fr";

  const tracks = ["24px"];
  if (props.visibleColumns.has("id")) tracks.push("56px");
  if (props.visibleColumns.has("time")) tracks.push("74px");
  if (props.visibleColumns.has("level")) tracks.push("58px");
  if (props.visibleColumns.has("origin")) tracks.push("minmax(112px, 180px)");
  if (props.visibleColumns.has("message")) tracks.push("minmax(0, 1fr)");
  return tracks.join(" ");
});

const rowVirtualizer = useVirtualizer(computed(() => ({
  count: props.records.length,
  getScrollElement: () => logList.value,
  estimateSize: (index) => estimateRecordHeight(props.records[index]),
  getItemKey: (index) => props.records[index] ? props.recordKey(props.records[index]) : index,
  overscan: 12,
  useAnimationFrameWithResizeObserver: true
})));

const virtualRows = computed(() => {
  return rowVirtualizer.value.getVirtualItems()
      .map((item) => ({item, record: props.records[item.index]}))
      .filter((row): row is { item: typeof row.item; record: LogRecord } => Boolean(row.record));
});

watch(() => props.records.length, () => {
  if (props.follow)
    scheduleFollowScroll();
});

watch(() => props.follow, (value) => {
  if (value)
    scheduleFollowScroll();
});

onUnmounted(() => {
  if (followScrollFrame != null) {
    window.cancelAnimationFrame(followScrollFrame);
    followScrollFrame = null;
  }
});

watch(() => props.appendedRecordKeys, (keys) => {
  if (keys.length === 0)
    return;

  if (props.follow)
    scheduleFollowScroll();

  const animatedKeys = keys.length > 80 ? keys.slice(-20) : keys;
  enteringKeys.value = new Set([...enteringKeys.value, ...animatedKeys]);
  window.setTimeout(() => {
    const next = new Set(enteringKeys.value);
    for (const key of animatedKeys)
      next.delete(key);
    enteringKeys.value = next;
  }, 520);
}, {flush: "post"});

watch(compact, () => rowVirtualizer.value.measure());

function handleScroll() {
  scrollTop.value = logList.value?.scrollTop ?? 0;
  if (programmaticScroll)
    return;

  emit("update:follow", isAtBottom());
}

function scheduleFollowScroll() {
  if (followScrollFrame != null)
    return;

  followScrollFrame = window.requestAnimationFrame(() => {
    followScrollFrame = null;
    if (props.follow)
      scrollToBottom();
  });
}

function scrollToBottom() {
  if (props.records.length === 0)
    return;

  programmaticScroll = true;
  rowVirtualizer.value.scrollToIndex(props.records.length - 1, {align: "end"});
  requestAnimationFrame(() => {
    const el = logList.value;
    if (el) {
      el.scrollTop = el.scrollHeight;
      scrollTop.value = el.scrollTop;
    }
    programmaticScroll = false;
    emit("update:follow", true);
  });
}

function isAtBottom() {
  const el = logList.value;
  if (!el) return true;
  return el.scrollHeight - el.scrollTop - el.clientHeight <= 8;
}

function estimateRecordHeight(record: LogRecord) {
  if (!record)
    return compact.value ? 74 : 36;

  if (compact.value) {
    const lineCount = record.body.split("\n").length;
    const wrappedLines = Math.floor(record.body.length / 34);
    return Math.min(460, 70 + Math.max(lineCount - 1, wrappedLines) * 18);
  }

  if (record.body.includes("\n") || record.body.length > 160) {
    const lineCount = record.body.split("\n").length;
    const wrappedLines = Math.floor(record.body.length / 180);
    return Math.min(220, 34 + Math.max(lineCount - 1, wrappedLines) * 18);
  }

  return 34;
}

function measureVirtualElement(element: Element | ComponentPublicInstance | null) {
  if (element instanceof Element)
    rowVirtualizer.value.measureElement(element);
}

function pinOffset(item: { start: number; size: number }, record: LogRecord) {
  if (compact.value)
    return 0;

  const distancePastTop = scrollTop.value - item.start;
  if (distancePastTop <= 0)
    return 0;

  return Math.min(distancePastTop, Math.max(0, item.size - pinBlockHeight(record)));
}

function pinBlockHeight(record: LogRecord) {
  return originSubtitle(record) ? 33 : 34;
}

function isExpandedRecord(record: LogRecord) {
  return !compact.value && (record.body.includes("\n") || record.body.length > 160);
}

function formatTime(timestamp: string) {
  return new Date(timestamp).toLocaleTimeString();
}

function originName(record: LogRecord) {
  return record.source || t("game");
}

function originSubtitle(record: LogRecord) {
  return record.category || "";
}

function originTooltip(record: LogRecord) {
  const category = originSubtitle(record);
  return category ? `${originName(record)} / ${category}` : originName(record);
}

function messageSegments(record: LogRecord) {
  return record.bodySegments?.length ? record.bodySegments : [{text: record.body}];
}

function segmentStyle(segment: MessageSegment) {
  return {
    color: segment.color,
    fontWeight: segment.bold ? "750" : undefined,
    opacity: segment.dim ? 0.72 : undefined
  };
}
</script>

<template>
  <main class="log-panel">
    <div v-if="!compact" :style="{gridTemplateColumns: gridTemplate}" class="log-header">
      <label v-tooltip="t('tipSelect')" class="select-cell">
        <input
            :checked="allSelected"
            :disabled="records.length === 0"
            type="checkbox"
            @change="emit('toggle-all')"
        />
      </label>
      <span v-if="visibleColumns.has('id')">{{ t("colId") }}</span>
      <span v-if="visibleColumns.has('time')">{{ t("colTime") }}</span>
      <span v-if="visibleColumns.has('level')">{{ t("colLevel") }}</span>
      <span v-if="visibleColumns.has('origin')">{{ t("colOrigin") }}</span>
      <span v-if="visibleColumns.has('message')">{{ t("colMessage") }}</span>
    </div>

    <div ref="logList" class="log-list" @scroll="handleScroll">
      <div :style="{height: `${rowVirtualizer.getTotalSize()}px`}" class="virtual-pad">
        <div class="virtual-window">
          <article
              v-for="{ item, record } in virtualRows"
              :key="String(item.key)"
              :ref="measureVirtualElement"
              :class="[
                'log-row',
                record.severityText,
                {
                  compact,
                  expanded: isExpandedRecord(record),
                  entering: enteringKeys.has(recordKey(record)),
                  focused: selectedRecord ? recordKey(selectedRecord) === recordKey(record) : false,
                  selected: selectedIds.has(recordKey(record))
                }
              ]"
              :data-index="item.index"
              :style="{gridTemplateColumns: gridTemplate, '--row-y': `${item.start}px`, '--pin-y': `${pinOffset(item, record)}px`}"
              @click="emit('row-click', record, $event)"
              @dblclick="emit('open-payload', record)"
          >
            <template v-if="!compact">
              <label v-tooltip="t('tipSelect')" class="select-cell row-pin">
                <input
                    :checked="selectedIds.has(recordKey(record))"
                    type="checkbox"
                    @click.stop="emit('toggle-selection', record, $event)"
                />
              </label>
              <span v-if="visibleColumns.has('id')" class="id row-pin">#{{ record.id }}</span>
              <span v-if="visibleColumns.has('time')" class="time row-pin">{{ formatTime(record.timestamp) }}</span>
              <span v-if="visibleColumns.has('level')" class="severity row-pin">{{ record.severityText }}</span>
              <span v-if="visibleColumns.has('origin')" v-tooltip="originTooltip(record)" class="origin row-pin">
                <span class="source">{{ originName(record) }}</span>
                <span v-if="originSubtitle(record)" class="category">{{ originSubtitle(record) }}</span>
              </span>
              <span v-if="visibleColumns.has('message')" class="message">
                <span v-for="(segment, segmentIndex) in messageSegments(record)" :key="segmentIndex"
                      :style="segmentStyle(segment)">
                  {{ segment.text }}
                </span>
              </span>
            </template>

            <template v-else>
              <div class="feed-meta">
                <span class="severity">{{ record.severityText }}</span>
                <span>{{ formatTime(record.timestamp) }}</span>
                <label v-tooltip="t('tipSelect')" class="select-cell">
                  <input
                      :checked="selectedIds.has(recordKey(record))"
                      type="checkbox"
                      @click.stop="emit('toggle-selection', record, $event)"
                  />
                </label>
              </div>
              <div v-tooltip="originTooltip(record)" class="feed-origin">
                <span class="source">{{ originName(record) }}</span>
                <span v-if="originSubtitle(record)" class="category">{{ originSubtitle(record) }}</span>
              </div>
              <div class="message feed-message">
                <span v-for="(segment, segmentIndex) in messageSegments(record)" :key="segmentIndex"
                      :style="segmentStyle(segment)">
                  {{ segment.text }}
                </span>
              </div>
            </template>
          </article>
        </div>
      </div>
    </div>
  </main>
</template>
