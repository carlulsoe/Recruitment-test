namespace Core.Interfaces.Repositories;

public interface IRepository<T, in TK>
{
    List<T> GetAll();
    T GetById(TK id);
    T Create(T entity);
    T Update(T entity);
    void Delete(TK id);
}