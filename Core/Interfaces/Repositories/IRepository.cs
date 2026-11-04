namespace Core.Interfaces.Repositories;

public interface IRepository<T, in TK> where T : class
{
    List<T> GetAll();
    T? GetById(TK id);
    T Create(T entity);

    /// <returns>False if no entity has the given id.</returns>
    bool Delete(TK id);
}
