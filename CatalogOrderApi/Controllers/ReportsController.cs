using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Data;
using CatalogOrderApi.Models;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(AppDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/reports/sales?startDate=2026-01-01&endDate=2026-01-31&currency=USD&source=Instagram&customerName=
    [HttpGet("sales")]
    public async Task<ActionResult> GetSalesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? currency = null,
        [FromQuery] OrderSource? source = null,
        [FromQuery] string? customerName = null,
        [FromQuery] string groupBy = "day") // day, week, month, customer, source
    {
        _logger.LogInformation("Generating sales report");

        var query = _context.Orders
            .Include(o => o.Items)
            .AsQueryable();

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(currency))
        {
            query = query.Where(o => o.Currency == currency);
        }

        if (source.HasValue)
        {
            query = query.Where(o => o.Source == source.Value);
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            query = query.Where(o => o.CustomerName.Contains(customerName));
        }

        var orders = await query.ToListAsync();

        // Group by specified dimension
        object result = groupBy.ToLower() switch
        {
            "customer" => orders
                .GroupBy(o => o.CustomerName)
                .Select(g => new
                {
                    customer = g.Key,
                    orderCount = g.Count(),
                    totalAmount = g.SelectMany(o => o.Items).Sum(oi => oi.LineTotal),
                    currency = g.First().Currency
                })
                .OrderByDescending(x => x.totalAmount)
                .ToList(),

            "source" => orders
                .GroupBy(o => o.Source)
                .Select(g => new
                {
                    source = g.Key.ToString(),
                    orderCount = g.Count(),
                    totalAmount = g.SelectMany(o => o.Items).Sum(oi => oi.LineTotal),
                    currency = g.First().Currency
                })
                .OrderByDescending(x => x.totalAmount)
                .ToList(),

            "week" => orders
                .GroupBy(o => new
                {
                    Year = o.CreatedAt.Year,
                    Week = System.Globalization.CultureInfo.InvariantCulture.Calendar
                        .GetWeekOfYear(o.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                })
                .Select(g => new
                {
                    year = g.Key.Year,
                    week = g.Key.Week,
                    orderCount = g.Count(),
                    totalAmount = g.SelectMany(o => o.Items).Sum(oi => oi.LineTotal),
                    currency = g.First().Currency
                })
                .OrderBy(x => x.year).ThenBy(x => x.week)
                .ToList(),

            "month" => orders
                .GroupBy(o => new { Year = o.CreatedAt.Year, Month = o.CreatedAt.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    orderCount = g.Count(),
                    totalAmount = g.SelectMany(o => o.Items).Sum(oi => oi.LineTotal),
                    currency = g.First().Currency
                })
                .OrderBy(x => x.year).ThenBy(x => x.month)
                .ToList(),

            _ => // day (default)
                orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    orderCount = g.Count(),
                    totalAmount = g.SelectMany(o => o.Items).Sum(oi => oi.LineTotal),
                    currency = g.First().Currency
                })
                .OrderBy(x => x.date)
                .ToList()
        };

        var summary = new
        {
            totalOrders = orders.Count,
            totalAmount = orders.SelectMany(o => o.Items).Sum(oi => oi.LineTotal),
            currency = orders.FirstOrDefault()?.Currency ?? "N/A",
            groupedData = result
        };

        return Ok(summary);
    }
}
