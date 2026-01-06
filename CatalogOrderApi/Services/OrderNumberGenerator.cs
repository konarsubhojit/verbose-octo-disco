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
            // Get the highest order number
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastOrder != null && !string.IsNullOrEmpty(lastOrder.OrderNumber))
            {
                // Extract number from ODxxxx format
                var numberPart = lastOrder.OrderNumber.Substring(2);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Format as ODxxxx (e.g., OD0001, OD0002, etc.)
            return $"OD{nextNumber:D4}";
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
