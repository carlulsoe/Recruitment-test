using Core.Models;

namespace Core.Interfaces.Repositories;

public interface ITodoRepository : IRepository<Todo, Guid>
{
    /// <returns>The updated todo, or null if no todo has the given id.</returns>
    Todo? ToggleTodo(Guid id);
}
