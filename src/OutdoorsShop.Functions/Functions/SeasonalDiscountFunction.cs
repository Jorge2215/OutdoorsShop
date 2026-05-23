using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Functions.Functions;

public class SeasonalDiscountFunction
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SeasonalDiscountFunction> _logger;

    public SeasonalDiscountFunction(AppDbContext dbContext, ILogger<SeasonalDiscountFunction> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 06:00 UTC. Evaluates products for seasonal discount eligibility.
    /// </summary>
    [Function("SeasonalDiscount")]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("SeasonalDiscount function triggered at {UtcNow}", DateTime.UtcNow);

        var products = await _dbContext.Products.ToListAsync();
        _logger.LogInformation("Evaluating {Count} active products for seasonal discounts.", products.Count);

        // TODO: Implement discount pricing logic based on season/category
        foreach (var product in products)
        {
            _logger.LogDebug("Evaluating product {ProductID} - {Name}", product.ProductID, product.Name);
        }

        _logger.LogInformation("SeasonalDiscount function completed.");
    }
}
