using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Functions.Functions;

public class SeasonalDiscountFunction
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SeasonalDiscountFunction> _logger;
    private readonly TimeProvider _timeProvider;

    // CategoryID → name mapping (seeded in AppDbContext)
    private static readonly int[] WinterCategoryIds = [1, 2]; // Camping, Trekking
    private static readonly int[] SummerCategoryIds = [3, 4]; // Cycling, Climbing

    public SeasonalDiscountFunction(AppDbContext dbContext, ILogger<SeasonalDiscountFunction> logger, TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Runs daily at 02:00 UTC. Applies or removes seasonal pricing discounts on active products.
    /// Winter (Dec/Jan/Feb): Camping + Trekking → 15% off.
    /// Summer (Jun/Jul/Aug): Cycling + Climbing → 10% off.
    /// Spring/Autumn: Reset all to no discount.
    /// </summary>
    [Function("SeasonalDiscount")]
    public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        _logger.LogInformation("SeasonalDiscount triggered at {UtcNow}", now);

        var season = GetSeason(now.Month);
        _logger.LogInformation("Current season: {Season} (month {Month})", season, now.Month);

        var products = await _dbContext.Products
            .Include(p => p.Category)
            .ToListAsync();

        int updatedCount = 0;

        foreach (var product in products)
        {
            decimal newMultiplier = season switch
            {
                Season.Winter when WinterCategoryIds.Contains(product.CategoryID) => 0.85m,
                Season.Summer when SummerCategoryIds.Contains(product.CategoryID) => 0.90m,
                _ => 1.0m
            };

            if (product.DiscountMultiplier != newMultiplier)
            {
                product.DiscountMultiplier = newMultiplier;
                updatedCount++;
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation(
            "SeasonalDiscount completed. {UpdatedCount} products updated for {Season}.",
            updatedCount, season);
    }

    private static Season GetSeason(int month) => month switch
    {
        12 or 1 or 2 => Season.Winter,
        3 or 4 or 5  => Season.Spring,
        6 or 7 or 8  => Season.Summer,
        _            => Season.Autumn
    };

    private enum Season { Winter, Spring, Summer, Autumn }
}
