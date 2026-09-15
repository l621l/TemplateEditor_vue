<template>
  <div class="container" :class="{ 'loading-cursor': isProgress }">
    <div v-if="isProgress" class="progress-bar-container">
      <div class="progress-bar"></div>
    </div>
    <div
      class="gray-field"
      :class="{ 'fade-in': isBackgroundSet, 'fade-out': isBackgroundRemoving }"
      :style="{
        width: fieldWidth + 'px',
        height: fieldHeight + 'px',
        left: fieldLeft + 'px',
        top: fieldTop + 'px',
      }"
    >
      <div
        v-for="(line, index) in lines"
        :key="index"
        class="rectangle"
        :class="{ 'fade-out': !linesVisible }"
        :style="{
          top: line.top,
          left: line.left,
          width: line.width,
          height: line.height,
        }"
      ></div>
    </div>

    <ImageUploaderBackgroundControls
      :is-button1="isButton1"
      :is-button2="isButton2"
      :is-button-darker="isButtonDarker"
      :is-background-set="isBackgroundSet"
      :is-background-removing="isBackgroundRemoving"
      :active-tab="activeTab"
      @toggle-lines-visibility="toggleLinesVisibility"
      @upload-background-image="uploadBackgroundImage"
      @start-remove-background-image="startRemoveBackgroundImage"
      @tab-event="tabEvent"
      @update:activeTab="activeTab = $event"
    />

    <ImageUploaderControls
      :is-link-mode="isLinkMode"
      :broad-options="broadOptions"
      :selected-broad="selectedBroad"
      :xml-items="xmlItems"
      :selected-xml="selectedXml"
      :server-images="serverImages"
      :selected-image="selectedImage"
      :tooltips="tooltips"
      :show-tooltip="showTooltip"
      :hide-tooltip="hideTooltip"
      @update:selectedBroad="selectedBroad = $event"
      @select-broad="selectBroadFromDropdown"
      @update:selectedXml="selectedXml = $event"
      @select-xml="selectXmlFromDropdown"
      @update:selectedImage="selectedImage = $event"
      @select-image="selectImageFromDropdown"
    />

    <img
      v-if="imageUrl"
      :src="imageUrl"
      alt="Selected Image"
      :style="{
        left: left + 'px',
        top: top + 'px',
        width: imageWidth + 'px',
        height: imageHeight + 'px',
      }"
      class="draggable-image"
      @mousedown="startDragging"
      @dragstart.prevent
    />

    <ImageUploaderInputs
      :full-hd-left="fullHdLeft"
      :full-hd-top="fullHdTop"
      :loop-input="loopInput"
      :loop-anim="loopAnim"
      :is-zip="isZip"
      :tooltips="tooltips"
      :show-tooltip="showTooltip"
      :hide-tooltip="hideTooltip"
      @filter-input="filterInput"
      @update-image-position-resize="updateImagePositionResize"
      @filter-loop-input="filterLoopInput"
    />

    <ImageUploaderActions
      :tooltips="tooltips"
      :show-tooltip="showTooltip"
      :hide-tooltip="hideTooltip"
      @file-change="onFileChange"
      @save-xml="saveXml"
    />
  </div>

  <img class="ico" :src="icon" alt="Home" @click="goHome" />
</template>

<script lang="ts">
import { defineComponent, ref, onMounted, onUnmounted, nextTick } from "vue";
import icon from "../assets/ReFin.jpg";
import axios from "axios";
import { getApiBaseUrl } from "../runtime-config";
import { toast } from "vue3-toastify";
import "vue3-toastify/dist/index.css";
import JSZip from "jszip";
import Swal from "sweetalert2";
import ImageUploaderBackgroundControls from "./ImageUploaderBackgroundControls.vue";
import ImageUploaderControls from "./ImageUploaderControls.vue";
import ImageUploaderInputs from "./ImageUploaderInputs.vue";
import ImageUploaderActions from "./ImageUploaderActions.vue";

