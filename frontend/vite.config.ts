import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  server: {
    // The dev server has its own origin, so API calls are forwarded to ASP.NET Core.
    proxy: { "/api": "http://localhost:5135" },
  },
  build: {
    // ASP.NET Core serves the built app as static files, giving one origin in production.
    outDir: "../Todo/wwwroot",
    emptyOutDir: true,
  },
});
