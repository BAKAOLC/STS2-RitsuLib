<script lang="ts" setup>
import {Regex, Search, X} from "@lucide/vue";
import {ref} from "vue";
import {useI18n} from "vue-i18n";

defineProps<{
  keyword: string;
  regexSearch: boolean;
  filteredCount: number;
  totalCount: number;
}>();

const emit = defineEmits<{
  "update:keyword": [value: string];
  "update:regexSearch": [value: boolean];
  reset: [];
}>();

const {t} = useI18n();
const input = ref<HTMLInputElement | null>(null);

function focusSearch() {
  input.value?.focus();
  input.value?.select();
}

defineExpose({focusSearch});
</script>

<template>
  <section class="search-strip">
    <label class="search-field">
      <Search/>
      <input
          ref="input"
          :aria-label="t('search')"
          :placeholder="t('searchPlaceholder')"
          :value="keyword"
          type="search"
          @input="emit('update:keyword', ($event.target as HTMLInputElement).value)"
      />
    </label>

    <button v-tooltip="t('tipRegex')" :class="{active: regexSearch}" type="button"
            @click="emit('update:regexSearch', !regexSearch)">
      <Regex/>
      <span>{{ t("regexMode") }}</span>
    </button>
    <button v-tooltip="t('reset')" :disabled="!keyword && !regexSearch" :title="t('reset')" class="quiet" type="button"
            @click="emit('reset')">
      <X/>
      <span>{{ t("reset") }}</span>
    </button>

    <div class="search-count">
      <strong>{{ filteredCount }}</strong>
      <span>/ {{ totalCount }}</span>
    </div>
  </section>
</template>
