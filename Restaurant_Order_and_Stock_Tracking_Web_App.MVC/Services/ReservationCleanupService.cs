using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _gracePeriod = TimeSpan.FromMinutes(30);

        public ReservationCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReservationCleanupService başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rezervasyon temizleme hatası.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        public async Task CleanupAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

            var cutoff = DateTime.UtcNow.Subtract(_gracePeriod);

            var expired = await db.Tables
                .Where(t => t.TableStatus == 2
                         && t.ReservationTime.HasValue
                         && t.ReservationTime.Value <= cutoff)
                .ToListAsync();

            if (!expired.Any()) return;

            foreach (var table in expired)
            {
                _logger.LogInformation(
                    "Rezervasyon süresi doldu → {TableName} ({ReservationName})",
                    table.TableName, table.ReservationName);

                table.TableStatus = 0;
                table.ReservationName = null;
                table.ReservationPhone = null;
                table.ReservationGuestCount = null;
                table.ReservationTime = null;
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("{Count} rezervasyon otomatik temizlendi.", expired.Count);
        }
    }
}