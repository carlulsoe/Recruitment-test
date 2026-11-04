using Infrastructure.Repositories;
// The web project's namespace is also called Todo, and it wins over the class.
using TodoItem = Core.Models.Todo;

namespace Tests;

public class TodoRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly TodoRepository _repository = new();

    private Guid Add(string title) => _repository.Create(TodoItem.Create(title, Now)).Id;

    [Fact]
    public void Create_stores_the_todo_it_is_given()
    {
        var todo = TodoItem.Create("Write tests", Now);

        Assert.Equal(todo, _repository.Create(todo));
        Assert.Equal(todo, _repository.GetById(todo.Id));
    }

    [Fact]
    public void GetAll_returns_todos_in_creation_order()
    {
        Add("First");
        Add("Second");

        Assert.Equal(["First", "Second"], _repository.GetAll().Select(todo => todo.Title));
    }

    [Fact]
    public void SetComplete_stores_the_new_state()
    {
        var id = Add("Finish me");

        var completed = _repository.SetComplete(id, true, Now.AddHours(1));

        Assert.Equal(Now.AddHours(1), completed!.CompletedAt);
        Assert.Equal(completed, _repository.GetById(id));

        _repository.SetComplete(id, false, Now.AddHours(2));

        Assert.False(_repository.GetById(id)!.IsComplete);
    }

    [Fact]
    public void SetComplete_changes_only_the_given_todo()
    {
        var kept = Add("Keep");
        var changed = Add("Change");

        _repository.SetComplete(changed, true, Now);

        Assert.False(_repository.GetById(kept)!.IsComplete);
    }

    [Fact]
    public void Delete_removes_only_the_given_todo()
    {
        var removed = Add("Remove");
        var kept = Add("Keep");

        Assert.True(_repository.Delete(removed));

        Assert.Null(_repository.GetById(removed));
        Assert.NotNull(_repository.GetById(kept));
    }

    [Fact]
    public void Missing_id_is_reported_rather_than_thrown()
    {
        var missing = Guid.NewGuid();

        Assert.Null(_repository.GetById(missing));
        Assert.Null(_repository.SetComplete(missing, true, Now));
        Assert.False(_repository.Delete(missing));
    }

    [Fact]
    public void Changing_a_returned_list_does_not_change_the_stored_one()
    {
        Add("Original");

        _repository.GetAll().Clear();

        Assert.Single(_repository.GetAll());
    }

    [Fact]
    public void Concurrent_creates_all_survive()
    {
        Parallel.For(0, 1000, i => Add($"Todo {i}"));

        Assert.Equal(1000, _repository.GetAll().Count);
    }

    [Fact]
    public void Concurrent_changes_and_deletes_do_not_interfere()
    {
        var toDelete = Enumerable.Range(0, 500).Select(i => Add($"Delete {i}")).ToList();
        var toComplete = Enumerable.Range(0, 500).Select(i => Add($"Complete {i}")).ToList();

        Parallel.For(0, 500, i =>
        {
            _repository.Delete(toDelete[i]);
            _repository.SetComplete(toComplete[i], true, Now);
        });

        var remaining = _repository.GetAll();
        Assert.Equal(toComplete, remaining.Select(todo => todo.Id));
        Assert.All(remaining, todo => Assert.True(todo.IsComplete));
    }

    [Fact]
    public void Concurrent_completions_of_one_todo_keep_a_single_completion_time()
    {
        var id = Add("Finish me");

        var results = new TodoItem?[100];
        Parallel.For(0, results.Length, i => results[i] = _repository.SetComplete(id, true, Now.AddSeconds(i)));

        Assert.Single(results.Select(todo => todo!.CompletedAt).Distinct());
    }
}
