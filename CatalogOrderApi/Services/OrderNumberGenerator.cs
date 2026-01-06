using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Data;

namespace CatalogOrderApi.Services;

public interface IOrderNumberGenerator
{
    Task<string> GenerateOrderNumberAsync();
}

public class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly AppDbContext _context;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public OrderNumberGenerator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var today = DateTime.UtcNow;
            var datePrefix = today.ToString("yyyyMMdd");
            
            // Get the highest order number for today
            var todayPrefix = $"ORD-{datePrefix}-";
            var lastOrderToday = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith(todayPrefix))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastOrderToday != null && !string.IsNullOrEmpty(lastOrderToday.OrderNumber))
            {
                // Extract number from ORD-YYYYMMDD-XXXX format
                var parts = lastOrderToday.OrderNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Format as ORD-YYYYMMDD-XXXX (e.g., ORD-20260106-0001)
            return $"ORD-{datePrefix}-{nextNumber:D4}";
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
