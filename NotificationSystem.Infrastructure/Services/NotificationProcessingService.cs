// Create a new file: NotificationSystem.API/Services/NotificationProcessingService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationSystem.Domain.Repositories;
using NotificationSystem.Infrastructure.Services;

namespace NotificationSystem.API.Services
{
    public class NotificationProcessingService : BackgroundService
    {
        private readonly ILogger<NotificationProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public NotificationProcessingService(
            ILogger<NotificationProcessingService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Processing Service is running.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessPendingNotificationsAsync();
            }
        }

        private async Task ProcessPendingNotificationsAsync()
        {
            _logger.LogInformation("Checking for pending notifications...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                var pendingNotifications = await repository.GetAllPendingAsync();
                var count = pendingNotifications.Count();

                _logger.LogInformation($"Found {count} pending notifications to process");

                foreach (var notification in pendingNotifications)
                {
                    await notificationService.ForceNotificationSendAsync(notification.Id);
                    _logger.LogInformation($"Forced processing of notification {notification.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending notifications");
            }
        }
    }
}