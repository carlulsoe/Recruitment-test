import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createTodo, deleteTodo, getTodos, toggleTodo, type Todo } from "./api";

const todosKey = ["todos"];

export function useTodos() {
  return useQuery({ queryKey: todosKey, queryFn: getTodos });
}

export function useCreateTodo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createTodo,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: todosKey }),
  });
}

/** Optimistic, because a checkbox should respond at once. A failure rolls it back. */
export function useToggleTodo(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => toggleTodo(id),
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: todosKey });
      const previous = queryClient.getQueryData<Todo[]>(todosKey);
      queryClient.setQueryData<Todo[]>(todosKey, (todos) =>
        todos?.map((todo) =>
          todo.id === id ? { ...todo, isComplete: !todo.isComplete } : todo,
        ),
      );
      return { previous };
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(todosKey, context?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: todosKey }),
  });
}

/** Not optimistic: the row stays until the server confirms, so it can show a failure. */
export function useDeleteTodo(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => deleteTodo(id),
    onSettled: () => queryClient.invalidateQueries({ queryKey: todosKey }),
  });
}
