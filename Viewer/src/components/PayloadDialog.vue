<script lang="ts" setup>
import {useI18n} from "vue-i18n";
import type {LogRecord} from "../logTypes";

defineProps<{
  record: LogRecord;
  recordJson: string;
}>();

defineEmits<{
  close: [];
  copy: [];
}>();

const {t} = useI18n();

function originName(record: LogRecord) {
  return record.source || t("game");
}
</script>

<template>
  <div class="payload-backdrop" @click.self="$emit('close')">
    <section class="payload-panel">
      <div class="payload-head">
        <div>
          <strong>{{ record.severityText }}</strong>
          <span>{{ originName(record) }}</span>
        </div>
        <div class="payload-actions">
          <button v-tooltip="t('tipCopyJson')" type="button" @click="$emit('copy')">{{ t("copyJson") }}</button>
          <button v-tooltip="t('close')" type="button" @click="$emit('close')">{{ t("close") }}</button>
        </div>
      </div>
      <pre class="payload-message">{{ record.body }}</pre>
      <pre>{{ recordJson }}</pre>
    </section>
  </div>
</template>
