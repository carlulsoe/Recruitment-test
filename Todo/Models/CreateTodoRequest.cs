using System.ComponentModel.DataAnnotations;

namespace Todo.Models;

public record CreateTodoRequest
{
    public const int MaxTitleLength = 200;

    // Trimmed when set, so validation sees the same title that gets stored.
    [Required, StringLength(MaxTitleLength)]
    public string Title { get; init => field = value?.Trim() ?? ""; } = "";
}
