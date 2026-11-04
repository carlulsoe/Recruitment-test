using System.Globalization;

namespace Core.Models;

/// <summary>
/// Immutable, so its rules cannot be bypassed and it is safe to share between threads.
/// Time is passed in, which keeps the rules deterministic.
/// </summary>
public sealed record Todo
{
    public const int MaxTitleLength = 200;

    public Guid Id { get; private init; }
    public string Title { get; private init; }
    public bool IsComplete { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? CompletedAt { get; private init; }

    private Todo(string title, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Title = title;
        CreatedAt = createdAt;
    }

    /// <exception cref="ArgumentException">The title is not allowed, see <see cref="FindTitleProblem"/>.</exception>
    public static Todo Create(string title, DateTimeOffset now)
    {
        if (FindTitleProblem(title) is { } problem)
        {
            throw new ArgumentException(problem, nameof(title));
        }

        return new Todo(title.Trim(), now);
    }

    /// <returns>Why the title cannot be used, or null if it can. A title is measured after trimming.</returns>
    public static string? FindTitleProblem(string title)
    {
        var trimmed = title.Trim();

        if (trimmed.All(IsInvisible))
        {
            return "The title must contain visible text.";
        }

        return trimmed.Length > MaxTitleLength
            ? $"The title must be at most {MaxTitleLength} characters."
            : null;
    }

    /// <summary>Repeating a state changes nothing, so CompletedAt keeps its original time.</summary>
    public Todo WithCompletion(bool isComplete, DateTimeOffset now) =>
        IsComplete == isComplete
            ? this
            : this with { IsComplete = isComplete, CompletedAt = isComplete ? now : null };

    private static bool IsInvisible(char character) =>
        char.IsWhiteSpace(character)
        || char.IsControl(character)
        || char.GetUnicodeCategory(character) == UnicodeCategory.Format;
}
