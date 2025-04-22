using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationSystem.Domain.Messages
{
    public interface ISendNotification
    {
        string Id { get; }
        string Content { get; }
        string Channel { get; }
        string Recipient { get; }
        string Timezone { get; }
        DateTime ScheduledAt { get; }
    }

    public class SendNotificationCommand : ISendNotification
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Channel { get; set; } = null!;
        public string Recipient { get; set; } = null!;
        public string Timezone { get; set; } = null!;
        public DateTime ScheduledAt { get; set; }
    }

    public class SendPushNotificationCommand
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Recipient { get; set; } = null!;
    }

    public class SendEmailNotificationCommand
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Recipient { get; set; } = null!;
    }
}