export default defineComponent({
  name: "ImageUploader",
  components: {
    ImageUploaderBackgroundControls,
    ImageUploaderControls,
    ImageUploaderInputs,
    ImageUploaderActions,
  },

  setup() {
    const myApi = getApiBaseUrl();
    const fieldWidth = ref(0);
    const fieldHeight = ref(0);
    const fieldLeft = ref(0);
    const fieldTop = ref(0);
    const linesVisible = ref(false);
    const isButtonDarker = ref(false);
    const backgroundImage = ref("");
    const isBackgroundSet = ref(false);
    const isBackgroundRemoving = ref(false);
    const isButton1 = ref(false);
    const isButton2 = ref(false);
    const scalePercent = ref(50);
    const activeTab = ref("50");

    const lines = [
      { top: "10%", left: "10%", width: "80%", height: "1px" },
      { top: "5%", left: "5%", width: "90%", height: "1px" },
      { top: "90%", left: "10%", width: "80%", height: "1px" },
      { top: "95%", left: "5%", width: "90%", height: "1px" },
      { top: "10%", left: "10%", height: "80%", width: "1px" },
      { top: "5%", left: "5%", height: "90%", width: "1px" },
      { top: "10%", left: "90%", height: "80%", width: "1px" },
      { top: "5%", left: "95%", height: "90%", width: "1px" },
      { top: "50%", left: "8%", width: "4%", height: "1px" },
      { top: "50%", left: "88%", width: "4%", height: "1px" },
      { top: "7%", left: "50%", height: "6%", width: "1px" },
      { top: "87%", left: "50%", height: "6%", width: "1px" },
    ];

    const tabEvent = (percent: number) => {
      if (percent === 0) {
        localStorage.setItem("actTab", "fit");
        scalePercent.value = Math.min(
          Math.round(((window.innerHeight - 110) / 1080) * 100),
          Math.round(((window.innerWidth - 8) / 1920) * 100),
        );
      } else {
        localStorage.setItem("actTab", percent.toString());
        scalePercent.value = percent;
      }
      localStorage.setItem("scale", scalePercent.value.toString());
      rightSize();
    };

    const rightSize = () => {
      fieldWidth.value = 1920 * (scalePercent.value / 100);
      fieldHeight.value = 1080 * (scalePercent.value / 100);
      fieldLeft.value = Math.max(
        (window.innerWidth - fieldWidth.value) / 2 - 8,
        0,
      );
      fieldTop.value = Math.max(
        (window.innerHeight - fieldHeight.value) / 2,
        56,
      );
      const container = document.querySelector(".container") as HTMLElement;
      if (container) {
        container.style.paddingBottom =
          (fieldHeight.value - 3).toString() + "px";
      }
      if (isProgress.value) {
        centerProgressBar();
      }
      updateImageSize();
    };

    const toggleLinesVisibility = () => {
      linesVisible.value = !linesVisible.value;
      isButtonDarker.value = !isButtonDarker.value;
      localStorage.setItem("linesVisible", linesVisible.value.toString());
      localStorage.setItem("isButtonDarker", isButtonDarker.value.toString());
    };

    const uploadBackgroundImage = (event: Event) => {
      const input = event.target as HTMLInputElement;
      if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = (e) => {
          const base64Image = e.target?.result as string;
          backgroundImage.value = `url(${base64Image})`;
          localStorage.setItem("backgroundImage", base64Image);
          localStorage.setItem("isBackgroundSet", "true"); 
          isBackgroundSet.value = true;
        };
        reader.readAsDataURL(input.files[0]);
      }
      input.value = ""; 
    };

    const startRemoveBackgroundImage = async () => {
      isBackgroundRemoving.value = true;
      isBackgroundSet.value = false;
      await nextTick();
      setTimeout(() => {
        removeBackgroundImage();
      }, 1000);
    };

    const removeBackgroundImage = () => {
      backgroundImage.value = "";
      isBackgroundRemoving.value = false;
      localStorage.removeItem("backgroundImage");
      localStorage.setItem("isBackgroundSet", "false");
    };

    const goHome = () => {
      window.location.href = "/";
    };

    const loadBackgroundImageFromLocalStorage = () => {
      const base64Image = localStorage.getItem("backgroundImage");
      if (base64Image) {
        backgroundImage.value = `url(${base64Image})`;
      }
      const isBackgroundSetFromStorage =
        localStorage.getItem("isBackgroundSet");
      if (isBackgroundSetFromStorage === "true") {
        isBackgroundSet.value = true;
      }
    };

    const imageUrl = ref<string | null>(null);
    const imageFile = ref<File | null>(null);
    const zipFile = ref<File | null>(null);
    const left = ref(0);
    const top = ref(0);
    const isDragging = ref(false);
    const startX = ref(0);
    const startY = ref(0);
    const imageWidth = ref(0);
    const imageHeight = ref(0);
    const loopInput = ref<string>("");
    const originalWidth = ref(0);
    const originalHeight = ref(0);
    const serverImages = ref<string[]>([]);
    const selectedImage = ref<string | null>(null);
    const aspectRatio = ref(1);
    const fullHdLeft = ref<number | null>(null);
    const fullHdTop = ref<number | null>(null);
    const loop = ref<number | null>(null);
    const isProgress = ref(false);
    const isLoading = ref(false);
    const progress = ref(0);
    let disabledList = <number[]>[];
    let tooltipTimeout: ReturnType<typeof setTimeout> | null = null;
    const tooltips = ref({
      fileUpload: false,
      imageDropdown: false,
      broadDropdown: false,
      xmlDropdown: false,
      fullHdLeft: false,
      fullHdTop: false,
      loop: false,
      saveButton: false,
      loopAnim: false,
    });
    const isZip = ref(false);
    const loopAnim = ref(0);

    const broadOptions = ref<string[]>([]);
    const selectedBroad = ref<string | null>(null);
    let xmlOptions = <string[]>[];
    const xmlItems = ref<
      { title: string; value: number; props: { disabled: boolean } }[]
    >([]);

    const selectedXml = ref<number | null>(null);
    const isLinkMode = ref(false);
    const linkBroadcast = ref<number | null>(null);
    const linkTemplate = ref<string | null>(null);
    const linkXmlPath = ref<string | null>(null);

    const showLinkModeError = (text: string) => {
      toast.error(text, {
        position: "top-right",
        autoClose: 3000,
        transition: "flip",
        hideProgressBar: true,
        pauseOnHover: false,
        theme: "dark",
      });
    };

    const readLinkModeParams = () => {
      const params = new URLSearchParams(window.location.search);
      const broadcastParam = params.get("preset");
      const templateParam = params.get("template");
      console.log(broadcastParam + " " + templateParam);
      if (broadcastParam !== null && templateParam) {
        const parsedBroadcast = parseInt(broadcastParam, 10);
        if (!isNaN(parsedBroadcast)) {
          isLinkMode.value = true;
          linkBroadcast.value = parsedBroadcast;
          linkTemplate.value = templateParam;
        }
      }
    };

    function delay(ms: number) {
      return new Promise((resolve) => setTimeout(resolve, ms));
    }

    const getSelectedBroadIndex = () => {
      if (selectedBroad.value === null) {
        return -1;
      }

      return broadOptions.value.findIndex((x) => x === selectedBroad.value);
    };

    const getCurrentImageRequestParams = () => {
      const broad = getSelectedBroadIndex();
      const xml =
        selectedXml.value !== null && selectedXml.value !== undefined
          ? selectedXml.value
          : -1;

      return { broad, xml };
    };

    let isDisposed = false;
    let imageRevision = 0;
    let previewVersion = 0;
    let previewRequest: AbortController | null = null;
    let cancelPreviewImage: (() => void) | null = null;
    let activePreviewUrl: string | null = null;
    const previewUrls = new Set<string>();

    const releasePreviewUrl = (url: string) => {
      if (previewUrls.delete(url)) URL.revokeObjectURL(url);
    };

    const stopPreview = () => {
      previewVersion++;
      isLoading.value = false;
      previewRequest?.abort();
      previewRequest = null;
      cancelPreviewImage?.();
      cancelPreviewImage = null;
      imageUrl.value = null;
      activePreviewUrl = null;
      previewUrls.forEach(releasePreviewUrl);
    };

    const showPreviewImage = async (
      source: string | Blob,
      version: number,
    ): Promise<boolean> => {
      if (isDisposed || version !== previewVersion) return false;
      const isObjectUrl = typeof source !== "string";
      const url =
        typeof source === "string" ? source : URL.createObjectURL(source);
      if (isObjectUrl) previewUrls.add(url);

      try {
        const size = await new Promise<{
          width: number;
          height: number;
        } | null>((resolve, reject) => {
          const img = new Image();
          const cleanup = () => {
            img.onload = null;
            img.onerror = null;
            if (cancelPreviewImage === cancel) cancelPreviewImage = null;
          };
          const cancel = () => {
            cleanup();
            img.src = "";
            resolve(null);
          };
          cancelPreviewImage = cancel;
          img.onload = () => {
            cleanup();
            resolve({ width: img.width, height: img.height });
          };
          img.onerror = () => {
            cleanup();
            reject(new Error("Не удалось прочитать изображение."));
          };
          img.src = url;
        });
        if (!size || isDisposed || version !== previewVersion) return false;

        const previousUrl = activePreviewUrl;
        activePreviewUrl = isObjectUrl ? url : null;
        imageUrl.value = url;
        originalWidth.value = size.width;
        originalHeight.value = size.height;
        updateImageSize();
        await nextTick();
        if (previousUrl) releasePreviewUrl(previousUrl);
        return !isDisposed && version === previewVersion;
      } finally {
        if (isObjectUrl && activePreviewUrl !== url) releasePreviewUrl(url);
      }
    };

    const isCurrentSelection = (request: { broad: number; xml: number }) => {
      const current = getCurrentImageRequestParams();
      return (
        !isDisposed &&
        current.broad === request.broad &&
        current.xml === request.xml
      );
    };

    const applyUploadedImage = async (
      result: { fileName: string; x: string | number; y: string | number },
      request: { broad: number; xml: number },
    ) => {
      if (!isCurrentSelection(request)) return;
      await fetchImages();
      if (!isCurrentSelection(request)) return;
      imageRevision++;
      await selectImageByName(result.fileName);
      if (
        !isCurrentSelection(request) ||
        selectedImage.value !== result.fileName
      )
        return;
      fullHdTop.value = Number(result.y);
      fullHdLeft.value = Number(result.x);
      updateImagePositionResize();
    };

    const filterInput = (
      event: Event,
      coordinate: "fullHdLeft" | "fullHdTop",
    ) => {
      const input = event.target as HTMLInputElement;
      const value = input.value;
      const filteredValue = value.replace("-", "");
      if (coordinate === "fullHdLeft") {
        fullHdLeft.value = Number(filteredValue);
      } else if (coordinate === "fullHdTop") {
        fullHdTop.value = Number(filteredValue);
      }
      input.value = filteredValue;
    };

    const filterLoopInput = (event: Event) => {
      const input = event.target as HTMLInputElement;
      const value = input.value.replace(",", ".");
      if (/^\d*\.?\d*\$/.test(value)) {
        loopInput.value = value;
        loop.value = parseFloat(value);
      } else {
        input.value = loopInput.value;
      }
    };

    const onFileChange = async (event: Event) => {
      // обработка выбора файла
      const input = event.target as HTMLInputElement;
      const selectedFiles = Array.from(input.files ?? []);
      input.value = "";
      if (selectedFiles.length === 0) return;

      const onlyPng = selectedFiles.every((file) => /\.png$/i.test(file.name));
      const singleZip =
        selectedFiles.length === 1 && /\.zip$/i.test(selectedFiles[0].name);
      if (!onlyPng && !singleZip) {
        showLinkModeError(
          "Выберите PNG, последовательность PNG или один ZIP-архив.",
        );
        return;
      }

      stopPreview();
      const version = previewVersion;
      try {
        if (singleZip) {
          zipFile.value = selectedFiles[0];
          await uploadZip();
        } else if (selectedFiles.length === 1) {
          imageFile.value = selectedFiles[0];
          if (await showPreviewImage(selectedFiles[0], version)) {
            await uploadImage(selectedFiles[0]);
          }
        } else {
          const formData = new FormData();
          selectedFiles.forEach((file) => formData.append("files", file));
          await uploadFiles(formData);
        }
      } catch (error) {
        if (version === previewVersion && !isDisposed) {
          showLinkModeError("Не удалось прочитать выбранное изображение.");
          console.error("Upload preview error:", error);
        }
      }
    };

    const getProgress = async () => {
      // функция для запроса прогресса
      try {
        while (isProgress.value) {
          const response = await axios.get(myApi + "/api/image/getprog");
          progress.value = response.data;
          updateProgressBarWidth();
          updateProgressBarColor();
          await delay(100);
        }
      } catch (error) {
        console.error("Progress error: ", error);
      }
    };

    const uploadImage = async (file: File) => {
      // функция для постановки изображения
      const formData = new FormData();
      formData.append("file", file);

      const { broad, xml } = getCurrentImageRequestParams();
      formData.append("broad", broad.toString());
      formData.append("xml", xml.toString());

      try {
        isProgress.value = true;
        const response = await axios.post(
          myApi + "/api/image/upload",
          formData,
          {
            headers: {
              "Content-Type": "multipart/form-data",
            },
          },
        );
        await applyUploadedImage(response.data, { broad, xml });
        console.log("Image uploaded: ", response.data);
      } catch (error) {
        console.error("Error uploading image:", error);
      } finally {
        isProgress.value = false;
      }
    };

    const uploadFiles = async (formData: FormData) => {
      // функция для постановки анимации из набора файлов
      const { broad, xml } = getCurrentImageRequestParams();
      formData.append("broad", broad.toString());
      formData.append("xml", xml.toString());

      try {
        isProgress.value = true;
        await delay(0);
        centerProgressBar();
        getProgress();
        const response = await axios.post(
          myApi + "/api/image/upload",
          formData,
          {
            headers: {
              "Content-Type": "multipart/form-data",
            },
          },
        );
        await applyUploadedImage(response.data, { broad, xml });
      } catch (error) {
        console.error("Error uploading files:", error);
      } finally {
        isProgress.value = false;
      }
    };

    const uploadZip = async () => {
      // функция для постановки анимации из архива
      if (zipFile.value) {
        const formData = new FormData();
        formData.append("file", zipFile.value);

        const { broad, xml } = getCurrentImageRequestParams();
        formData.append("broad", broad.toString());
        formData.append("xml", xml.toString());
        try {
          isProgress.value = true;
          await delay(0);
          centerProgressBar();
          getProgress();
          const response = await axios.post(
            myApi + "/api/image/uploadzip",
            formData,
            {
              headers: {
                "Content-Type": "multipart/form-data",
              },
            },
          );
          await applyUploadedImage(response.data, { broad, xml });
        } catch (error) {
          console.error("Error uploading ZIP:", error);
        } finally {
          isProgress.value = false;
        }
      }
    };

    const fetchImages = async () => {
      // функция для обновления списка анимаций и картинок
      const { broad, xml } = getCurrentImageRequestParams();
      try {
        const response = await axios.get(myApi + "/api/image/list", {
          params: { broad, xml },
        });
        if (isCurrentSelection({ broad, xml }))
          serverImages.value = response.data;
      } catch (error) {
        console.error("Error fetching images:", error);
      }
    };

    const fetchBroadOptions = async () => {
      // функция для обновления списка broadOptions
      try {
        const response = await axios.get(myApi + "/api/image/getbroad");
        broadOptions.value = response.data;
        if (broadOptions.value.length > 0) {
          if (!isLinkMode.value) {
            selectedBroad.value = broadOptions.value[0];
            await fetchXmlOptions();
          } else {
            await applyLinkModeIfNeeded();
          }
        }
      } catch (error) {
        console.error("Error fetching broad options:", error);
      }
    };

    const fetchXmlOptions = async () => {
      // функция для обновления списка xmlOptions
      if (selectedBroad.value !== null) {
        try {
          const ind = broadOptions.value.findIndex(
            (x) => x === selectedBroad.value,
          );
          const response = await axios.get(myApi + "/api/image/getxml", {
            params: {
              broad: ind,
            },
          });
          xmlOptions = response.data.xml;
          disabledList = response.data.undef;
          xmlItems.value.splice(0, xmlItems.value.length);
          for (let i = 0; i < xmlOptions.length; i++) {
            xmlItems.value.push({
              title: xmlOptions[i],
              value: i,
              props: { disabled: disabledList.includes(i) },
            });
          }
          selectedXml.value = null;
        } catch (error) {
          console.error("Error fetching xml options:", error);
        }
      }
    };

    const applyLinkModeIfNeeded = async () => {
      if (!isLinkMode.value) return;

      if (
        linkBroadcast.value === null ||
        linkTemplate.value === null ||
        broadOptions.value.length === 0
      ) {
        return;
      }

      if (
        linkBroadcast.value < 0 ||
        linkBroadcast.value >= broadOptions.value.length
      ) {
        showLinkModeError(
          "Эфир с индексом " + linkBroadcast.value.toString() + " не найден!",
        );
        return;
      }

      selectedBroad.value = broadOptions.value[linkBroadcast.value];

      await fetchXmlOptions();

      const response = await axios.get(myApi + "/api/image/getXmlByBind", {
        params: {
          broad: linkBroadcast.value,
          bind: linkTemplate.value,
        },
      });

      if (!response.data.pathToXml) {
        showLinkModeError("Шаблон не был найден!");
        return;
      }

      linkXmlPath.value = response.data.pathToXml;

      const xmlIndex = xmlOptions.findIndex((x) => x === response.data.text);
      if (xmlIndex !== -1) {
        selectedXml.value = xmlIndex;
      }

      await fetchImages();

      fullHdLeft.value = parseInt(response.data.result[0], 10);
      fullHdTop.value = parseInt(response.data.result[1], 10);
      loopInput.value = response.data.result[2];
      selectImageByName(response.data.result[3]);
    };

    const getImageUrl = (fileName: string) => {
      // функция для получения url картинки
      const params = new URLSearchParams();
      const { broad, xml } = getCurrentImageRequestParams();

      params.set("broad", broad.toString());
      params.set("xml", xml.toString());
      if (imageRevision > 0) params.set("v", imageRevision.toString());

      return myApi + `/api/image/${fileName}?${params.toString()}`;
    };

    const selectImageFromDropdown = async () => {
      // функция для обработки выбора анимации или картинки из списка
      const selectedFileName = selectedImage.value;
      stopPreview();
      isZip.value = false;
      if (!selectedFileName || isDisposed) return;
      const version = previewVersion;
      const match = selectedFileName.match(/=(\d+)x(\d+)(?:\.[^.]+)?$/);
      if (match) {
        fullHdTop.value = Number(match[2]);
        fullHdLeft.value = Number(match[1]);
      }

      try {
        if (/\.zip$/i.test(selectedFileName)) {
          const request = new AbortController();
          previewRequest = request;
          const response = await fetch(getImageUrl(selectedFileName), {
            signal: request.signal,
          });
          if (!response.ok)
            throw new Error(`ZIP request failed: ${response.status}`);
          const zip = await JSZip.loadAsync(await response.arrayBuffer());
          if (isDisposed || version !== previewVersion) return;
          if (previewRequest === request) previewRequest = null;

          const frames = Object.values(zip.files).filter(
            (file) => !file.dir && /\.png$/i.test(file.name),
          );
          frames.sort((a, b) => a.name.localeCompare(b.name));
          if (frames.length === 0) throw new Error("В архиве нет PNG-кадров.");
          loopAnim.value = frames.length / 25;
          isZip.value = true;
          isLoading.value = true;
          if (await showPreviewImage(await frames[0].async("blob"), version)) {
            void loadNextImage(version, frames);
          }
        } else {
          await showPreviewImage(getImageUrl(selectedFileName), version);
        }
      } catch (error) {
        if (version === previewVersion && !isDisposed) {
          stopPreview();
          isZip.value = false;
          console.error("Error loading preview:", error);
        }
      }
    };

    const selectBroadFromDropdown = async () => {
      // функция для обработки выбора broad из списка
      stopPreview();
      if (selectedBroad.value !== null) {
        await fetchXmlOptions();
        await fetchImages();
      }
    };

    const selectImageByName = async (name: string) => {
      // функция для выбора пункта из списка по названию
      const index = serverImages.value.findIndex((image) => image === name);
      if (index !== -1) {
        selectedImage.value = serverImages.value[index];
        await selectImageFromDropdown();
      } else {
        console.error("Image not found:", name);
        toast.error("Image not found: " + name, {
          position: "top-right",
          pauseOnHover: false,
          autoClose: 3000,
          transition: "flip",
          hideProgressBar: true,
          theme: "dark",
        });
      }
    };

    const selectXmlFromDropdown = async () => {
      // функция для обработки выбора xml из списка
      stopPreview();
      if (selectedBroad.value !== null && selectedXml.value !== null) {
        const broad = broadOptions.value.findIndex(
          (x) => x === selectedBroad.value,
        );
        try {
          await fetchImages();
          const response = await axios.get(myApi + "/api/image/xmlchoose", {
            params: { broad: broad, xml: selectedXml.value },
          });

          await selectImageByName(response.data.fileName);
          loopInput.value = response.data.loop;
          fullHdTop.value = +response.data.y;
          fullHdLeft.value = +response.data.x;
          await nextTick();
          await delay(40);
          updateImagePositionResize();
        } catch {
          showLinkModeError("Такого шаблона не существует.");
        }
      }
    };

    const saveXml = async () => {
      loop.value = parseFloat(loopInput.value);
      const broad = broadOptions.value.findIndex(
        (x) => x === selectedBroad.value,
      );

      if (!isLinkMode.value && (broad === -1 || selectedXml.value === null)) {
        toast.error("Выберите шаблон!", {
          position: "top-right",
          autoClose: 3000,
          transition: "flip",
          hideProgressBar: true,
          pauseOnHover: false,
          theme: "dark",
        });
        return;
      }

      if (isLinkMode.value && !linkXmlPath.value) {
        toast.error("Путь к xml не найден!", {
          position: "top-right",
          autoClose: 3000,
          transition: "flip",
          hideProgressBar: true,
          pauseOnHover: false,
          theme: "dark",
        });
        return;
      }

      const templateTitle = isLinkMode.value
        ? (linkTemplate.value ?? "Link template")
        : xmlItems.value[selectedXml.value!].title;

      const result = await Swal.fire({
        title:
          "<h5 style='color:#dddddd'> Вы уверены, что хотите сохранить шаблон " +
          templateTitle +
          "?",
        icon: "warning",
        iconColor: "#dddddd",
        showCancelButton: true,
        confirmButtonColor: "#4CAF50",
        cancelButtonColor: "#f44336",
        confirmButtonText: "<text style='color:#dddddd'>Да, сохранить!",
        cancelButtonText: "<text style='color:#dddddd'>Отмена",
        background: "#333",
        backdrop: `
        rgba(0,0,0,0.8)
        left top
        no-repeat
      `,
        width: "300px",
        padding: "1em",
      });

      if (!result.isConfirmed) {
        return;
      }

      let loopF = loop.value;
      if (isZip.value) {
        loopF = 0.04;
      }

      try {
        let response;

        if (isLinkMode.value) {
          response = await axios.get(myApi + "/api/image/SaveXmlByPath", {
            params: {
              path: linkXmlPath.value,
              img: selectedImage.value,
              x: fullHdLeft.value,
              y: fullHdTop.value,
              loop: loopF,
            },
          });
        } else {
          response = await axios.get(myApi + "/api/image/savexml", {
            params: {
              broad: broad,
              xml: selectedXml.value,
              img: selectedImage.value,
              x: fullHdLeft.value,
              y: fullHdTop.value,
              loop: loopF,
            },
          });
        }

        if (response.data.res === 0) {
          toast.success("Шаблон был успешно сохранен", {
            dangerouslyHTMLString: true,
            position: "top-right",
            autoClose: 3000,
            transition: "flip",
            hideProgressBar: true,
            pauseOnHover: false,
            theme: "dark",
          });
        } else {
          toast.error("Шаблон не был сохранен", {
            position: "top-right",
            autoClose: 3000,
            transition: "flip",
            hideProgressBar: true,
            pauseOnHover: false,
            theme: "dark",
          });
        }
      } catch (error) {
        console.error("Error saving xml:", error);
        toast.error("Шаблон не был сохранен", {
          position: "top-right",
          autoClose: 3000,
          transition: "flip",
          hideProgressBar: true,
          pauseOnHover: false,
          theme: "dark",
        });
      }
    };

    const loadNextImage = async (
      version: number,
      frames: JSZip.JSZipObject[],
    ) => {
      let index = 1 % frames.length;
      try {
        while (!isDisposed && version === previewVersion && isLoading.value) {
          await delay(40);
          if (isDisposed || version !== previewVersion || !isLoading.value)
            return;
          const blob = await frames[index].async("blob");
          if (!(await showPreviewImage(blob, version))) return;
          index = (index + 1) % frames.length;
        }
      } catch (error) {
        if (version === previewVersion && !isDisposed) {
          stopPreview();
          console.error("Animation preview error:", error);
        }
      }
    };

    const updateImageSize = () => {
      // функция для изменения размера картинки в зависимости от размера поля(с соблюдением пропорций)
      aspectRatio.value = fieldWidth.value / 1920;
      imageWidth.value = originalWidth.value * aspectRatio.value;
      imageHeight.value = originalHeight.value * aspectRatio.value;
      updateImagePositionResize();
    };

    const startDragging = (event: MouseEvent) => {
      // функция для обработки начала перетаскивания картинки
      isDragging.value = true;
      startX.value = event.clientX - left.value;
      startY.value = event.clientY - top.value;
      document.addEventListener("mousemove", onDragging);
      document.addEventListener("mouseup", stopDragging);
    };

    const onDragging = (event: MouseEvent) => {
      // функция для обработки перетаскивания картинки
      if (isDragging.value) {
        let newLeft = event.clientX - startX.value;
        let newTop = event.clientY - startY.value;

        if (newLeft < fieldLeft.value) {
          newLeft = fieldLeft.value;
        } else if (
          newLeft + imageWidth.value >
          fieldLeft.value + fieldWidth.value
        ) {
          newLeft = fieldLeft.value + fieldWidth.value - imageWidth.value;
        }

        if (newTop < fieldTop.value) {
          newTop = fieldTop.value;
        } else if (
          newTop + imageHeight.value >
          fieldTop.value + fieldHeight.value
        ) {
          newTop = fieldTop.value + fieldHeight.value - imageHeight.value;
        }

        left.value = newLeft;
        top.value = newTop;

        fullHdLeft.value = Math.round(
          (left.value - fieldLeft.value) / aspectRatio.value,
        );
        fullHdTop.value = Math.round(
          (top.value - fieldTop.value) / aspectRatio.value,
        );
        if (fullHdLeft.value + originalWidth.value >= 1920) {
          fullHdLeft.value = 1919 - originalWidth.value;
        }
        if (fullHdTop.value + originalHeight.value >= 1080) {
          fullHdTop.value = 1079 - originalHeight.value;
        }
      }
    };

    const stopDragging = () => {
      // функция для обработки прекращения перетаскивания картинки
      isDragging.value = false;
      document.removeEventListener("mousemove", onDragging);
      document.removeEventListener("mouseup", stopDragging);
    };

    const updateImagePositionResize = () => {
      if (originalWidth.value + (fullHdLeft.value ?? 0) >= 1920) {
        fullHdLeft.value = 1919 - originalWidth.value;
      }

      if (originalHeight.value + (fullHdTop.value ?? 0) >= 1080) {
        fullHdTop.value = 1079 - originalHeight.value;
      }

      let newLeft =
        (fullHdLeft.value ?? 0) * aspectRatio.value + fieldLeft.value;
      let newTop = (fullHdTop.value ?? 0) * aspectRatio.value + fieldTop.value;
      if (fullHdLeft.value ?? 0 > 0) {
        newLeft += 1;
      }
      if (fullHdTop.value ?? 0 > 0) {
        newTop += 1;
      }

      left.value = newLeft;
      top.value = newTop;
    };

    const onResize = () => {
      // функция для обработки изменения размера окна
      if (activeTab.value == "fit") {
        tabEvent(0);
      } else {
        rightSize();
      }

      updateImagePositionResize();
    };

    const centerProgressBar = () => {
      // функция для отцентровки визуализации загрузки
      const progressBarContainer = document.querySelector(
        ".progress-bar-container",
      ) as HTMLElement;
      if (progressBarContainer) {
        progressBarContainer.style.left = `${innerWidth / 2 - fieldWidth.value / 4}px`;
        progressBarContainer.style.top = `${innerHeight / 2 - 14}px`;
        progressBarContainer.style.width = `${fieldWidth.value / 2}px`;
      }
    };

    const updateProgressBarWidth = () => {
      const progressBar = document.querySelector(
        ".progress-bar",
      ) as HTMLElement;
      if (progressBar) {
        const widthPercentage = (progress.value / 100) * 100; // Преобразуем значение прогресса в проценты
        progressBar.style.width = `${widthPercentage}%`;
      }
    };

    const updateProgressBarColor = () => {
      // функция для изменения цвета загрузки, в зависимости от процентов.
      const progressBar = document.querySelector(
        ".progress-bar",
      ) as HTMLElement;
      if (progressBar) {
        const brightness = Math.round(150 * (progress.value / 100) + 100);
        progressBar.style.backgroundColor = `rgb(${brightness}, ${brightness}, ${brightness})`;
      }
    };

    const showTooltip = (elementId: keyof typeof tooltips.value) => {
      if (tooltipTimeout) {
        clearTimeout(tooltipTimeout);
      }
      tooltipTimeout = setTimeout(() => {
        tooltips.value[elementId] = true;
      }, 1000); // Задержка в 1 секунду
    };

    const hideTooltip = (elementId: keyof typeof tooltips.value) => {
      if (tooltipTimeout) {
        clearTimeout(tooltipTimeout);
      }
      tooltips.value[elementId] = false;
    };

    onMounted(() => {
      const islinesVisible = localStorage.getItem("linesVisible");
      if (islinesVisible === "true") {
        linesVisible.value = true;
      }
      const isisButtonDarker = localStorage.getItem("isButtonDarker");
      if (isisButtonDarker === "true") {
        isButtonDarker.value = true;
      }
      const sc = localStorage.getItem("scale");
      scalePercent.value = +(sc ?? "50");
      const actTab = localStorage.getItem("actTab");
      activeTab.value = actTab ?? "50";
      loadBackgroundImageFromLocalStorage();
      readLinkModeParams();
      isButton1.value = true;
      isButton2.value = true;
      left.value = fieldLeft.value;
      top.value = fieldTop.value;
      fetchBroadOptions();
      fetchImages();
      onResize();
      document.addEventListener("mouseup", stopDragging);
      window.addEventListener("resize", onResize);
    });

    onUnmounted(() => {
      isDisposed = true;
      isProgress.value = false;
      stopPreview();
      if (tooltipTimeout) clearTimeout(tooltipTimeout);
      document.removeEventListener("mousemove", onDragging);
      document.removeEventListener("mouseup", stopDragging);
      window.removeEventListener("resize", onResize);
    });

    return {
      imageUrl,
      imageFile,
      zipFile,
      onFileChange,
      uploadImage,
      uploadFiles,
      uploadZip,
      left,
      top,
      startDragging,
      imageWidth,
      imageHeight,
      serverImages,
      selectedImage,
      getImageUrl,
      updateImagePositionResize,
      selectImageFromDropdown,
      fullHdLeft,
      fullHdTop,
      isLoading,
      isProgress,
      progress,
      filterInput,
      broadOptions,
      selectedBroad,
      selectedXml,
      isLinkMode,
      selectBroadFromDropdown,
      selectXmlFromDropdown,
      loop,
      tooltips,
      showTooltip,
      hideTooltip,
      saveXml,
      filterLoopInput,
      loopInput,
      fieldWidth,
      fieldHeight,
      linesVisible,
      lines,
      isButtonDarker,
      backgroundImage,
      toggleLinesVisibility,
      uploadBackgroundImage,
      isBackgroundSet,
      startRemoveBackgroundImage,
      isBackgroundRemoving,
      scalePercent,
      isButton1,
      isButton2,
      tabEvent,
      activeTab,
      fieldLeft,
      fieldTop,
      rightSize,
      icon,
      goHome,
      xmlItems,
      isZip,
      loopAnim,
      linkXmlPath,
    };
  },
});
</script>

