namespace Todo.Models;

public record SetCompletionRequest
{
    // Required, so that a request without it is rejected instead of meaning false.
    public required bool IsComplete { get; init; }
}
