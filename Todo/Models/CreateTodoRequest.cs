using System.ComponentModel.DataAnnotations;

namespace Todo.Models;

public record CreateTodoRequest(
    [Required, StringLength(CreateTodoRequest.MaxTitleLength)] string Title)
{
    public const int MaxTitleLength = 200;
}
