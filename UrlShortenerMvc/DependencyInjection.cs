using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using UrlShortenerMvc.Data;
using UrlShortenerMvc.Models;
using UrlShortenerMvc.Services;
using UrlShortenerMvc.Services.ClickTracking;

namespace UrlShortenerMvc;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddControllersWithViews();

        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = "UrlShortener:";

            // For Local Dev
            // options.Configuration = configuration.GetConnectionString("Redis");

            // For Production
            var redis = configuration.GetSection("Redis");
            options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
            {
                EndPoints = { redis["Endpoint"]! },
                User = redis["Username"],
                Password = redis["Password"],
                Ssl = true,
                AbortOnConnectFail = false
            };
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        services.AddHttpContextAccessor();
        services.AddSingleton<IUrlValidationService, UrlValidationService>();
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddScoped<ILinkService, LinkService>();
        services.AddSingleton<IGeoIpService, GeoIpService>();
        services.AddSingleton<IClickQueue, ClickQueue>();
        services.AddHostedService<ClickWorker>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"global:{ip}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy("CreateLink", httpContext =>
            {
                var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
                if (isAuthenticated)
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    return RateLimitPartition.GetFixedWindowLimiter(
                        userId ?? "authenticated-unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
                }

                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"anonymous:{ip}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy("Redirect", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    $"redirect:{ip}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}