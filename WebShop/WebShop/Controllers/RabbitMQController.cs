using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System;

namespace WebShop.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RabbitMQController : ControllerBase
    {
        private readonly IRabbitMqService _rabbit;

        public RabbitMQController(IRabbitMqService rabbit)
        {
            _rabbit = rabbit;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            try
            {
                var reply = await _rabbit.SendToConsumerAndWaitAsync(message);
                Console.WriteLine("Получено от Consumer: " + reply);
                //var card = reply; // псевдокарта
                var result = await _rabbit.SendToAccountingAndWaitAsync(message);
                if (result.Contains("Карта отклонена"))
                {
                    return BadRequest(result);
                }

                await _rabbit.SendToDeliveryAsync(result, message);
                return Ok(new
                {
                    Consumer = reply,
                    Accounting = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
