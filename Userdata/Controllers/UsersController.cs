using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Userdata.Models;

namespace Userdata.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
            private readonly UserInfoContext _context;

            public UsersController(UserInfoContext context)
            {
                _context = context;
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
                var userInfo = await _context.UserData.FindAsync(id);

                if (userInfo == null)
                {
                    return NotFound();
                }

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
