<script lang="ts" setup>
import {PanelLeftClose, X} from "@lucide/vue";
import {useI18n} from "vue-i18n";
import type {NoiseRule} from "../logTypes";
import {type ColumnId, columnOptions, levelLabel, logLevels} from "../viewerConfig";

defineProps<{
  selectedLevels: Set<string>;
  selectedSources: Set<string>;
  visibleColumns: Set<ColumnId>;
  clearOnNewSession: boolean;
  levelCounts: Map<string, number>;
  sources: { name: string; count: number }[];
  noiseRules: NoiseRule[];
  customNoiseText: string;
  activeFilterCount: number;
}>();

const emit = defineEmits<{
  "update:clearOnNewSession": [value: boolean];
  "update:customNoiseText": [value: string];
  "toggle-level": [value: string];
  "toggle-source": [value: string];
  "toggle-column": [value: ColumnId];
  "toggle-noise": [rule: NoiseRule];
  "add-noise": [];
  "clear-filters": [];
  close: [];
}>();

const {t} = useI18n();
</script>

<template>
  <aside class="filter-pane">
    <div class="pane-head">
      <div>
        <strong>{{ t("filters") }}</strong>
        <span>{{ activeFilterCount }} {{ t("activeFilters") }}</span>
      </div>
      <button v-tooltip="t('close')" class="icon-button" type="button" @click="emit('close')">
        <PanelLeftClose/>
      </button>
    </div>

    <div class="filter-toolbar">
      <button v-tooltip="t('reset')" class="quiet" type="button" @click="emit('clear-filters')">
        <X/>
        <span>{{ t("reset") }}</span>
      </button>
    </div>

    <section class="filter-group always-open">
      <h2>{{ t("levels") }}</h2>
      <div class="level-grid">
        <button
            v-for="level in logLevels"
            :key="level"
            v-tooltip="level"
            :class="['level-chip', level, {active: selectedLevels.has(level)}]"
            type="button"
            @click="emit('toggle-level', level)"
        >
          <span>{{ levelLabel(level) }}</span>
          <strong>{{ levelCounts.get(level) ?? 0 }}</strong>
        </button>
      </div>
    </section>

    <details v-if="sources.length > 0" class="filter-details" open>
      <summary>{{ t("sources") }}</summary>
      <div class="chip-grid">
        <button
            v-for="source in sources"
            :key="source.name"
            v-tooltip="`${t('tipSource')}: ${source.name}`"
            :class="['facet', {active: selectedSources.has(source.name)}]"
            type="button"
            @click="emit('toggle-source', source.name)"
        >
          <span>{{ source.name }}</span>
          <strong>{{ source.count }}</strong>
        </button>
      </div>
    </details>

    <details class="filter-details">
      <summary>{{ t("columns") }}</summary>
      <div class="chip-grid">
        <button
            v-for="column in columnOptions"
            :key="column.id"
            v-tooltip="t(column.tipKey)"
            :class="['facet', {active: visibleColumns.has(column.id)}]"
            type="button"
            @click="emit('toggle-column', column.id)"
        >
          <span>{{ t(column.labelKey) }}</span>
        </button>
      </div>
    </details>

    <details class="filter-details">
      <summary>{{ t("noise") }}</summary>
      <div class="noise-add">
        <input
            :placeholder="t('noisePattern')"
            :value="customNoiseText"
            type="text"
            @input="emit('update:customNoiseText', ($event.target as HTMLInputElement).value)"
            @keydown.enter="emit('add-noise')"
        />
        <button v-tooltip="t('tipAddNoiseRule')" type="button" @click="emit('add-noise')">{{
            t("addNoiseRule")
          }}
        </button>
      </div>
      <div class="chip-grid">
        <button
            v-for="rule in noiseRules"
            :key="rule.pattern"
            v-tooltip="rule.tipKey ? t(rule.tipKey) : rule.pattern"
            :class="['facet', {active: rule.enabled}]"
            type="button"
            @click="emit('toggle-noise', rule)"
        >
          <span>{{ rule.pattern }}</span>
        </button>
      </div>
    </details>

    <label class="check-option">
      <input
          :checked="clearOnNewSession"
          type="checkbox"
          @change="emit('update:clearOnNewSession', ($event.target as HTMLInputElement).checked)"
      />
      <span>{{ t("clearOnNewSession") }}</span>
    </label>
  </aside>
</template>
