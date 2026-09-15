<template>
  <div id="app">
    <ImageUploader />
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import ImageUploader from "./components/ImageUploader.vue";

export default defineComponent({
  name: "App",
  components: {
    ImageUploader,
  },
});
</script>

<style>
input::-webkit-outer-spin-button,
input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
</style>