<style scoped>
.container {
  height: 100%;
  width: 100%;
  margin: 56px 0px;
}

:deep(.controls) {
  height: 56px;
  z-index: 1650;
  position: fixed;
  top: 0px;
  left: 15px; /* Align left side with the left side of .gray-field */
  gap: 15px;
  display: flex;
}

:deep(.panelTop) {
  left: 0px;
  top: 0px;
  height: 56px;
  width: 100%;
  z-index: 1600;
  background-color: #1a1818;
  position: fixed;
  display: flex;
}

:deep(.panelBottom) {
  left: 0px;
  bottom: 0px;
  height: 54px;
  width: 100%;
  z-index: 1600;
  background-color: #1a1818;
  position: fixed;
  display: flex;
}

:deep(.file-input-label),
:deep(.save-button-label) {
  color: #d3cbcb;
  height: 46px;
  background-color: rgb(29, 28, 31);
  transition: background-color 0.3s;
  padding: 10px 20px;
  cursor: pointer;
  font-size: 15px;
  text-transform: none;
  font-weight: 0;
  font-family: "Andale Mono", monospace;
  font-style: normal;
  border-radius: 5px;
  white-space: nowrap;
  border: 1px solid rgb(64, 63, 66);
}
:deep(.save-button-label:hover),
:deep(.file-input-label:hover) {
  background-color: rgb(38, 37, 38);
}

