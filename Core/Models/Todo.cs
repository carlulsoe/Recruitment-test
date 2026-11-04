namespace Core.Models;

public class Todo
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Repeating a state changes nothing, so CompletedAt keeps its original time.</summary>
    public void SetComplete(bool isComplete)
    {
        if (IsComplete == isComplete) return;

        IsComplete = isComplete;
        CompletedAt = isComplete ? DateTime.UtcNow : null;
    }
}
