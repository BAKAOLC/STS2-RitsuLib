<script lang="ts" setup>
import {Languages, ListFilter, ListRestart, Moon, Pause, Play, Sun} from "@lucide/vue";
import {useI18n} from "vue-i18n";
import type {ConnectionState, Status, ThemeMode} from "../logTypes";
import {storagePrefix} from "../viewerConfig";

defineProps<{
  connection: ConnectionState;
  filteredCount: number;
  totalCount: number;
  suppressedCount: number;
  status: Status | null;
  paused: boolean;
  follow: boolean;
  themeMode: ThemeMode;
  filtersOpen: boolean;
}>();

const emit = defineEmits<{
  "update:paused": [value: boolean];
  "toggle-follow": [];
  "update:themeMode": [value: ThemeMode];
  "toggle-filters": [];
}>();

const {t, locale} = useI18n();

function toggleLocale() {
  const next = locale.value === "zh-CN" ? "en-US" : "zh-CN";
  locale.value = next;
  localStorage.setItem(storagePrefix + "locale", next);
}
</script>

<template>
  <header class="topbar">
    <div class="brand">
      <h1>{{ t("appTitle") }}</h1>
      <span :class="['connection', connection]"><span class="status-dot"></span>{{ t(connection) }}</span>
    </div>

    <div class="stats">
      <span><strong>{{ filteredCount }}</strong>{{ t("visible") }}</span>
      <span><strong>{{ totalCount }}</strong>{{ t("total") }}</span>
      <span><strong>{{ suppressedCount }}</strong>{{ t("suppressed") }}</span>
      <span v-if="status"><strong>{{ status.queueDepth }}</strong>{{ t("queue") }}</span>
    </div>

    <div class="top-actions">
      <button v-tooltip="t('filters')" :class="{active: filtersOpen}" type="button" @click="emit('toggle-filters')">
        <ListFilter/>
        <span>{{ t("filters") }}</span>
      </button>
      <button v-tooltip="t('tipPause')" :class="{active: paused}" type="button" @click="emit('update:paused', !paused)">
        <Play v-if="paused"/>
        <Pause v-else/>
        <span>{{ paused ? t("resume") : t("pause") }}</span>
      </button>
      <button v-tooltip="t('tipFollow')" :class="{active: follow}" type="button" @click="emit('toggle-follow')">
        <ListRestart/>
        <span>{{ follow ? t("following") : t("followOff") }}</span>
      </button>
      <button v-tooltip="t('tipTheme')" type="button"
              @click="emit('update:themeMode', themeMode === 'dark' ? 'light' : 'dark')">
        <Sun v-if="themeMode === 'dark'"/>
        <Moon v-else/>
      </button>
      <button v-tooltip="t('tipLanguage')" type="button" @click="toggleLocale">
        <Languages/>
      </button>
    </div>
  </header>
</template>
