using System.ComponentModel.DataAnnotations;

namespace Todo.Models;

public record SetCompletionRequest
{
    // Nullable and required, so that a request without it is rejected instead of meaning false.
    [Required]
    public bool? IsComplete { get; init; }
}
