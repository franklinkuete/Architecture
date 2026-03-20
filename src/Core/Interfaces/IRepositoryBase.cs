using Microsoft.EntityFrameworkCore;

namespace Core.Interfaces;

// Interface générique de base pour les opérations CRUD (Create, Read, Update, Delete) sur une entité de type T.
public interface IRepositoryBase<T> where T : class
{
    IQueryable<T> GetAll();
    Task<T?> GetByIdAsync(Object id);
    Task<T> AddAsync(T entity);
    Task<T?> UpdateAsync(Object id, T entity);
    Task<bool> DeleteAsync(Object id);
    DbSet<T> DataContext {  get; }
}
