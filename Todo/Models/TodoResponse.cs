namespace Todo.Models;

public record TodoResponse(
    Guid Id,
    string Title,
    bool IsComplete,
    DateTime CreatedAt,
    DateTime? CompletedAt)
{
    public static TodoResponse From(Core.Models.Todo todo) =>
        new(todo.Id, todo.Title, todo.IsComplete, todo.CreatedAt, todo.CompletedAt);
}
