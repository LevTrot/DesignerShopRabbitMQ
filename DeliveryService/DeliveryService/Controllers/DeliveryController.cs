using Microsoft.AspNetCore.Mvc;

namespace DeliveryService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeliveryController : ControllerBase
    {
        private readonly DeliveryMemoryStorage _memory;

        public DeliveryController(DeliveryMemoryStorage memory)
        {
            _memory = memory;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            if (_memory.LastMessage == null)
                return Ok("Сообщений ещё не получено.");

            return Ok(new
            {
                Message = _memory.LastMessage.Message,
                Card = _memory.LastMessage.Card,
                ReceivedAt = _memory.ReceivedAt?.ToString("u")
            });
        }
    }
}