:deep(.file-input) {
  width: 0.1px;
  height: 0.1px;
  opacity: 0;
  position: absolute;
  z-index: 1000;
}

:deep(.select-wrapper),
:deep(.input-wrapper) {
  position: relative;
  display: inline-block;
}

:deep(.image-dropdown) {
  background-color: rgb(29, 28, 31);
  cursor: pointer;
  width: 150px;
  height: 56px;
  z-index: 1000;
}

:deep(.coordinate-input) {
  color: #d3cbcb;
  background-color: rgb(29, 28, 31);
  max-width: 50px; /* Adjust this value as needed */
  box-sizing: border-box;
  padding: 5px; /* Adjust padding as needed */
  border: 1px solid rgb(64, 63, 66);
  border-radius: 4px;
  z-index: 1650;
  font-size: 14px;
  transition: background-color 0.5s ease;
}

:deep(.coordinate-input):hover {
  border: 1px solid rgb(159, 157, 163);
}

.draggable-image {
  max-width: 100%;
  max-height: 100%;
  margin-top: 0px;
  position: absolute;
  cursor: move;
  z-index: 10;
}

.loading-cursor {
  cursor: progress;
}

.default-cursor {
  cursor: default;
}

.gray-field {
  background-color: gray;
  position: relative;
  width: 100%;
  height: 100%;
}

