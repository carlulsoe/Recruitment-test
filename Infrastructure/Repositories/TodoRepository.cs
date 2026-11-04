using Core.Interfaces.Repositories;
using Core.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Keeps todos in memory for the lifetime of the process. It is registered as a
/// singleton and so is shared between requests: every access takes the lock, and
/// callers only get copies, so no one can change a stored todo outside the lock.
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly Lock _lock = new();
    private readonly List<Todo> _todos = [];

    public List<Todo> GetAll()
    {
        lock (_lock)
        {
            return _todos.Select(Copy).ToList();
        }
    }

    public Todo? GetById(Guid id)
    {
        lock (_lock)
        {
            var todo = Find(id);
            return todo is null ? null : Copy(todo);
        }
    }

    public Todo Create(Todo entity)
    {
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = entity.Title,
            CreatedAt = DateTime.UtcNow,
        };

        lock (_lock)
        {
            _todos.Add(todo);
            return Copy(todo);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _todos.RemoveAll(todo => todo.Id == id) > 0;
        }
    }

    public Todo? SetComplete(Guid id, bool isComplete)
    {
        lock (_lock)
        {
            var todo = Find(id);
            todo?.SetComplete(isComplete);
            return todo is null ? null : Copy(todo);
        }
    }

    private Todo? Find(Guid id) => _todos.Find(todo => todo.Id == id);

    private static Todo Copy(Todo todo) => new()
    {
        Id = todo.Id,
        Title = todo.Title,
        IsComplete = todo.IsComplete,
        CreatedAt = todo.CreatedAt,
        CompletedAt = todo.CompletedAt,
    };
}
