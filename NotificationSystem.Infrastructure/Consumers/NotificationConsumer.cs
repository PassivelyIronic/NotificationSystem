using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using NotificationSystem.Domain.Messages;
using NotificationSystem.Domain.Repositories;

namespace NotificationSystem.Infrastructure.Consumers
{
    public class SendNotificationConsumer : IConsumer<ISendNotification>
    {
        private readonly INotificationRepository _repository;
        private readonly IBus _bus;
        private readonly Random _random = new Random();

        public SendNotificationConsumer(INotificationRepository repository, IBus bus)
        {
            _repository = repository;
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<ISendNotification> context)
        {
            var notification = await _repository.GetByIdAsync(context.Message.Id);

            if (notification == null)
            {
                return;
            }

            notification.Status = "Processing";
            notification.AttemptCount++;
            await _repository.UpdateAsync(notification);

            if (_random.NextDouble() < 0.5)
            {
                Console.WriteLine($"Notification delivery attempt {notification.AttemptCount} failed for ID: {notification.Id}");

                if (notification.AttemptCount >= 3)
                {
                    Console.WriteLine($"Notification failed after 3 attempts for ID: {notification.Id}");
                    notification.Status = "Failed";
                    await _repository.UpdateAsync(notification);
                    return;
                }

                await Task.Delay(5000);

                await _bus.Publish<ISendNotification>(new SendNotificationCommand
                {
                    Id = notification.Id,
                    Content = notification.Content,
                    Channel = notification.Channel,
                    Recipient = notification.Recipient,
                    Timezone = notification.Timezone,
                    ScheduledAt = notification.ScheduledAt
                });

                return;
            }

            Console.WriteLine($"Processing notification for channel {notification.Channel}, ID: {notification.Id}");

            if (string.Equals(notification.Channel, "push", StringComparison.OrdinalIgnoreCase))
            {
                await _bus.Send(new SendPushNotificationCommand
                {
                    Id = notification.Id,
                    Content = notification.Content,
                    Recipient = notification.Recipient
                });
            }
            else if (string.Equals(notification.Channel, "email", StringComparison.OrdinalIgnoreCase))
            {
                await _bus.Send(new SendEmailNotificationCommand
                {
                    Id = notification.Id,
                    Content = notification.Content,
                    Recipient = notification.Recipient
                });
            }

            notification.Status = "Sent";
            await _repository.UpdateAsync(notification);
            Console.WriteLine($"Notification {notification.Id} marked as sent");
        }
    }
}