.progress-bar-container {
  position: absolute;
  width: 50%;
  height: 30px;
  background-color: rgb(80, 80, 80);
  border-color: white;
  border-radius: 5px;
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.8);
  z-index: 1800;
}

.progress-bar {
  height: 100%;
  background-color: rgb(255, 255, 255);
  transition:
    width 0.2s,
    background-color 0.2s;
  z-index: 1200;
}

:deep(.inputs) {
  position: fixed;
  left: 250px;
  display: flex;
  align-items: center;
  flex-direction: stroke;
  gap: 5px;
  bottom: -5px;
  transform: translateY(-50%);
  z-index: 1650;
}

:deep(.tooltipcoor),
:deep(.tooltipinp),
:deep(.tooltipbut),
:deep(.tooltip) {
  position: absolute;
  background-color: #333;
  color: #fff;
  padding: 5px;
  border-radius: 3px;
  z-index: 1100;
  font-size: 15px;
  text-transform: none;
  font-weight: 0;
  font-family: "Times New Roman", Times, serif;
  white-space: wrap;
  opacity: 0.9;
  margin-top: 0px;
}

:deep(.tooltipbut) {
  margin-top: 95px;
}

:deep(.tooltipcoor) {
  margin-top: -88px;
}

:deep(.tooltipinp) {
  margin-top: 23px;
}

