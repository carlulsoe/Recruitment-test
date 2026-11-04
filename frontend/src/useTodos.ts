import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { createTodo, deleteTodo, getTodos, toggleTodo, type Todo } from "./api";

const todosKey = ["todos"];

/**
 * Only the last change to finish refetches. An earlier refetch could return before
 * the server has handled a change still in flight, and undo its optimistic update.
 *
 * The refetch is not returned to the mutation, which would stay pending through the
 * refetch's retries and hold back its own error message while the server is down.
 */
function refetchWhenLastChangeSettles(queryClient: QueryClient) {
  if (queryClient.isMutating({ mutationKey: todosKey }) === 1) {
    void queryClient.invalidateQueries({ queryKey: todosKey });
  }
}

export function useTodos() {
  return useQuery({ queryKey: todosKey, queryFn: getTodos });
}

export function useCreateTodo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: todosKey,
    mutationFn: createTodo,
    onSuccess: (created) => {
      queryClient.setQueryData<Todo[]>(todosKey, (todos) => todos && [...todos, created]);
    },
    onSettled: () => refetchWhenLastChangeSettles(queryClient),
  });
}

/** Optimistic, because a checkbox should respond at once. A failure rolls it back. */
export function useToggleTodo(id: string) {
  const queryClient = useQueryClient();

  const flipInCache = () =>
    queryClient.setQueryData<Todo[]>(todosKey, (todos) =>
      todos?.map((todo) => (todo.id === id ? { ...todo, isComplete: !todo.isComplete } : todo)),
    );

  return useMutation({
    mutationKey: todosKey,
    mutationFn: () => toggleTodo(id),
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: todosKey });
      flipInCache();
    },
    // Flips back rather than restoring a snapshot, which would also undo other rows.
    onError: flipInCache,
    onSettled: () => refetchWhenLastChangeSettles(queryClient),
  });
}

/** Not optimistic: the row stays until the server confirms, so it can show a failure. */
export function useDeleteTodo(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: todosKey,
    mutationFn: () => deleteTodo(id),
    onSuccess: () => {
      queryClient.setQueryData<Todo[]>(todosKey, (todos) => todos?.filter((todo) => todo.id !== id));
    },
    onSettled: () => refetchWhenLastChangeSettles(queryClient),
  });
}
