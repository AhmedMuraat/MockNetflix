using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using Subscribe.Models;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace Subscribe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscribeController : ControllerBase
    {
        private readonly SubContext _context;
        private readonly ILogger<SubscribeController> _logger;
        private readonly HttpClient _httpClient;

        public SubscribeController(SubContext context, ILogger<SubscribeController> logger, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://51.8.3.51:5000"); // Set base address to the user info service
        }

        [HttpPost("buycredits")]
        public async Task<IActionResult> BuyCredits([FromHeader] string authorization, [FromBody] BuyCreditsRequest request)
        {
            var user = await GetUserByIdAsync(authorization, request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found for UserId: {UserId}", request.UserId);
                return NotFound(new { Message = "User not found" });
            }

            var credits = await _context.Credits.FirstOrDefaultAsync(c => c.ExternalUserId == request.UserId);
            if (credits == null)
            {
                credits = new Credit
                {
                    ExternalUserId = request.UserId,
                    Amount = request.Amount,
                    PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                _context.Credits.Add(credits);
                _logger.LogInformation("Created new credit record for UserId: {UserId} with Amount: {Amount}", request.UserId, request.Amount);
            }
            else
            {
                credits.Amount += request.Amount;
                credits.PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
                _logger.LogInformation("Updated existing credit record for UserId: {UserId} with new Amount: {Amount}", request.UserId, credits.Amount);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to credits for UserId: {UserId}", request.UserId);
                throw;
            }

            return Ok(new { UserId = request.UserId, TotalCredits = credits.Amount, Message = "Credits purchased successfully" });
        }

        [HttpGet("totalcredits/{userId}")]
        public async Task<IActionResult> GetTotalCredits([FromHeader] string authorization, [FromRoute] int userId)
        {
            // Ensure the authorization header is correctly parsed and userId is valid
            if (string.IsNullOrWhiteSpace(authorization))
            {
                _logger.LogWarning("Authorization header is missing.");
                return Unauthorized(new { Message = "Authorization header is missing." });
            }

            var user = await GetUserByIdAsync(authorization, userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for UserId: {UserId}", userId);
                return NotFound(new { Message = "User not found" });
            }

            var totalCredits = await _context.Credits
                                             .Where(c => c.ExternalUserId == userId)
                                             .SumAsync(c => c.Amount);

            if (totalCredits == 0)
            {
                return NotFound(new { Message = "No credits found for user" });
            }

            return Ok(new { UserId = userId, TotalCredits = totalCredits });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromHeader] string authorization, [FromBody] SubscribeRequest request)
        {
            var user = await GetUserByIdAsync(authorization, request.UserId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
            if (plan == null)
            {
                return NotFound(new { Message = "Plan not found" });
            }

            var endDateTime = plan.Duration.ToLower() == "monthly" ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1);
            var userSubscription = new UserSubscription
            {
                ExternalUserId = request.UserId,
                PlanId = request.PlanId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(endDateTime)
            };

            _context.UserSubscriptions.Add(userSubscription);
            await _context.SaveChangesAsync();

            return Ok(new { UserId = request.UserId, PlanId = request.PlanId, Message = "Subscription successful" });
        }

        [HttpGet("haspremium/{userId}")]
        public async Task<IActionResult> HasPremiumSubscription([FromHeader] string authorization, [FromRoute] int userId)
        {
            var user = await GetUserByIdAsync(authorization, userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var hasPremium = await _context.UserSubscriptions
                .AnyAsync(us => us.ExternalUserId == userId && us.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow));

            return Ok(new { UserId = userId, HasPremium = hasPremium });
        }

        private async Task<User> GetUserByIdAsync(string authorization, int userId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authorization.Split(" ")[1]);
            var response = await _httpClient.GetAsync($"/api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>();
            }

            _logger.LogWarning("Failed to fetch user details for userId: {UserId}. Status code: {StatusCode}", userId, response.StatusCode);
            return null;
        }
    }

    public class BuyCreditsRequest
    {
        public int UserId { get; set; }
        public int Amount { get; set; }
    }

    public class SubscribeRequest
    {
        public int UserId { get; set; }
        public int PlanId { get; set; }
    }
}
