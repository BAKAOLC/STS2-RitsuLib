import {defineConfig} from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
    plugins: [vue()],
    base: "./",
    server: {
        proxy: {
            "/api": {
                target: "http://127.0.0.1:18742",
                changeOrigin: true,
                ws: false
            }
        }
    },
    build: {
        outDir: "dist",
        emptyOutDir: true,
        rolldownOptions: {
            output: {
                entryFileNames: "assets/[name].js",
                chunkFileNames: "assets/[name].js",
                assetFileNames: "assets/[name][extname]"
            }
        }
    }
});
