using Core.Models;

namespace Core.Interfaces.Repositories;

public interface ITodoRepository : IRepository<Todo, Guid>
{
    public void ToggleTodo(Guid id);
}