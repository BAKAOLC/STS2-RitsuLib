<script lang="ts" setup>
import {Braces, ClipboardList, Copy, Download, Eraser, FileJson, Trash2} from "@lucide/vue";
import {useI18n} from "vue-i18n";

defineProps<{
  selectedCount: number;
  hasFocusedRecord: boolean;
}>();

defineEmits<{
  "copy-lines": [];
  "copy-jsonl": [];
  "copy-json": [];
  "open-payload": [];
  "clear-selection": [];
  export: [];
  "clear-view": [];
}>();

const {t} = useI18n();
</script>

<template>
  <section class="record-actions">
    <div class="selection-state">
      <strong>{{ selectedCount }}</strong>
      <span>{{ t("selected") }}</span>
    </div>
    <button v-tooltip="t('tipCopyLines')" :aria-label="t('copyLines')" :disabled="selectedCount === 0"
            :title="t('copyLines')" type="button" @click="$emit('copy-lines')">
      <Copy/>
    </button>
    <button v-tooltip="t('tipCopyJsonl')" :aria-label="t('copyJsonl')" :disabled="selectedCount === 0"
            :title="t('copyJsonl')" type="button" @click="$emit('copy-jsonl')">
      <ClipboardList/>
    </button>
    <button v-tooltip="t('tipCopyJson')" :aria-label="t('copyJson')" :disabled="!hasFocusedRecord"
            :title="t('copyJson')" type="button" @click="$emit('copy-json')">
      <FileJson/>
    </button>
    <button v-tooltip="t('tipPayload')" :aria-label="t('payload')" :disabled="!hasFocusedRecord" :title="t('payload')"
            type="button" @click="$emit('open-payload')">
      <Braces/>
    </button>
    <button v-tooltip="t('tipClearSelection')" :aria-label="t('clearSelection')" :disabled="selectedCount === 0"
            :title="t('clearSelection')" type="button" @click="$emit('clear-selection')">
      <Eraser/>
    </button>
    <button v-tooltip="t('tipExport')" :aria-label="t('export')" :title="t('export')" type="button"
            @click="$emit('export')">
      <Download/>
    </button>
    <button v-tooltip="t('tipClearView')" :aria-label="t('clearView')" :title="t('clearView')" class="danger"
            type="button" @click="$emit('clear-view')">
      <Trash2/>
    </button>
  </section>
</template>
