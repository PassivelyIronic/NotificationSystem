using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using NotificationSystem.Domain.Models;
using NotificationSystem.Domain.Repositories;

namespace NotificationSystem.Infrastructure.Repositories
{
    public class MongoNotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<Notification> _notifications;

        public MongoNotificationRepository(IMongoDatabase database)
        {
            _notifications = database.GetCollection<Notification>("notifications");
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            await _notifications.InsertOneAsync(notification);
            return notification;
        }

        public async Task<Notification?> GetByIdAsync(string id)
        {
            return await _notifications.Find(n => n.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            await _notifications.ReplaceOneAsync(n => n.Id == notification.Id, notification);
            return notification;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _notifications.DeleteOneAsync(n => n.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<IEnumerable<Notification>> GetAllPendingAsync()
        {
            return await _notifications.Find(n => n.Status == "Waiting" && n.ScheduledAt <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByStatusAsync(string status)
        {
            return await _notifications.Find(n => n.Status == status)
                .ToListAsync();
        }
    }
}
