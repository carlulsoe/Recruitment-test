using Infrastructure.Repositories;
// The web project's namespace is also called Todo, and it wins over the class.
using TodoItem = Core.Models.Todo;

namespace Tests;

public class TodoRepositoryTests
{
    private readonly TodoRepository _repository = new();

    [Fact]
    public void Create_assigns_id_and_timestamp_and_starts_incomplete()
    {
        var created = _repository.Create(new TodoItem { Title = "Write tests" });

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.False(created.IsComplete);
        Assert.Null(created.CompletedAt);
    }

    [Fact]
    public void GetAll_returns_todos_in_creation_order()
    {
        _repository.Create(new TodoItem { Title = "First" });
        _repository.Create(new TodoItem { Title = "Second" });

        Assert.Equal(["First", "Second"], _repository.GetAll().Select(todo => todo.Title));
    }

    [Fact]
    public void SetComplete_sets_and_clears_completion()
    {
        var id = _repository.Create(new TodoItem { Title = "Finish me" }).Id;

        var completed = _repository.SetComplete(id, true);
        Assert.True(completed!.IsComplete);
        Assert.NotNull(completed.CompletedAt);

        var reopened = _repository.SetComplete(id, false);
        Assert.False(reopened!.IsComplete);
        Assert.Null(reopened.CompletedAt);
    }

    [Fact]
    public void SetComplete_twice_keeps_the_first_completion_time()
    {
        var id = _repository.Create(new TodoItem { Title = "Finish me" }).Id;

        var first = _repository.SetComplete(id, true)!.CompletedAt;
        Thread.Sleep(5);
        var second = _repository.SetComplete(id, true)!.CompletedAt;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Delete_removes_only_the_given_todo()
    {
        var removed = _repository.Create(new TodoItem { Title = "Remove" }).Id;
        var kept = _repository.Create(new TodoItem { Title = "Keep" }).Id;

        Assert.True(_repository.Delete(removed));

        Assert.Null(_repository.GetById(removed));
        Assert.NotNull(_repository.GetById(kept));
    }

    [Fact]
    public void Missing_id_is_reported_rather_than_thrown()
    {
        var missing = Guid.NewGuid();

        Assert.Null(_repository.GetById(missing));
        Assert.Null(_repository.SetComplete(missing, true));
        Assert.False(_repository.Delete(missing));
    }

    [Fact]
    public void Changing_a_returned_todo_does_not_change_the_stored_one()
    {
        var id = _repository.Create(new TodoItem { Title = "Original" }).Id;

        _repository.GetById(id)!.Title = "Changed";
        _repository.GetAll()[0].Title = "Changed";

        Assert.Equal("Original", _repository.GetById(id)!.Title);
    }

    [Fact]
    public void Concurrent_creates_all_survive()
    {
        Parallel.For(0, 1000, i => _repository.Create(new TodoItem { Title = $"Todo {i}" }));

        Assert.Equal(1000, _repository.GetAll().Count);
    }
}
