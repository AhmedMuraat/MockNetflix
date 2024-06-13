using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subscription.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly SubContext _context;

    public SubscriptionController(SubContext context)
    {
        _context = context;
    }

    [HttpPost("buy-credits")]
    public async Task<IActionResult> BuyCredits(int externalUserId, int amount)
    {
        var credits = new Credit
        {
            ExternalUserId = externalUserId,
            Amount = amount,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _context.Credits.Add(credits);
        await _context.SaveChangesAsync();

        return Ok(credits);
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(int externalUserId, int planId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
        {
            return NotFound("Plan not found");
        }

        int requiredCredits = plan.Duration.ToLower() == "monthly" ? 10 : 100;

        var userCredits = await _context.Credits
            .Where(c => c.ExternalUserId == externalUserId)
            .SumAsync(c => c.Amount);

        if (userCredits < requiredCredits)
        {
            return BadRequest("Insufficient credits");
        }

        var endDateTime = plan.Duration.ToLower() == "monthly" ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1);
        var endDate = DateOnly.FromDateTime(endDateTime);

        var userSubscription = new UserSubscription
        {
            ExternalUserId = externalUserId,
            PlanId = planId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = endDate
        };

        _context.UserSubscriptions.Add(userSubscription);

        var remainingCredits = new Credit
        {
            ExternalUserId = externalUserId,
            Amount = -requiredCredits,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        _context.Credits.Add(remainingCredits);

        await _context.SaveChangesAsync();

        return Ok(userSubscription);
    }
}
