import {createApp} from "vue";
import FloatingVue from "floating-vue";
import App from "./App.vue";
import "floating-vue/dist/style.css";
import "./styles.css";

createApp(App).use(FloatingVue).mount("#app");
