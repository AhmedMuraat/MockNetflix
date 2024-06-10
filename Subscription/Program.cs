using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Subscription.Models;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel early in the configuration
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (!builder.Environment.IsDevelopment())
    {
        serverOptions.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps("certhttps.pfx", "Password123");
        });
    }
});

// Configure Services
builder.Services.AddHealthChecks()
    .AddCheck("sqlserver", new SqlServerHealthCheck("Server=dbsubscription;Database=Sub;User Id=sa;Password=Sjeemaa12!;TrustServerCertificate=true;"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<RabbitMqConsumer>();
builder.Services.AddDbContext<SubContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("thisisasecretkeyanddontsharewithanyone")),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build the app
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("MyPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();  // This is essential to place after UseAuthorization
app.MapHealthChecks("/health");

app.Run();

// Health Check Class
public class SqlServerHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public SqlServerHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                if (connection.State == ConnectionState.Open)
                {
                    return HealthCheckResult.Healthy("SQL Server is available.");
                }
            }
            return HealthCheckResult.Unhealthy("SQL Server is not available.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"SQL Server check failed: {ex.Message}");
        }
    }
}
