using ACC_Demo.Data;
using Microsoft.EntityFrameworkCore;

namespace ACC_Demo.Services;

public class AllocationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AllocationBackgroundService> _logger;

    public AllocationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AllocationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllocations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hour allocations");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessAllocations(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var allocations = await db.HourAllocations
            .Where(a => a.IsActive)
            .ToListAsync(stoppingToken);

        foreach (var allocation in allocations)
        {
            if (!IsDue(allocation, now)) continue;

            if (allocation.UserId.HasValue)
            {
                var user = await db.Users.FindAsync(new object[] { allocation.UserId.Value }, stoppingToken);
                if (user != null)
                    user.CurrentBalance += allocation.HoursPerPeriod;
            }
            else if (allocation.OrganizationId.HasValue)
            {
                // Credit the org's creator/rep
                var org = await db.Organizations
                    .Include(o => o.CreatedByUser)
                    .FirstOrDefaultAsync(o => o.OrganizationId == allocation.OrganizationId.Value, stoppingToken);
                if (org?.CreatedByUser != null)
                    org.CreatedByUser.CurrentBalance += allocation.HoursPerPeriod;
            }

            allocation.LastProcessedAt = now;
            _logger.LogInformation("Processed allocation {Id} ({Period}): {Hours}h", allocation.HourAllocationId, allocation.PeriodType, allocation.HoursPerPeriod);
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private static bool IsDue(Models.HourAllocation allocation, DateTime now)
    {
        if (allocation.LastProcessedAt == null) return true;

        return allocation.PeriodType switch
        {
            "Weekly" => (now - allocation.LastProcessedAt.Value).TotalDays >= 7,
            "Monthly" => now >= allocation.LastProcessedAt.Value.AddMonths(1),
            _ => false
        };
    }
}
