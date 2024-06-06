using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subscription.Models;

namespace Subscription.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly SubContext _context;
        private readonly RabbitMqPublisher _rabbitMqPublisher;

        public SubscriptionController(SubContext context, RabbitMqPublisher rabbitMqPublisher)
        {
            _context = context;
            _rabbitMqPublisher = rabbitMqPublisher;
        }

        [HttpPost("buy-credits")]
        public async Task<IActionResult> BuyCredits(int userId, int credits)
        {
            var userCredits = _context.Credits.FirstOrDefault(c => c.UserId == userId);
            if (userCredits == null)
            {
                userCredits = new Credit
                {
                    UserId = userId,
                    AvailableCredits = credits
                };
                _context.Credits.Add(userCredits);
            }
            else
            {
                userCredits.AvailableCredits += credits;
                _context.Credits.Update(userCredits);
            }
            await _context.SaveChangesAsync();

            var message = $"User {userId} bought {credits} credits.";
            _rabbitMqPublisher.PublishMessage(message);

            return Ok(userCredits);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(int userId)
        {
            const int subscriptionCost = 10;
            var userCredits = _context.Credits.FirstOrDefault(c => c.UserId == userId);
            if (userCredits == null || userCredits.AvailableCredits < subscriptionCost)
            {
                return BadRequest("Insufficient credits.");
            }

            var subscription = new Models.Subscription
            {
                UserId = userId,
                SubscriptionType = "Premium",
                Credits = subscriptionCost,
                SubscriptionStart = DateOnly.FromDateTime(DateTime.UtcNow),
                SubscriptionEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))
            };

            userCredits.AvailableCredits -= subscriptionCost;
            _context.Credits.Update(userCredits);
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var message = $"User {userId} subscribed to Premium plan.";
            _rabbitMqPublisher.PublishMessage(message);

            return Ok(subscription);
        }
    }
}
