import {createApp} from "vue";
import FloatingVue from "floating-vue";
import App from "./App.vue";
import {i18n} from "./i18n";
import "floating-vue/dist/style.css";
import "./styles.css";

createApp(App).use(i18n).use(FloatingVue).mount("#app");
