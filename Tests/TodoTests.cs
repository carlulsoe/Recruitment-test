// The web project's namespace is also called Todo, and it wins over the class.
using TodoItem = Core.Models.Todo;

namespace Tests;

public class TodoTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_assigns_id_and_time_and_starts_incomplete()
    {
        var todo = TodoItem.Create("Write tests", Now);

        Assert.NotEqual(Guid.Empty, todo.Id);
        Assert.Equal(Now, todo.CreatedAt);
        Assert.False(todo.IsComplete);
        Assert.Null(todo.CompletedAt);
    }

    [Fact]
    public void Create_trims_the_title()
    {
        Assert.Equal("Buy milk", TodoItem.Create("  Buy milk  ", Now).Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("​")]
    [InlineData(" ​﻿\t")]
    public void Create_rejects_a_title_without_visible_text(string title)
    {
        Assert.Throws<ArgumentException>(() => TodoItem.Create(title, Now));
    }

    [Fact]
    public void Create_measures_the_title_after_trimming()
    {
        var atTheLimit = new string('x', TodoItem.MaxTitleLength);

        Assert.Equal(atTheLimit, TodoItem.Create(atTheLimit + "   ", Now).Title);
        Assert.Throws<ArgumentException>(() => TodoItem.Create(atTheLimit + "x", Now));
    }

    [Fact]
    public void WithCompletion_sets_and_clears_the_completion_time()
    {
        var todo = TodoItem.Create("Finish me", Now);
        var later = Now.AddHours(1);

        var completed = todo.WithCompletion(true, later);
        Assert.True(completed.IsComplete);
        Assert.Equal(later, completed.CompletedAt);

        var reopened = completed.WithCompletion(false, later.AddHours(1));
        Assert.False(reopened.IsComplete);
        Assert.Null(reopened.CompletedAt);
    }

    [Fact]
    public void WithCompletion_keeps_the_first_completion_time_when_repeated()
    {
        var completed = TodoItem.Create("Finish me", Now).WithCompletion(true, Now.AddHours(1));

        Assert.Equal(Now.AddHours(1), completed.WithCompletion(true, Now.AddHours(2)).CompletedAt);
    }

    [Fact]
    public void WithCompletion_leaves_the_original_unchanged()
    {
        var todo = TodoItem.Create("Finish me", Now);

        todo.WithCompletion(true, Now);

        Assert.False(todo.IsComplete);
    }
}
