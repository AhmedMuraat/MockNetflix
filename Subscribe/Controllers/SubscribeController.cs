using Microsoft.AspNetCore.Mvc;
using Subscribe.Models;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace Subscribe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscribeController : Controller
    {
        private readonly SubContext _context;
        private readonly IModel _channel;
        private readonly ILogger<SubscribeController> _logger;
        private readonly HttpClient _httpClient;

        public SubscribeController(SubContext context, IModel channel, ILogger<SubscribeController> logger, HttpClient httpClient)
        {
            _context = context;
            _channel = channel;
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpPost("buy-credits")]
        public async Task<IActionResult> BuyCredits(int userId, int amount)
        {
            var user = await GetUserById(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var credits = new Credit
            {
                ExternalUserId = user.UserId,
                Amount = amount,
                PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Credits.Add(credits);
            await _context.SaveChangesAsync();

            SendMessageToQueue("buy-credits-queue", credits);

            return Ok(credits);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(int userId, int planId)
        {
            var user = await GetUserById(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null)
            {
                return NotFound("Plan not found");
            }

            int requiredCredits = plan.Duration.ToLower() == "monthly" ? 10 : 100;

            var userCredits = await _context.Credits
                .Where(c => c.ExternalUserId == user.UserId)
                .SumAsync(c => c.Amount);

            if (userCredits < requiredCredits)
            {
                return BadRequest("Insufficient credits");
            }

            var endDateTime = plan.Duration.ToLower() == "monthly" ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1);
            var endDate = DateOnly.FromDateTime(endDateTime);

            var userSubscription = new UserSubscription
            {
                ExternalUserId = user.UserId,
                PlanId = planId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = endDate
            };

            _context.UserSubscriptions.Add(userSubscription);
            await _context.SaveChangesAsync();

            SendMessageToQueue("subscribe-queue", userSubscription);

            return Ok(userSubscription);
        }

        private async Task<User> GetUserById(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:8090/api/Users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user details from Login service");
            }
            return null;
        }

        private void SendMessageToQueue(string queueName, object message)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            _channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: body);
            _logger.LogInformation("Message sent to queue {QueueName}: {Message}", queueName, message);
        }
    }
}
