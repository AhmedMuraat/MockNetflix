using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Net;
using System.Text;
using Userdata.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Services
builder.Services.AddHealthChecks()
    .AddCheck("sqlserver", new SqlServerHealthCheck("Server=dbuserinfo;Database=UserInfo;User Id=sa;Password=Sjeemaa12!;TrustServerCertificate=true;"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddDbContext<UserInfoContext>(options =>
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

// Configure HTTP request pipeline.
var app = builder.Build();

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps("certhttps.pfx", "Password123");
        });
    });

    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
