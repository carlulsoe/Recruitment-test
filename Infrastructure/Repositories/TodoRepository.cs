using Core.Interfaces.Repositories;
using Core.Models;

namespace Infrastructure.Repositories;

public class TodoRepository : ITodoRepository
{
    public List<Todo> GetAll()
    {
        throw new NotImplementedException();
    }

    public Todo GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Todo Create(Todo entity)
    {
        throw new NotImplementedException();
    }

    public Todo Update(Todo entity)
    {
        throw new NotImplementedException();
    }

    public void Delete(Guid id)
    {
        throw new NotImplementedException();
    }

    public void ToggleTodo(Guid id)
    {
        throw new NotImplementedException();
    }
}