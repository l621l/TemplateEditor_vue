<template>
  <div class="panelTop"></div>
  <div class="panelBottom"></div>
  <div class="controls">
    <div
      v-if="!isLinkMode"
      class="select-wrapper"
      @mouseover="showTooltip('broadDropdown')"
      @mouseleave="hideTooltip('broadDropdown')"
    >
      <v-select
        :model-value="selectedBroad"
        @update:modelValue="onBroadChange"
        :items="broadOptions"
        label="Эфир"
        class="image-dropdown"
        id="broad-dropdown"
      ></v-select>
      <transition name="fade">
        <div v-if="tooltips.broadDropdown" class="tooltip">Список эфиров</div>
      </transition>
    </div>

    <div
      v-if="!isLinkMode"
      class="select-wrapper"
      @mouseover="showTooltip('xmlDropdown')"
      @mouseleave="hideTooltip('xmlDropdown')"
    >
      <v-select
        :model-value="selectedXml"
        @update:modelValue="onXmlChange"
        :items="xmlItems"
        label="Шаблон"
        class="image-dropdown"
        id="xml-dropdown"
      ></v-select>
      <transition name="fade">
        <div v-if="tooltips.xmlDropdown" class="tooltip">Список Шаблонов</div>
      </transition>
    </div>

    <div
      class="select-wrapper"
      @mouseover="showTooltip('imageDropdown')"
      @mouseleave="hideTooltip('imageDropdown')"
    >
      <v-select
        :model-value="selectedImage"
        @update:modelValue="onImageChange"
        :items="serverImages"
        label="Картинка"
        class="image-dropdown"
        id="image-dropdown"
      ></v-select>
      <transition name="fade">
        <div v-if="tooltips.imageDropdown" class="tooltip">
          Список доступных изображений и анимаций(zip)
        </div>
      </transition>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from "vue";

export default defineComponent({
  name: "ImageUploaderControls",
  props: {
    isLinkMode: {
      type: Boolean,
      required: true,
    },
    broadOptions: { type: Array as PropType<string[]>, required: true },
    selectedBroad: {
      type: String as PropType<string | null>,
      required: false,
      default: null,
    },
    xmlItems: {
      type: Array as PropType<
        Array<{ title: string; value: number; props: { disabled: boolean } }>
      >,
      required: true,
    },
    selectedXml: {
      type: Number as PropType<number | null>,
      required: false,
      default: null,
    },
    serverImages: { type: Array as PropType<string[]>, required: true },
    selectedImage: {
      type: String as PropType<string | null>,
      required: false,
      default: null,
    },
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
  emits: [
    "update:selectedBroad",
    "select-broad",
    "update:selectedXml",
    "select-xml",
    "update:selectedImage",
    "select-image",
  ],
  setup(props, { emit }) {
    const onBroadChange = (value: string | null) => {
      emit("update:selectedBroad", value);
      emit("select-broad");
    };

    const onXmlChange = (value: number | null) => {
      emit("update:selectedXml", value);
      emit("select-xml");
    };

    const onImageChange = (value: string | null) => {
      emit("update:selectedImage", value);
      emit("select-image");
    };

    return {
      onBroadChange,
      onXmlChange,
      onImageChange,
    };
  },
});
</script>
