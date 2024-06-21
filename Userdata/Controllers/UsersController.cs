using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Userdata.Models;
using Userdata.Rabbit;

namespace Userdata.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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


        // GET: api/UserInfo
        [HttpGet]
            
            public async Task<IActionResult> GetUsersInfo()
            {
                var usersInfo = await _context.UserData.ToListAsync();
                return Ok(usersInfo);
            }

        // GET: api/UserInfo/5
            [HttpGet("{id}")]
            public async Task<IActionResult> GetUserInfo(int id)
            {
                _logger.LogInformation("Received request to fetch user info for userId: {UserId}", id);

                // Find the user info by UserId, not UserInfoId
                var userInfo = await _context.UserData.FirstOrDefaultAsync(u => u.UserId == id);

                if (userInfo == null)
                {
                    _logger.LogWarning("User info for userId: {UserId} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("User info for userId: {UserId} found", id);
                return Ok(userInfo);
            }

        // POST: api/UserInfo

        [HttpPost]
            public async Task<IActionResult> PostUserInfo([FromBody] UserDatum userInfo)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.UserData.Add(userInfo);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetUserInfo", new { id = userInfo.UserInfoId }, userInfo);
            }

        // PUT: api/UserInfo/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserInfo(int id, [FromBody] UserDatum userInfo, [FromBody] UserUpdateInfo updateInfo)
        {
            if (id != userInfo.UserInfoId)
            {
                return BadRequest();
            }

            var existingUserInfo = await _context.UserData.FirstOrDefaultAsync(u => u.UserInfoId == id);
            if (existingUserInfo == null)
            {
                return NotFound();
            }

            // Update the existing user info
            existingUserInfo.Name = userInfo.Name;
            existingUserInfo.LastName = userInfo.LastName;
            existingUserInfo.Address = userInfo.Address;
            existingUserInfo.DateOfBirth = userInfo.DateOfBirth;

            _context.Entry(existingUserInfo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Send a RabbitMQ message to update the Login database
                var message = new
                {
                    UserId = existingUserInfo.UserId,
                    Username = updateInfo.Username,
                    Email = updateInfo.Email
                };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                _channel.BasicPublish(exchange: "",
                                     routingKey: "user-update-queue",
                                     basicProperties: null,
                                     body: body);

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

        // DELETE: api/UserInfo/5
        [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteUserInfo(int id)
            {
                var userInfo = await _context.UserData.FindAsync(id);
                if (userInfo == null)
                {
                    return NotFound();
                }

                _context.UserData.Remove(userInfo);
                await _context.SaveChangesAsync();

                return NoContent();
            }
    }
}
