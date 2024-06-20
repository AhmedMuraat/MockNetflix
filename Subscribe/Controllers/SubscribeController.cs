using Microsoft.AspNetCore.Mvc;
using Subscribe.Models;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

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
            _httpClient.BaseAddress = new Uri("http://userinfo:8080"); // Set base address to the service name
        }

        [HttpPost("buycredits")]
        public async Task<IActionResult> BuyCredits(int userId, int amount)
        {
            var user = await GetUserById(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var existingCredits = await _context.Credits.FirstOrDefaultAsync(c => c.ExternalUserId == userId);

            if (existingCredits != null)
            {
                existingCredits.Amount += amount;
                existingCredits.PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow); // Update the purchase date if needed
            }
            else
            {
                var credits = new Credit
                {
                    ExternalUserId = userId,
                    Amount = amount,
                    PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _context.Credits.Add(credits);
            }

            await _context.SaveChangesAsync();

            var updatedCredits = await _context.Credits.Where(c => c.ExternalUserId == userId).SumAsync(c => c.Amount);

            var response = new
            {
                UserId = userId,
                TotalCredits = updatedCredits
            };

            SendMessageToQueue("buy-credits-queue", response);

            return Ok(response);
        }

        [HttpGet("totalcredits/{userId}")]
        public async Task<IActionResult> GetTotalCredits(int userId)
        {
            var totalCredits = await _context.Credits
                .Where(c => c.ExternalUserId == userId)
                .SumAsync(c => c.Amount);

            if (totalCredits == 0)
            {
                return NotFound("User not found or no credits available");
            }

            var response = new
            {
                UserId = userId,
                TotalCredits = totalCredits
            };

            return Ok(response);
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
                .Where(c => c.ExternalUserId == userId)
                .SumAsync(c => c.Amount);

            if (userCredits < requiredCredits)
            {
                return BadRequest("Insufficient credits");
            }

            var endDateTime = plan.Duration.ToLower() == "monthly" ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1);
            var endDate = DateOnly.FromDateTime(endDateTime);

            var userSubscription = new UserSubscription
            {
                ExternalUserId = userId,
                PlanId = planId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = endDate
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Deduct the credits
                    var creditsToDeduct = requiredCredits;
                    var creditsList = await _context.Credits
                        .Where(c => c.ExternalUserId == userId)
                        .OrderBy(c => c.PurchaseDate)
                        .ToListAsync();

                    foreach (var credit in creditsList)
                    {
                        if (creditsToDeduct <= 0)
                            break;

                        if (credit.Amount > creditsToDeduct)
                        {
                            credit.Amount -= creditsToDeduct;
                            creditsToDeduct = 0;
                        }
                        else
                        {
                            creditsToDeduct -= credit.Amount;
                            credit.Amount = 0;
                        }

                        _context.Credits.Update(credit);
                    }

                    _context.UserSubscriptions.Add(userSubscription);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing subscription for user ID {UserId} and plan ID {PlanId}", userId, planId);
                    return StatusCode(500, "Internal server error");
                }
            }

            SendMessageToQueue("subscribe-queue", userSubscription);

            return Ok(userSubscription);
        }

        [HttpGet("haspremium/{userId}")]
        public async Task<IActionResult> HasPremiumSubscription(int userId)
        {
            var hasPremium = await _context.UserSubscriptions
                .AnyAsync(us => us.ExternalUserId == userId && us.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow));

            var response = new
            {
                UserId = userId,
                HasPremium = hasPremium
            };

            return Ok(response);
        }

        private async Task<User> GetUserById(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User found with ID {UserId}", userId);
                    return await response.Content.ReadFromJsonAsync<User>();
                }
                else
                {
                    _logger.LogWarning("User with ID {UserId} not found. Status code: {StatusCode}", userId, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user details from Login service for user ID {UserId}");
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
