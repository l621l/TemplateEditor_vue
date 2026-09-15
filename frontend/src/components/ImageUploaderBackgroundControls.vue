<template>
  <div class="button-container">
    <transition name="fade">
      <label
        v-show="isButton1"
        class="toggle-button-label"
        :class="{ darker: !isButtonDarker }"
        for="toggle-button"
      >
        ⊞
      </label>
    </transition>

    <button
      class="toggle-button"
      id="toggle-button"
      @click="$emit('toggle-lines-visibility')"
    ></button>

    <transition name="fade">
      <label
        v-show="isButton2"
        class="upload-button-label"
        :class="{ darker: isBackgroundSet }"
        for="upload-button"
      >
        ◪
      </label>
    </transition>

    <input
      type="file"
      class="upload-button"
      id="upload-button"
      accept="image/*"
      @change="$emit('upload-background-image', $event)"
    />

    <transition name="fade">
      <label
        v-show="isBackgroundSet && !isBackgroundRemoving"
        class="remove-button-label"
        :class="{ darker: isBackgroundSet }"
        for="remove-button"
      >
        ✖
      </label>
    </transition>

    <button
      v-if="isBackgroundSet"
      class="remove-button"
      id="remove-button"
      @click="$emit('start-remove-background-image')"
    ></button>
  </div>

  <div class="percent">
    <v-tabs
      :model-value="activeTab"
      class="tab"
      @update:modelValue="$emit('update:activeTab', $event)"
    >
      <v-tab value="fit" @click="$emit('tab-event', 0)">fit</v-tab>
      <v-tab value="50" @click="$emit('tab-event', 50)">50%</v-tab>
      <v-tab value="75" @click="$emit('tab-event', 75)">75%</v-tab>
      <v-tab value="100" @click="$emit('tab-event', 100)">100%</v-tab>
    </v-tabs>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";

export default defineComponent({
  name: "ImageUploaderBackgroundControls",
  props: {
    isButton1: { type: Boolean, required: true },
    isButton2: { type: Boolean, required: true },
    isButtonDarker: { type: Boolean, required: true },
    isBackgroundSet: { type: Boolean, required: true },
    isBackgroundRemoving: { type: Boolean, required: true },
    activeTab: { type: String, required: true },
  },
  emits: [
    "toggle-lines-visibility",
    "upload-background-image",
    "start-remove-background-image",
    "tab-event",
    "update:activeTab",
  ],
});
</script>
