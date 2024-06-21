using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Userdata.Models;

namespace Userdata.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserInfoContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserInfoContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
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
        public async Task<IActionResult> PutUserInfo(int id, [FromBody] UserDatum userInfo)
        {
            if (id != userInfo.UserInfoId)
            {
                return BadRequest();
            }

            _context.Entry(userInfo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
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

            return NoContent();
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
