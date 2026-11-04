export interface Todo {
  id: string;
  title: string;
  isComplete: boolean;
  createdAt: string;
  completedAt: string | null;
}

/** Must match CreateTodoRequest.MaxTitleLength, which the API enforces. */
export const MAX_TITLE_LENGTH = 200;

async function request(path: string, init?: RequestInit): Promise<Response> {
  const response = await fetch(`/api/todos${path}`, init);
  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
  return response;
}

export async function getTodos(): Promise<Todo[]> {
  const response = await request("");
  return response.json();
}

export async function createTodo(title: string): Promise<Todo> {
  const response = await request("", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title }),
  });
  return response.json();
}

export async function setTodoComplete(id: string, isComplete: boolean): Promise<Todo> {
  const response = await request(`/${id}/completion`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ isComplete }),
  });
  return response.json();
}

export async function deleteTodo(id: string): Promise<void> {
  await request(`/${id}`, { method: "DELETE" });
}
