namespace Core.Models;

public class Todo
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}