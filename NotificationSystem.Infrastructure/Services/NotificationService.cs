// NotificationSystem.Infrastructure/Services/NotificationService.cs
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

            // Only schedule if it's in the future
            if (utcScheduledTime > DateTime.UtcNow)
            {
                // Schedule notification delivery
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
            }
            else
            {
                // Send immediately
                await _scheduler.ScheduleSend(
                    new Uri($"queue:send-notification"),
                    DateTime.UtcNow,
                    new SendNotificationCommand
                    {
                        Id = notification.Id,
                        Content = notification.Content,
                        Channel = notification.Channel,
                        Recipient = notification.Recipient,
                        Timezone = notification.Timezone,
                        ScheduledAt = notification.ScheduledAt
                    });
            }

            return notification;
        }

        public async Task<Notification?> GetNotificationStatusAsync(string id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteNotificationAsync(string id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null)
                return false;

            if (notification.Status == "Waiting")
            {
                return await _repository.DeleteAsync(id);
            }

            return false; // Can't delete notifications that are in progress, sent, or failed
        }

        public async Task<Notification?> ForceNotificationSendAsync(string id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null)
                return null;

            if (notification.Status != "Waiting")
                return notification; // Can't force if not in waiting status

            notification.ScheduledAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification);

            // Send immediately
            await _scheduler.ScheduleSend<ISendNotification>(
                new Uri($"queue:send-notification"),
                DateTime.UtcNow,
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