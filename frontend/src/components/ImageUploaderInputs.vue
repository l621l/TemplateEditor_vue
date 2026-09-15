<template>
  <div class="inputs">
    <div
      class="input-wrapper"
      @mouseover="showTooltip('fullHdLeft')"
      @mouseleave="hideTooltip('fullHdLeft')"
    >
      <input
        :value="fullHdLeft ?? ''"
        @input="$emit('filter-input', $event, 'fullHdLeft')"
        @keyup.enter="$emit('update-image-position-resize')"
        type="number"
        class="coordinate-input"
        placeholder="X"
        id="fullHdLeft"
      />
      <transition name="fade">
        <div v-if="tooltips.fullHdLeft" class="tooltipcoor">Координата X</div>
      </transition>
    </div>

    <div
      class="input-wrapper"
      @mouseover="showTooltip('fullHdTop')"
      @mouseleave="hideTooltip('fullHdTop')"
    >
      <input
        :value="fullHdTop ?? ''"
        @input="$emit('filter-input', $event, 'fullHdTop')"
        @keyup.enter="$emit('update-image-position-resize')"
        type="number"
        class="coordinate-input"
        placeholder="Y"
        id="fullHdTop"
      />
      <transition name="fade">
        <div v-if="tooltips.fullHdTop" class="tooltipcoor">Координата Y</div>
      </transition>
    </div>

    <div
      class="input-wrapper"
      @mouseover="showTooltip('loop')"
      @mouseleave="hideTooltip('loop')"
    >
      <input
        v-show="!isZip"
        :value="loopInput"
        @input="$emit('filter-loop-input', $event)"
        type="text"
        class="coordinate-input"
        placeholder="Sec"
        id="loop"
      />
      <transition name="fade">
        <div v-if="tooltips.loop" class="tooltipcoor">
          Длительность показа картинки в секундах
        </div>
      </transition>
    </div>

    <div
      class="input-wrapper"
      @mouseover="showTooltip('loopAnim')"
      @mouseleave="hideTooltip('loopAnim')"
    >
      <input
        v-show="isZip"
        :value="loopAnim"
        type="text"
        class="coordinate-input"
        placeholder="Sec"
        id="loopAnim"
        disabled
      />
      <transition name="fade">
        <div v-if="tooltips.loopAnim" class="tooltipcoor">
          Длительность анимации в секундах
        </div>
      </transition>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from "vue";

export default defineComponent({
  name: "ImageUploaderInputs",
  props: {
    fullHdLeft: {
      type: Number as PropType<number | null>,
      required: false,
      default: null,
    },
    fullHdTop: {
      type: Number as PropType<number | null>,
      required: false,
      default: null,
    },
    loopInput: { type: String, required: true },
    loopAnim: { type: Number, required: true },
    isZip: { type: Boolean, required: true },
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
  emits: ["filter-input", "update-image-position-resize", "filter-loop-input"],
});
</script>
