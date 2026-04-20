using System.Linq.Expressions;

namespace Infrastructure.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(string id);
    Task DeleteAsync(T entity);
    Task<long> CountAsync();
    Task<long> CountAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(string id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
