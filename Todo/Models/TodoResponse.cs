using TodoItem = Core.Models.Todo;

namespace Todo.Models;

public record TodoResponse(
    Guid Id,
    string Title,
    bool IsComplete,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    public static TodoResponse From(TodoItem todo) =>
        new(todo.Id, todo.Title, todo.IsComplete, todo.CreatedAt, todo.CompletedAt);
}
