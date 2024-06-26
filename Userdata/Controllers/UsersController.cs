using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Userdata.Models;
using Microsoft.Extensions.Logging;
using Userdata.Rabbit;

namespace Userdata.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly UserInfoContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IModel _channel;

        public UsersController(UserInfoContext context, ILogger<UsersController> logger, IModel channel)
        {
            _context = context;
            _logger = logger;
            _channel = channel;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.UserData.ToListAsync();
            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            _logger.LogInformation("Received request to fetch user info for userId: {UserId}", id);

            var user = await _context.UserData.FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                _logger.LogWarning("User info for userId: {UserId} not found", id);
                return NotFound();
            }

            _logger.LogInformation("User info for userId: {UserId} found", id);
            return Ok(user);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<IActionResult> PostUser([FromBody] UserDatum user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _context.UserData.AddAsync(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserInfoId }, user);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, [FromBody] UserUpdateRequest updateRequest)
        {
            if (id != updateRequest.UserInfoId)
            {
                return BadRequest();
            }

            var existingUser = await _context.UserData.FirstOrDefaultAsync(u => u.UserInfoId == id);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Name = updateRequest.Name;
            existingUser.LastName = updateRequest.LastName;
            existingUser.Address = updateRequest.Address;
            existingUser.DateOfBirth = updateRequest.DateOfBirth;

            _context.Entry(existingUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                var message = new
                {
                    UserId = existingUser.UserId,
                    Username = updateRequest.Username,
                    Email = updateRequest.Email
                };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                _channel.BasicPublish(exchange: "", routingKey: "user-update-queue", basicProperties: null, body: body);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.UserData.Any(e => e.UserInfoId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.UserData.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            _context.UserData.Remove(user);

            try
            {
                await _context.SaveChangesAsync();

                var message = new { UserId = id };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                _channel.BasicPublish(exchange: "", routingKey: "user-delete-login-queue", basicProperties: null, body: body);
                _channel.BasicPublish(exchange: "", routingKey: "user-delete-subscription-queue", basicProperties: null, body: body);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                throw;
            }
        }
    }
}
