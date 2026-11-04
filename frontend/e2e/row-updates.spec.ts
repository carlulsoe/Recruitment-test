import { expect, type Page, type Route } from "@playwright/test";
import { addTodo, checkbox, row, test } from "./helpers";

/** Holds the toggle request of one todo until the test releases it. */
async function holdToggle(page: Page, title: string) {
  const todos: { id: string; title: string }[] = await (await page.request.get("/api/todos")).json();
  const id = todos.find((todo) => todo.title === title)!.id;
  let held: Route | undefined;
  const isHeld = new Promise<void>((resolve) => {
    void page.route(`**/api/todos/${id}/*`, (route) => {
      held = route;
      resolve();
    });
  });
  return { isHeld, route: () => held! };
}

test("a finished toggle does not undo another row's pending toggle", async ({ todoPage: page }) => {
  await addTodo(page, "Slow");
  await addTodo(page, "Fast");
  const slow = await holdToggle(page, "Slow");

  await checkbox(page, "Slow").click();
  await slow.isHeld;
  const fastDone = page.waitForResponse("**/api/todos/*/*");
  await checkbox(page, "Fast").click();
  await fastDone;

  // Long enough for a refetch to return and overwrite the pending row.
  await page.waitForTimeout(500);
  await expect(checkbox(page, "Slow")).toBeChecked();

  await slow.route().continue();
  await expect(page.getByText("2 of 2 done")).toBeVisible();
});

test("a failed toggle rolls back only its own row", async ({ todoPage: page }) => {
  await addTodo(page, "Failing");
  await addTodo(page, "Pending");
  const failing = await holdToggle(page, "Failing");
  const pending = await holdToggle(page, "Pending");

  await checkbox(page, "Failing").click();
  await failing.isHeld;
  await checkbox(page, "Pending").click();
  await pending.isHeld;
  await failing.route().fulfill({ status: 500 });

  await expect(checkbox(page, "Failing")).not.toBeChecked();
  await expect(checkbox(page, "Pending")).toBeChecked();

  await pending.route().continue();
  await expect(page.getByText("1 of 2 done")).toBeVisible();
});

test("a row's old error goes away when another action on it starts", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  const milk = row(page, "Buy milk");
  await page.route("**/api/todos/*", (route) =>
    route.request().method() === "DELETE" ? route.fulfill({ status: 500 }) : route.continue(),
  );

  await milk.getByRole("button", { name: /Delete/ }).click();
  await expect(milk.getByRole("alert")).toBeVisible();

  await checkbox(page, "Buy milk").click();

  await expect(page.getByText("1 of 1 done")).toBeVisible();
  await expect(milk.getByRole("alert")).toBeHidden();
});

/** What the browser reports when the user returns to the tab. TanStack Query refetches on it. */
async function returnToTab(page: Page) {
  await page.evaluate(() => window.dispatchEvent(new Event("visibilitychange")));
}

test("a failed toggle agrees with the server even after a refetch", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  const held = await holdToggle(page, "Buy milk");

  await checkbox(page, "Buy milk").click();
  await held.isHeld;
  await returnToTab(page);
  await page.waitForTimeout(500);
  // With the list unavailable too, no later refetch can hide a wrong rollback.
  await page.route("**/api/todos", (route) => route.abort());
  await held.route().fulfill({ status: 500 });

  await expect(row(page, "Buy milk").getByRole("alert")).toBeVisible();
  await expect(checkbox(page, "Buy milk")).not.toBeChecked();
  await expect(page.getByText("0 of 1 done")).toBeVisible();
});

test("marking a todo done in a stale tab keeps it done", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  const staleTab = await page.context().newPage();
  await staleTab.goto("/");
  await expect(checkbox(staleTab, "Buy milk")).not.toBeChecked();

  await checkbox(page, "Buy milk").click();
  await expect(page.getByText("1 of 1 done")).toBeVisible();

  const done = staleTab.waitForResponse("**/api/todos/*/*");
  await checkbox(staleTab, "Buy milk").click();
  await done;

  await expect(staleTab.getByText("1 of 1 done")).toBeVisible();
  await expect(checkbox(staleTab, "Buy milk")).toBeChecked();
});
