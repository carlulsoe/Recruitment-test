import { expect, type Page, type Route } from "@playwright/test";
import { addTodo, checkbox, list, row, test } from "./helpers";

/** Holds the completion request of one todo until the test releases it. */
async function holdCompletion(page: Page, title: string) {
  const todos: { id: string; title: string }[] = await (await page.request.get("/api/todos")).json();
  const id = todos.find((todo) => todo.title === title)!.id;
  let held: Route | undefined;
  let markHeld!: () => void;
  const isHeld = new Promise<void>((resolve) => (markHeld = resolve));
  await page.route(`**/api/todos/${id}/completion`, (route) => {
    held = route;
    markHeld();
  });
  return { isHeld, route: () => held! };
}

/** Counts the list requests the page sends from now on. */
async function countListRequests(page: Page) {
  const counter = { count: 0 };
  await page.route("**/api/todos", (route) => {
    if (route.request().method() === "GET") counter.count++;
    return route.fallback();
  });
  return counter;
}

/** Lets the page finish reacting to what it has received. Takes no fixed amount of time. */
async function settle(page: Page) {
  await page.evaluate(() => new Promise((resolve) => setTimeout(resolve)));
}

test("a finished change does not refetch over another row's pending change", async ({ todoPage: page }) => {
  await addTodo(page, "Slow");
  await addTodo(page, "Fast");
  const slow = await holdCompletion(page, "Slow");
  const listRequests = await countListRequests(page);

  await checkbox(page, "Slow").click();
  await slow.isHeld;
  const fastDone = page.waitForResponse("**/api/todos/*/completion");
  await checkbox(page, "Fast").click();
  await (await fastDone).finished();
  await settle(page);

  expect(listRequests.count).toBe(0);
  await expect(checkbox(page, "Slow")).toBeChecked();

  await slow.route().continue();
  await expect.poll(() => listRequests.count).toBe(1);
  await expect(page.getByText("2 of 2 done")).toBeVisible();
});

test("a failed change rolls back only its own row", async ({ todoPage: page }) => {
  await addTodo(page, "Failing");
  await addTodo(page, "Pending");
  const failing = await holdCompletion(page, "Failing");
  const pending = await holdCompletion(page, "Pending");

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

test("returning to the tab refetches, but not during a pending change", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  const held = await holdCompletion(page, "Buy milk");
  const listRequests = await countListRequests(page);

  await checkbox(page, "Buy milk").click();
  await held.isHeld;
  await returnToTab(page);
  await settle(page);

  expect(listRequests.count).toBe(0);
  await expect(checkbox(page, "Buy milk")).toBeChecked();

  await held.route().continue();
  await expect.poll(() => listRequests.count).toBe(1);
  await expect(list(page)).toHaveAttribute("aria-busy", "false");
  await expect(page.getByText("1 of 1 done")).toBeVisible();

  await returnToTab(page);
  await expect.poll(() => listRequests.count).toBe(2);
});

test("a failed change rolls back when the list cannot be reloaded either", async ({ todoPage: page }) => {
  await addTodo(page, "Buy milk");
  const held = await holdCompletion(page, "Buy milk");

  await checkbox(page, "Buy milk").click();
  await held.isHeld;
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

  const done = staleTab.waitForResponse("**/api/todos/*/completion");
  await checkbox(staleTab, "Buy milk").click();
  await done;

  await expect(staleTab.getByText("1 of 1 done")).toBeVisible();
  await expect(checkbox(staleTab, "Buy milk")).toBeChecked();
});
