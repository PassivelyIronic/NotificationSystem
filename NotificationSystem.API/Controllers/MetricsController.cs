
using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Domain.Repositories;
using System;

namespace NotificationSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly INotificationRepository _repository;

        public MetricsController(INotificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetOverallMetrics()
        {
            var allNotifications = await _repository.GetAllAsync();

            var result = new
            {
                Total = allNotifications.Count(),
                ByStatus = new
                {
                    Waiting = allNotifications.Count(n => n.Status == "Waiting"),
                    Processing = allNotifications.Count(n => n.Status == "Processing"),
                    Sent = allNotifications.Count(n => n.Status == "Sent"),
                    Failed = allNotifications.Count(n => n.Status == "Failed")
                },
                ByChannel = new
                {
                    Push = allNotifications.Count(n => n.Channel.ToLower() == "push"),
                    Email = allNotifications.Count(n => n.Channel.ToLower() == "email")
                }
            };

            return Ok(result);
        }

        [HttpGet("channel/{channel}")]
        public async Task<IActionResult> GetChannelMetrics(string channel)
        {
            var allNotifications = await _repository.GetAllAsync();
            var channelNotifications = allNotifications.Where(n => n.Channel.ToLower() == channel.ToLower());

            var result = new
            {
                Total = channelNotifications.Count(),
                Waiting = channelNotifications.Count(n => n.Status == "Waiting"),
                Processing = channelNotifications.Count(n => n.Status == "Processing"),
                Sent = channelNotifications.Count(n => n.Status == "Sent"),
                Failed = channelNotifications.Count(n => n.Status == "Failed")
            };

            return Ok(result);
        }
    }
}