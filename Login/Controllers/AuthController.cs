using Login.Handlers;
using Login.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Login.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly NetflixLoginContext _context;
        private readonly IConfiguration _configuration;
        private readonly IModel _rabbitMqChannel;
        private readonly ILogger<AuthController> _logger;

        public AuthController(NetflixLoginContext context, IConfiguration configuration, IModel rabbitMqChannel, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _rabbitMqChannel = rabbitMqChannel;
            _logger = logger;

            _rabbitMqChannel.QueueDeclare(queue: "user.created", durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user, string name, string lastName, string address, string dateOfBirth, int role)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("User already exists.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.RoleId = role;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Publish user creation event to RabbitMQ
            var parsedDateOfBirth = DateOnly.Parse(dateOfBirth);

            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = user.Id,
                Username = user.Username,
                Name = name,
                LastName = lastName,
                Email = user.Email,
                Address = address,
                DateOfBirth = parsedDateOfBirth,
                AccountCreated = DateTime.UtcNow,
                RoleId = role
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userCreatedEvent));
            _rabbitMqChannel.BasicPublish(exchange: "", routingKey: "user.created", basicProperties: null, body: messageBody);

            _logger.LogInformation("User creation message published to RabbitMQ: {UserId}", user.Id);

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User login)
        {
            var user = await _context.Users.Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.Email == login.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
                return Unauthorized();

            var userWithToken = new UserWithToken(user)
            {
                AccessToken = GenerateAccessToken(user),
                RefreshToken = GenerateRefreshToken(user)
            };

            return Ok(userWithToken);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest refreshRequest)
        {
            var principal = GetPrincipalFromExpiredToken(refreshRequest.AccessToken);
            if (principal == null)
                return BadRequest("Invalid access token or refresh token.");

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == principal.Identity.Name);
            if (user == null || !user.RefreshTokens.Any(rt => rt.Token == refreshRequest.RefreshToken && rt.ExpiryDate > DateTime.UtcNow))
                return BadRequest("Invalid access token or refresh token.");

            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken(user);

            return new ObjectResult(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role?.RoleDesc)
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken(User user)
        {
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(refreshToken);
            _context.SaveChanges();

            return refreshToken.Token;
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
