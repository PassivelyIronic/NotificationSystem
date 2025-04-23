using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using NotificationSystem.Domain.Messages;
using NotificationSystem.Domain.Models;
using NotificationSystem.Domain.Repositories;
using System.Globalization;

namespace NotificationSystem.Infrastructure.Services
{
    public class NotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IMessageScheduler _scheduler;
        private const int AllowedHoursStart = 8;
        private const int AllowedHoursEnd = 22;

        public NotificationService(INotificationRepository repository, IMessageScheduler scheduler)
        {
            _repository = repository;
            _scheduler = scheduler;
        }

        public async Task<Notification> CreateNotificationAsync(NotificationDto notificationDto)
        {
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(notificationDto.Timezone);
            }
            catch (Exception)
            {
                throw new ArgumentException($"Invalid timezone: {notificationDto.Timezone}");
            }

            var utcScheduledTime = TimeZoneInfo.ConvertTimeToUtc(notificationDto.ScheduledAt,
                TimeZoneInfo.FindSystemTimeZoneById(notificationDto.Timezone));

            if (utcScheduledTime <= DateTime.UtcNow)
            {
                throw new ArgumentException("Scheduled time must be in the future");
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(notificationDto.Timezone);
            var localScheduledTime = TimeZoneInfo.ConvertTimeFromUtc(utcScheduledTime, timeZone);

            if (localScheduledTime.Hour < AllowedHoursStart || localScheduledTime.Hour >= AllowedHoursEnd)
            {
                var nextDay = localScheduledTime.Date.AddDays(1);
                localScheduledTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, AllowedHoursStart, 0, 0);
                utcScheduledTime = TimeZoneInfo.ConvertTimeToUtc(localScheduledTime, timeZone);
                Console.WriteLine($"Adjusted scheduled time due to quiet hours: {utcScheduledTime}");
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Content = notificationDto.Content,
                Channel = notificationDto.Channel.ToLowerInvariant(),
                Recipient = notificationDto.Recipient,
                Timezone = notificationDto.Timezone,
                ScheduledAt = utcScheduledTime,
                Status = "Waiting"
            };

            await _repository.CreateAsync(notification);

            await _scheduler.ScheduleSend(
                new Uri($"queue:send-notification"),
                utcScheduledTime,
                new SendNotificationCommand
                {
                    Id = notification.Id,
                    Content = notification.Content,
                    Channel = notification.Channel,
                    Recipient = notification.Recipient,
                    Timezone = notification.Timezone,
                    ScheduledAt = notification.ScheduledAt
                });

            return notification;
        }

        public async Task<Notification?> GetNotificationStatusAsync(string id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteNotificationAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<Notification?> ForceNotificationSendAsync(string id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null)
                return null;

            notification.ScheduledAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification);

            await _scheduler.ScheduleSend<ISendNotification>(
                new Uri($"queue:send-notification"),
                notification.ScheduledAt,
                new SendNotificationCommand
                {
                    Id = notification.Id,
                    Content = notification.Content,
                    Channel = notification.Channel,
                    Recipient = notification.Recipient,
                    Timezone = notification.Timezone,
                    ScheduledAt = notification.ScheduledAt
                });

            return notification;
        }
    }

    public class NotificationDto
    {
        public string Content { get; set; } = null!;
        public string Channel { get; set; } = null!;
        public string Recipient { get; set; } = null!;
        public string Timezone { get; set; } = null!;
        public DateTime ScheduledAt { get; set; }
    }
}
