using Core.Interfaces.Repositories;
using Core.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Keeps todos in memory for the lifetime of the process. It is registered as a
/// singleton and so is shared between requests: the lock protects the list, and
/// the todos themselves are immutable, so they can safely be handed out.
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly Lock _lock = new();
    private readonly List<Todo> _todos = [];

    public List<Todo> GetAll()
    {
        lock (_lock)
        {
            return [.. _todos];
        }
    }

    public Todo? GetById(Guid id)
    {
        lock (_lock)
        {
            return _todos.Find(todo => todo.Id == id);
        }
    }

    public Todo Create(Todo entity)
    {
        lock (_lock)
        {
            _todos.Add(entity);
            return entity;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _todos.RemoveAll(todo => todo.Id == id) > 0;
        }
    }

    public Todo? SetComplete(Guid id, bool isComplete, DateTimeOffset now)
    {
        lock (_lock)
        {
            var index = _todos.FindIndex(todo => todo.Id == id);
            if (index < 0)
            {
                return null;
            }

            return _todos[index] = _todos[index].WithCompletion(isComplete, now);
        }
    }
}
