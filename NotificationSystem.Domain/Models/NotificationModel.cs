using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationSystem.Domain.Models
{
    public class Notification
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Channel { get; set; } = null!;  // "push" or "email"
        public string Recipient { get; set; } = null!;
        public string Timezone { get; set; } = null!;
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = "Waiting";
        public int AttemptCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum NotificationStatus
    {
        Waiting,
        Processing,
        Sent,
        Failed
    }

    public enum NotificationChannel
    {
        Push,
        Email
    }
}
