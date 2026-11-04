import { useState, type FormEvent } from "react";
import { MAX_TITLE_LENGTH, type Todo } from "./api";
import { useCreateTodo, useDeleteTodo, useTodos, useToggleTodo } from "./useTodos";

export function App() {
  return (
    <main className="app">
      <h1>Todo</h1>
      <AddTodoForm />
      <TodoList />
    </main>
  );
}

function AddTodoForm() {
  const [title, setTitle] = useState("");
  const createTodo = useCreateTodo();
  const trimmedTitle = title.trim();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!trimmedTitle || createTodo.isPending) return;
    createTodo.mutate(trimmedTitle, { onSuccess: () => setTitle("") });
  }

  return (
    <form className="add-form" onSubmit={handleSubmit}>
      <div className="add-form-row">
        <input
          aria-label="New todo"
          placeholder="What needs doing?"
          value={title}
          maxLength={MAX_TITLE_LENGTH}
          autoFocus
          onChange={(event) => setTitle(event.target.value)}
        />
        <button type="submit" className="primary" disabled={!trimmedTitle || createTodo.isPending}>
          Add
        </button>
      </div>
      {createTodo.isError && (
        <p className="error" role="alert">
          Could not add the todo. Please try again.
        </p>
      )}
    </form>
  );
}

function TodoList() {
  const todos = useTodos();

  if (todos.isPending) return <p className="status">Loading…</p>;

  // A failed refetch keeps the old data, and that list is better than an error page.
  if (!todos.data) {
    return (
      <div className="status" role="alert">
        <p>Could not load your todos.</p>
        <button onClick={() => todos.refetch()}>Try again</button>
      </div>
    );
  }

  if (todos.data.length === 0) {
    return <p className="status">Nothing to do yet. Add your first todo above.</p>;
  }

  const completed = todos.data.filter((todo) => todo.isComplete).length;

  return (
    <section>
      <p className="summary">
        {completed} of {todos.data.length} done
      </p>
      <ul className="todo-list">
        {todos.data.map((todo) => (
          <TodoRow key={todo.id} todo={todo} />
        ))}
      </ul>
    </section>
  );
}

function TodoRow({ todo }: { todo: Todo }) {
  const toggleTodo = useToggleTodo(todo.id);
  const deleteTodo = useDeleteTodo(todo.id);

  return (
    <li className={deleteTodo.isPending ? "todo removing" : "todo"}>
      <label className="todo-main">
        <input
          type="checkbox"
          checked={todo.isComplete}
          // Ignored rather than disabled, so keyboard users keep their focus here.
          onChange={() => {
            if (toggleTodo.isPending || deleteTodo.isPending) return;
            deleteTodo.reset();
            toggleTodo.mutate();
          }}
        />
        <span className={todo.isComplete ? "todo-title complete" : "todo-title"}>{todo.title}</span>
      </label>
      <button
        className="delete"
        aria-label={`Delete ${todo.title}`}
        disabled={deleteTodo.isPending}
        onClick={() => {
          toggleTodo.reset();
          deleteTodo.mutate();
        }}
      >
        Delete
      </button>
      {(toggleTodo.isError || deleteTodo.isError) && (
        <p className="error" role="alert">
          Could not save that change. Please try again.
        </p>
      )}
    </li>
  );
}
