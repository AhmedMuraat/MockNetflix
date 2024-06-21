using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using Subscribe.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Subscribe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscribeController : ControllerBase
    {
        private readonly SubContext _context;
        private readonly IModel _channel;
        private readonly ILogger<SubscribeController> _logger;
        private readonly HttpClient _httpClient;

        public SubscribeController(SubContext context, IModel channel, ILogger<SubscribeController> logger, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _channel = channel;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://48.217.203.73:5000"); // Set base address to the user info service
        }

        [HttpPost("buycredits")]
        public async Task<IActionResult> BuyCredits([FromHeader] string authorization, [FromBody] int amount)
        {
            var user = await GetUserFromTokenAsync(authorization);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var credits = await _context.Credits.FirstOrDefaultAsync(c => c.ExternalUserId == user.UserId);
            if (credits == null)
            {
                credits = new Credit
                {
                    ExternalUserId = user.UserId,
                    Amount = amount,
                    PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                _context.Credits.Add(credits);
            }
            else
            {
                credits.Amount += amount;
                credits.PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
                _context.Credits.Update(credits);
            }

            await _context.SaveChangesAsync();

            SendMessageToQueue("buy-credits-queue", new { UserId = user.UserId, Amount = amount });

            return Ok(new { UserId = user.UserId, TotalCredits = credits.Amount, Message = "Credits purchased successfully" });
        }

        [HttpGet("totalcredits")]
        public async Task<IActionResult> GetTotalCredits([FromHeader] string authorization)
        {
            var user = await GetUserFromTokenAsync(authorization);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var totalCredits = await _context.Credits.Where(c => c.ExternalUserId == user.UserId).SumAsync(c => c.Amount);

            if (totalCredits == 0)
            {
                return NotFound(new { Message = "No credits found for user" });
            }

            return Ok(new { UserId = user.UserId, TotalCredits = totalCredits });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromHeader] string authorization, [FromBody] int planId)
        {
            var user = await GetUserFromTokenAsync(authorization);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null)
            {
                return NotFound(new { Message = "Plan not found" });
            }

            var endDateTime = plan.Duration.ToLower() == "monthly" ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1);
            var userSubscription = new UserSubscription
            {
                ExternalUserId = user.UserId,
                PlanId = planId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(endDateTime)
            };

            _context.UserSubscriptions.Add(userSubscription);
            await _context.SaveChangesAsync();

            SendMessageToQueue("subscribe-queue", new { UserId = user.UserId, PlanId = planId });

            return Ok(new { UserId = user.UserId, PlanId = planId, Message = "Subscription successful" });
        }

        [HttpGet("haspremium")]
        public async Task<IActionResult> HasPremiumSubscription([FromHeader] string authorization)
        {
            var user = await GetUserFromTokenAsync(authorization);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var hasPremium = await _context.UserSubscriptions
                .AnyAsync(us => us.ExternalUserId == user.UserId && us.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow));

            return Ok(new { UserId = user.UserId, HasPremium = hasPremium });
        }

        private async Task<User> GetUserFromTokenAsync(string authorization)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(authorization.Split(" ")[1]);
            var userId = int.Parse(jwtToken.Claims.First(c => c.Type == "id").Value);

            var response = await _httpClient.GetAsync($"/api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>();
            }

            _logger.LogWarning("Failed to fetch user details. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        private void SendMessageToQueue(string queueName, object message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                _channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: null,
                                     body: body);
                _logger.LogInformation("Message sent to queue {QueueName}: {Message}", queueName, JsonSerializer.Serialize(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue {QueueName}: {Message}", queueName, JsonSerializer.Serialize(message));
            }
        }
    }
}