:deep(.fade-enter-active),
:deep(.fade-leave-active) {
  transition:
    opacity 0.4s ease,
    transform 0.4s ease;
}

:deep(.fade-enter-from),
:deep(.fade-leave-to) {
  transform: translateX(-20%) translateY(-10px);
  opacity: 0;
}

:deep(.buttns) {
  position: fixed;
  top: 5px;
  right: 70px;
  display: flex;
  align-items: center;
  gap: 15px;
  z-index: 1650;
}

.gray-field {
  background-color: #111111;
  position: absolute;
  background-size: cover;
  background-position: center;
  overflow: visible;
}

.gray-field::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image: v-bind(backgroundImage);
  background-size: cover;
  background-position: center;
  opacity: 0;
  transition: opacity 0.8s ease-in-out;
}

.gray-field.fade-in::before {
  opacity: 1;
}

.gray-field.fade-out::before {
  opacity: 0;
}

.rectangle {
  position: absolute;
  background-color: white;
  transition: background-color 0.8s;
  z-index: 1550;
}

.rectangle.fade-out {
  background-color: transparent;
}

.rectangle-enter-active,
.fade-leave-active {
  opacity: 1;
}

.rectangle-enter-active .fade-leave-active {
  opacity: 1;
}

:deep(.button-container) {
  position: fixed;
  bottom: -21px;
  left: 15px;
  display: flex;
  align-items: center;
  flex-direction: stroke;
  transform: translateY(-50%);
  z-index: 1650;
  gap: 5px;
}

