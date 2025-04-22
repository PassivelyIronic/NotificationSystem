using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Infrastructure.Services;

namespace NotificationSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationDto notificationDto)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(notificationDto);
                return CreatedAtAction(nameof(GetNotificationStatus), new { id = notification.Id }, notification);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the notification", error = ex.Message });
            }
        }

        [HttpGet("status/{id}")]
        public async Task<IActionResult> GetNotificationStatus(string id)
        {
            var notification = await _notificationService.GetNotificationStatusAsync(id);
            if (notification == null)
                return NotFound(new { message = $"Notification with ID {id} not found" });

            return Ok(new { id = notification.Id, status = notification.Status });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(string id)
        {
            var result = await _notificationService.DeleteNotificationAsync(id);
            if (!result)
                return NotFound(new { message = $"Notification with ID {id} not found" });

            return Ok(new { message = $"Notification with ID {id} deleted successfully" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ForceSendNotification(string id)
        {
            var notification = await _notificationService.ForceNotificationSendAsync(id);
            if (notification == null)
                return NotFound(new { message = $"Notification with ID {id} not found" });

            return Ok(new { message = $"Notification with ID {id} scheduled for immediate delivery", notification });
        }
    }
}
