using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotificationSystem.Domain.Models;

namespace NotificationSystem.Domain.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification?> GetByIdAsync(string id);
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<Notification>> GetAllPendingAsync();
        Task<IEnumerable<Notification>> GetByStatusAsync(string status);
        Task<IEnumerable<Notification>> GetAllAsync();
    }
}
