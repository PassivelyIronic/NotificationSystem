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
        private readonly IBus _bus;
        private const int AllowedHoursStart = 8;
        private const int AllowedHoursEnd = 22;

        public NotificationService(INotificationRepository repository, IBus bus)
        {
            _repository = repository;
            _bus = bus;
        }

        public async Task<Notification> CreateNotificationAsync(NotificationDto notificationDto)
        {
            // Validate timezone
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

            // Check if the local scheduled time is within allowed hours
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(notificationDto.Timezone);
            var localScheduledTime = TimeZoneInfo.ConvertTimeFromUtc(utcScheduledTime, timeZone);

            if (localScheduledTime.Hour < AllowedHoursStart || localScheduledTime.Hour >= AllowedHoursEnd)
            {
                // Adjust to next day's allowed start time
                var nextDay = localScheduledTime.Date.AddDays(1);
                localScheduledTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, AllowedHoursStart, 0, 0);
                utcScheduledTime = TimeZoneInfo.ConvertTimeToUtc(localScheduledTime, timeZone);
                Console.WriteLine($"Adjusted scheduled time due to quiet hours: {utcScheduledTime}");
            }

            // Create notification record
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

            // Schedule notification delivery
            await _bus.ScheduleSend<ISendNotification>(
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

            // Send immediately
            await _bus.Send<ISendNotification>(
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
        public string Channel { get; set; } = null!;  // "push" or "email"
        public string Recipient { get; set; } = null!;
        public string Timezone { get; set; } = null!;
        public DateTime ScheduledAt { get; set; }
    }
}
