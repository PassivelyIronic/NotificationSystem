using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Domain.Repositories;

namespace NotificationSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly INotificationRepository _repository;

        public StatusController(INotificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStatusCounts()
        {
            var waiting = await _repository.GetByStatusAsync("Waiting");
            var processing = await _repository.GetByStatusAsync("Processing");
            var sent = await _repository.GetByStatusAsync("Sent");
            var failed = await _repository.GetByStatusAsync("Failed");

            var result = new
            {
                Waiting = waiting.Count(),
                Processing = processing.Count(),
                Sent = sent.Count(),
                Failed = failed.Count()
            };

            return Ok(result);
        }

        [HttpGet("waiting")]
        public async Task<IActionResult> GetWaitingNotifications()
        {
            var notifications = await _repository.GetByStatusAsync("Waiting");
            return Ok(notifications);
        }

        [HttpGet("sent")]
        public async Task<IActionResult> GetSentNotifications()
        {
            var notifications = await _repository.GetByStatusAsync("Sent");
            return Ok(notifications);
        }

        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedNotifications()
        {
            var notifications = await _repository.GetByStatusAsync("Failed");
            return Ok(notifications);
        }
    }
}
