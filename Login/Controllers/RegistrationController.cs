using Login.Handlers;
using Login.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Login.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly NetflixLoginContext _dbContext;
        private readonly JWTSettings _jwtSettings;
        private readonly RabbitMQ.Client.IModel _rabbitMqChannel;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(NetflixLoginContext datacontext, IOptions<JWTSettings> jwtSettings, RabbitMQ.Client.IModel rabbitMqChannel, ILogger<RegistrationController> logger)
        {
            _dbContext = datacontext;
            _jwtSettings = jwtSettings.Value;
            _rabbitMqChannel = rabbitMqChannel;
            _logger = logger;
        }

        [HttpPost("{username}&{name}&{lastName}&{email}&{password}&{address}&{year:int}&{month:int}&{day:int}&{role:int}")]
        public async Task<ActionResult<UserWithToken>> User(string username, string name, string lastName, string email, string password, string address, int year, int month, int day, int role)
        {
            _logger.LogInformation("Registering new user: {Username}, {Email}", username, email);

            if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                _logger.LogWarning("Invalid date parameters: {Year}-{Month}-{Day}", year, month, day);
                return BadRequest("Invalid date parameters.");
            }

            var hashedPassword = Password.hashPassword(password);
            var parsedDateOfBirth = new DateOnly(year, month, day);

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = hashedPassword,
                RoleId = role
            };

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User registered: {UserId}", newUser.Id);

            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = newUser.Id,
                Username = newUser.Username,
                Name = name,
                LastName = lastName,
                Email = newUser.Email,
                Address = address,
                DateOfBirth = parsedDateOfBirth,
                AccountCreated = DateTime.UtcNow,
                RoleId = role
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userCreatedEvent));
            _rabbitMqChannel.BasicPublish(exchange: "", routingKey: "user.created", basicProperties: null, body: messageBody);

            _logger.LogInformation("User creation message published to RabbitMQ: {UserId}", newUser.Id);

            var user = await _dbContext.Users.Include(u => u.Role)
                                             .Where(u => u.Id == newUser.Id).FirstOrDefaultAsync();

            UserWithToken userWithToken = null;

            if (user != null)
            {
                var refreshToken = GenerateRefreshToken();
                user.RefreshTokens.Add(refreshToken);
                await _dbContext.SaveChangesAsync();

                userWithToken = new UserWithToken(user)
                {
                    RefreshToken = refreshToken.Token
                };
            }

            if (userWithToken == null)
            {
                _logger.LogWarning("UserWithToken is null for user: {UserId}", newUser.Id);
                return NotFound();
            }

            userWithToken.AccessToken = GenerateAccessToken(user.Id);
            _logger.LogInformation("User logged in: {UserId}", newUser.Id);
            return userWithToken;
        }


        // Other methods...

        private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
            }

            return refreshToken;
        }

        private string GenerateAccessToken(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, Convert.ToString(userId))
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
