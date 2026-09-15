import { createApp } from "vue";
import "./styles.css";
import "vuetify/styles";
import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import App from "./App.vue";
import { loadRuntimeConfig } from "./runtime-config";
// Определение флагов компиляции
declare global {
  interface Window {
    __VUE_OPTIONS_API__: boolean;
    __VUE_PROD_DEVTOOLS__: boolean;
    __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: boolean;
  }
}

window.__VUE_OPTIONS_API__ = true;
window.__VUE_PROD_DEVTOOLS__ = false;
window.__VUE_PROD_HYDRATION_MISMATCH_DETAILS__ = false;

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: "dark",
  },
});

loadRuntimeConfig()
  .then(() => createApp(App).use(vuetify).mount("#app"))
  .catch((error: unknown) => {
    console.error("Failed to load frontend configuration:", error);
    const root = document.getElementById("app");
    if (root) {
      root.textContent =
        "Не удалось загрузить настройки подключения. Проверьте config.json и обновите страницу.";
    }
  });
