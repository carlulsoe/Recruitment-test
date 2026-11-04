namespace Todo.Models;

public record CreateTodoRequest
{
    [TodoTitle]
    public string Title { get; init; } = "";
}
