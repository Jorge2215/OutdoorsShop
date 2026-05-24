using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class CategoryRepository : Repository<ProductCategory>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }
}
