import { expect, type Route } from "@playwright/test";
import { addTodo, checkbox, list, row, test } from "./helpers";

test("shows an empty state until a todo exists", async ({ todoPage: page }) => {
  await expect(page.getByText("Nothing to do yet")).toBeVisible();

  await addTodo(page, "Buy milk");
  await expect(page.getByText("Nothing to do yet")).toBeHidden();

  await row(page, "Buy milk").getByRole("button", { name: /Delete/ }).click();
  await expect(page.getByText("Nothing to do yet")).toBeVisible();
});

test("adds todos with the button and with Enter, in order", async ({ todoPage: page }) => {
  const input = page.getByLabel("New todo");

  await addTodo(page, "First");
  await expect(input).toHaveValue("");

  await input.fill("Second");
  await input.press("Enter");

  await expect(page.getByRole("listitem")).toHaveText([/First/, /Second/]);
});

test("does not allow a blank todo", async ({ todoPage: page }) => {
  await page.getByLabel("New todo").fill("    ");

  await expect(page.getByRole("button", { name: "Add", exact: true })).toBeDisabled();
});

test("marks a todo as done and back, and keeps it after a reload", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  await addTodo(page, "Write README");

  await checkbox(page, "Buy milk").click();
  await expect(page.getByText("1 of 2 done")).toBeVisible();

  await page.reload();
  await expect(checkbox(page, "Buy milk")).toBeChecked();
  await expect(checkbox(page, "Write README")).not.toBeChecked();

  await checkbox(page, "Buy milk").click();
  await expect(page.getByText("0 of 2 done")).toBeVisible();
});

test("deletes only the chosen todo", async ({ todoPage: page }) => {
  await addTodo(page, "Keep");
  await addTodo(page, "Remove");

  await row(page, "Remove").getByRole("button", { name: /Delete/ }).click();

  await expect(row(page, "Remove")).toBeHidden();
  await expect(row(page, "Keep")).toBeVisible();
});

test("shows a title as text, not as HTML", async ({ todoPage: page }) => {
  const title = "<img src=x onerror=alert(1)>";

  await addTodo(page, title);

  await expect(row(page, title)).toBeVisible();
  await expect(page.getByRole("listitem").locator("img")).toHaveCount(0);
});

test("rolls back a change the server rejects", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  await page.route("**/api/todos/*/completion", (route) => route.fulfill({ status: 500 }));

  await checkbox(page, "Buy milk").click();

  await expect(row(page, "Buy milk").getByRole("alert")).toBeVisible();
  await expect(checkbox(page, "Buy milk")).not.toBeChecked();
});

test("keeps a row when deleting it fails", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  await page.route("**/api/todos/*", (route) =>
    route.request().method() === "DELETE" ? route.fulfill({ status: 500 }) : route.continue(),
  );

  await row(page, "Buy milk").getByRole("button", { name: /Delete/ }).click();

  await expect(row(page, "Buy milk").getByRole("alert")).toBeVisible();
});

test("keeps the typed text when adding fails", async ({ todoPage: page }) => {
  await page.route("**/api/todos", (route) =>
    route.request().method() === "POST" ? route.fulfill({ status: 500 }) : route.continue(),
  );
  const input = page.getByLabel("New todo");

  await input.fill("Buy milk");
  await input.press("Enter");

  await expect(page.getByRole("alert")).toContainText("Buy milk");
  await expect(input).toHaveValue("Buy milk");
});

test("offers a retry when loading fails", async ({ todoPage: page }) => {
  await page.route("**/api/todos", (route) => route.abort());
  await page.reload();
  await expect(page.getByText("Could not load your todos.")).toBeVisible({ timeout: 15_000 });

  await page.unroute("**/api/todos");
  await page.getByRole("button", { name: "Try again" }).click();

  await expect(page.getByText("Nothing to do yet")).toBeVisible();
});

test("keeps the list and shows the failure when the server goes down", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  await page.route("**/api/todos/**", (route) => route.abort());
  await page.route("**/api/todos", (route) => route.abort());

  await checkbox(page, "Buy milk").click();

  // The failure must not wait for the refetch, which is still retrying at this point.
  await expect(row(page, "Buy milk").getByRole("alert")).toBeVisible();
  await expect(list(page)).toHaveAttribute("aria-busy", "true");
  await expect(checkbox(page, "Buy milk")).not.toBeChecked();

  // When the refetch has given up, the list must still be there.
  await expect(list(page)).toHaveAttribute("aria-busy", "false", { timeout: 15_000 });
  await expect(row(page, "Buy milk")).toBeVisible();
});

test("keeps what the user types while the previous todo is being saved", async ({ todoPage: page }) => {
  let held: Route | undefined;
  await page.route("**/api/todos", (route) =>
    route.request().method() === "POST" ? void (held = route) : route.continue(),
  );
  const input = page.getByLabel("New todo");

  await input.fill("First");
  await input.press("Enter");
  await expect(input).toHaveValue("");
  await input.pressSequentially("Second");
  await expect.poll(() => held).toBeDefined();
  await held!.continue();

  await expect(row(page, "First")).toBeVisible();
  await expect(input).toHaveValue("Second");
});

test("a failed todo can be tried again after starting on the next one", async ({ todoPage: page }) => {
  let held: Route | undefined;
  await page.route("**/api/todos", (route) =>
    route.request().method() === "POST" ? void (held = route) : route.continue(),
  );
  const input = page.getByLabel("New todo");

  await input.fill("First");
  await input.press("Enter");
  await input.pressSequentially("Second");
  await expect.poll(() => held).toBeDefined();
  await held!.fulfill({ status: 500 });

  await expect(page.getByRole("alert")).toContainText("First");
  await expect(input).toHaveValue("Second");

  await page.unroute("**/api/todos");
  await page.getByRole("button", { name: "Try again" }).click();

  await expect(row(page, "First")).toBeVisible();
  await expect(page.getByRole("alert")).toBeHidden();
  await expect(input).toHaveValue("Second");
  await expect(input).toBeFocused();
});

test("the input is ready for the next todo after adding one with the button", async ({ todoPage: page }) => {
  await addTodo(page, "First");

  await page.keyboard.type("Second");

  await expect(page.getByLabel("New todo")).toHaveValue("Second");
});

test("reports failures at once while offline", async ({ todoPage: page, context }) => {
  await addTodo(page, "Existing");
  await context.setOffline(true);
  const input = page.getByLabel("New todo");

  await input.fill("Buy milk");
  await input.press("Enter");
  await expect(page.getByRole("alert")).toContainText("Buy milk");
  await expect(input).toHaveValue("Buy milk");

  await checkbox(page, "Existing").click();
  await expect(row(page, "Existing").getByRole("alert")).toBeVisible();
  await expect(checkbox(page, "Existing")).not.toBeChecked();

  await row(page, "Existing").getByRole("button", { name: /Delete/ }).click();
  await expect(row(page, "Existing").getByRole("alert")).toBeVisible();

  await context.setOffline(false);
  await input.press("Enter");
  await expect(row(page, "Buy milk")).toBeVisible();
});

test("serves the built files compressed and with suitable caching", async ({ request }) => {
  const index = await request.get("/");
  expect(index.headers()["cache-control"]).toBe("no-cache");

  const script = (await index.text()).match(/assets\/[^"]+\.js/)![0];
  const asset = await request.get(`/${script}`);

  expect(asset.headers()["cache-control"]).toContain("immutable");
  expect(asset.headers()["content-encoding"]).toMatch(/br|gzip/);
});