:deep(.toggle-button-label),
:deep(.upload-button-label),
:deep(.remove-button-label) {
  cursor: pointer;
  padding: 8px 15px;
  border-radius: 10px;
  white-space: nowrap;
  border: 1px solid #645f5f;
  transition: background-color 0.5s;
  transition: transform 0.5s;
  position: relative;
  display: inline-block;
  margin-bottom: 10px;
  font-size: 16px;
  line-height: 1;
}

:deep(.toggle-button-label:hover),
:deep(.upload-button-label:hover),
:deep(.remove-button-label:hover) {
  transform: scale(1.1); /* Увеличивает вкладку на 10% */
}

:deep(.upload-button-label),
:deep(.remove-button-label) {
  background: #242222;
}

:deep(.toggle-button-label) {
  background: #302e2e;
}

:deep(.toggle-button-label.darker) {
  background-color: #242222;
}

:deep(.remove-button-label.darker),
:deep(.upload-button-label.darker) {
  background-color: #302e2e;
}
:deep(.savebutton),
:deep(.toggle-button),
:deep(.upload-button),
:deep(.remove-button) {
  width: 0.1px;
  height: 0.1px;
  opacity: 0;
  position: absolute;
  z-index: 1000;
}

:deep(.fade-enter-active),
:deep(.fade-leave-active) {
  transition: all 0.5s cubic-bezier(1, 0.5, 0.8, 1);
}

:deep(.fade-enter-from),
:deep(.fade-leave-to) {
  transform: scale(0);
  opacity: 0;
}

:deep(.tab) {
  color: #dbd5d5;
  background-color: #1a1818;
  height: 57px;
}

:deep(.percent) {
  position: fixed;
  height: 53px;
  bottom: -1px;
  right: 0px;
  display: flex;
  align-items: center;
  z-index: 1650;
}

.ico {
  width: 40px;
  height: auto;
  right: 15px;
  top: 6px;
  z-index: 1800;
  position: fixed;
  user-select: none;
  cursor: pointer;
  transition: transform 0.4s ease;
  transform-origin: center; /* Центрирует масштабирование */
}

.ico:hover {
  transform: scale(1.3);
}

:deep(.tab .v-tab) {
  transition: transform 0.3s ease-in-out;
}

:deep(.tab .v-tab):hover {
  transform: scale(1.2); /* Увеличивает вкладку на 10% */
}
</style>
