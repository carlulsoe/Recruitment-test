import { expect, test as base, type Page } from "@playwright/test";

/** Starts each test from an empty list on the opened page. */
export const test = base.extend<{ todoPage: Page }>({
  todoPage: async ({ page, request }, use) => {
    const todos: { id: string }[] = await (await request.get("/api/todos")).json();
    await Promise.all(todos.map((todo) => request.delete(`/api/todos/${todo.id}`)));
    await page.goto("/");
    await use(page);
  },
});

export async function addTodo(page: Page, title: string) {
  await page.getByLabel("New todo").fill(title);
  await page.getByRole("button", { name: "Add", exact: true }).click();
  await expect(row(page, title)).toBeVisible();
}

export function row(page: Page, title: string) {
  return page.getByRole("listitem").filter({ hasText: title });
}

export function checkbox(page: Page, title: string) {
  return row(page, title).getByRole("checkbox");
}

/** The list reports through aria-busy whether it is loading, including any retries. */
export function list(page: Page) {
  return page.getByRole("list");
}
