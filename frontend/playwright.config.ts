import { defineConfig } from "@playwright/test";

// Set E2E_BASE_URL to test something already running, such as the container.
const externalUrl = process.env.E2E_BASE_URL;
const localUrl = "http://localhost:5136";

export default defineConfig({
  testDir: "./e2e",
  // All tests share the server's in-memory data, so they cannot run side by side.
  workers: 1,
  use: { baseURL: externalUrl ?? localUrl },
  webServer: externalUrl
    ? undefined
    : {
        command: `npm run build && dotnet run --project ../Todo --no-launch-profile --urls ${localUrl}`,
        url: localUrl,
        timeout: 120_000,
      },
});
