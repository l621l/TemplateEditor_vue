<template>
  <div class="buttns">
    <label
      class="file-input-label"
      for="file-upload"
      @mouseover="showTooltip('fileUpload')"
      @mouseleave="hideTooltip('fileUpload')"
    >
      Загрузить
      <transition name="fade">
        <div v-if="tooltips.fileUpload" class="tooltipinp">
          Загрузить изображение или анимацию со своего устройства. Анимацию
          можно загрузить либо архивом, либо выбором всех файлов, которые должны
          быть в анимации.
        </div>
      </transition>
    </label>

    <input
      id="file-upload"
      type="file"
      @change="$emit('file-change', $event)"
      accept=".png,.zip"
      class="file-input"
      multiple
    />

    <label
      class="save-button-label"
      for="save"
      @mouseover="showTooltip('saveButton')"
      @mouseleave="hideTooltip('saveButton')"
    >
      Сохранить
      <transition name="fade">
        <div v-if="tooltips.saveButton" class="tooltipinp">
          Сохранить шаблон
        </div>
      </transition>
    </label>

    <button
      stacked
      class="savebutton"
      @mouseover="showTooltip('saveButton')"
      @mouseleave="hideTooltip('saveButton')"
      @click="$emit('save-xml')"
      id="save"
    ></button>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from "vue";

export default defineComponent({
  name: "ImageUploaderActions",
  props: {
    tooltips: {
      type: Object as PropType<Record<string, boolean>>,
      required: true,
    },
    showTooltip: {
      type: Function as PropType<(key: string) => void>,
      required: true,
    },
    hideTooltip: {
      type: Function as PropType<(key: string) => void>,
      required: true,
    },
  },
  emits: ["file-change", "save-xml"],
});
</script>
