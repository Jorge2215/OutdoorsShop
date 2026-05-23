using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public virtual async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public virtual Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is not null)
            _dbSet.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
