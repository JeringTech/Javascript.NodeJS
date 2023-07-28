import { defineConfig } from "vite";

export default defineConfig({
    build: {
        emptyOutDir: false,
        ssr: true,
    },
});
