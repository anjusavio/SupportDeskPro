/// <summary>
/// Application entry point and startup configuration.
/// Configures Serilog structured logging, dependency injection,
/// middleware pipeline, and Swagger documentation.
/// Middleware order is critical — ExceptionMiddleware must be first
/// to catch exceptions from all subsequent middleware.
/// </summary>
using Microsoft.OpenApi.Models;
using Serilog;
using SupportDeskPro.API.Middleware;
using SupportDeskPro.Application;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Infrastructure;
using SupportDeskPro.Infrastructure.Services;
using SupportDeskPro.Infrastructure.Settings;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;


// ── SERILOG BOOTSTRAP LOGGER ─────────────────────────────────
// Bootstrap logger captures startup errors before full config loads
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SupportDeskPro API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddMemoryCache();

    // ── SERILOG FULL CONFIGURATION ────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                "{Message:lj} " +
                "{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/supportdesk-.log",
                rollingInterval: RollingInterval.Day,//every day new log file
                retainedFileCountLimit: 30,//Always keeps exactly 30 days of logs -Older files deleted automatically
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} " +
                "[{Level:u3}] " +
                "[{ThreadId}] " +
                "{Message:lj}" +
                "{NewLine}{Exception}")
            //adding to Azure Applictaion Insight
            .WriteTo.ApplicationInsights(
            context.Configuration["ApplicationInsights:ConnectionString"],
            TelemetryConverter.Traces)

    );

    // ── SERVICES ──────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SupportDesk Pro API",
            Version = "v1"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token here",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        c.AddSecurityDefinition("Bearer", securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });
    });
    builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

    // Makes ALL .NET responses camelCase automatically
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy
                = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy =>
            {
                policy.WithOrigins("http://localhost:3000",
                                   "https://kind-coast-000fe8c1e.2.azurestaticapps.net"
                      )
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<IEmailService, EmailService>();


    // ── RATE LIMITING & THROTTLING ────────────────────────────────────────────
    // Rate Limiting  = reject requests when limit exceeded (429 Too Many Requests)
    // Throttling     = queue and delay requests instead of rejecting them
    //
    // Strategy:
    //   Unauthenticated endpoints (login, register) → limit per IP address
    //   → IP is the only identifier before a user logs in
    //   → Prevents brute force attacks from a single machine
    //
    //   Authenticated endpoints (AI, uploads) → limit per User ID
    //   → JWT UserId is the partition key
    //   → Each user gets their own independent limit
    //   → Prevents one user from exhausting shared resources
    //
    // Policies:
    //   global  → 100 req/min per IP  — baseline protection for all endpoints
    //   auth    → 5 req/min per IP    — strict limit to block brute force on login
    //   ai      → 10 req/min per user — controls Claude API cost per user
    //   upload  → 20 req/min per user — controls Azure Blob Storage cost per user
    //   heavy   → 30 req/min per user — dashboard and reporting queries (DB heavy)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        // ── 1. GLOBAL LIMITER — applies to every endpoint automatically ───────
        // All requests pass through this first before hitting specific policies.
        // Acts as a baseline safety net — no [EnableRateLimiting] needed.
        // Uses IP address as partition key — one bucket per client IP.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                // Use IP address for global limit
                // Falls back to "unknown" if IP cannot be determined
                var ipAddress = context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"global:{ipAddress}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        // 100 requests allowed per 1 minute window per IP
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        // Process oldest queued requests first (FIFO)
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        // No queue — excess requests rejected immediately
                        QueueLimit = 0
                    });
            });

        // ── 2. AUTH POLICY — per IP, strict limit for brute force prevention ─
        // Applied to: POST /auth/login, /auth/register, /auth/forgot-password
        // Why per IP: User is not authenticated yet — no User ID available
        // Why 5/min:  Enough for legitimate use, blocks automated attacks
        // Pure rate limiting — no queue, immediate 429 on exceed
        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0; // reject immediately — no throttling
        });

        // ── 3. AI POLICY — per authenticated User ID ─────────────────────────
        // Applied to: ai-suggest, ai-draft-reply, similar tickets, sentiment
        // Why per user: Each user should have independent AI quota
        // Why 10/min:  Controls Claude API costs per user
        // Falls back to IP if user is somehow not authenticated
        options.AddPolicy("ai", httpContext =>
        {
            // Extract User ID from JWT claims — authenticated users only
            var userId = httpContext.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Use userId if authenticated, fall back to IP for safety
            var partitionKey = !string.IsNullOrEmpty(userId)
                ? $"ai:user:{userId}"
                : $"ai:ip:{httpContext.Connection.RemoteIpAddress}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    // 10 AI requests per minute per user
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    // Small queue — slight throttling before rejecting
                    // Allows brief bursts without immediate rejection
                    QueueLimit = 2
                });
        });

        // ── 4. UPLOAD POLICY — per authenticated User ID ─────────────────────
        // Applied to: POST /tickets/{id}/attachments
        // Why per user: Controls Azure Blob Storage costs per user
        // Why 20/min:  Generous enough for legitimate use
        // QueueLimit 5 — throttles before rejecting (better UX for uploads)
        options.AddPolicy("upload", httpContext =>
        {
            var userId = httpContext.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var partitionKey = !string.IsNullOrEmpty(userId)
                ? $"upload:user:{userId}"
                : $"upload:ip:{httpContext.Connection.RemoteIpAddress}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    // 20 uploads per minute per user
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    // Queue up to 5 — throttles uploads before rejecting
                    // Prevents jarring failures on rapid consecutive uploads
                    QueueLimit = 5
                });
        });

        // ── 5. HEAVY POLICY — per authenticated User ID ───────────────────────
        // Applied to: dashboard stats, reports (expensive DB aggregation queries)
        // Why 30/min: These queries are DB intensive — GROUP BY across many rows
        // QueueLimit 3 — slight throttling before rejecting
        options.AddPolicy("heavy", httpContext =>
        {
            var userId = httpContext.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var partitionKey = !string.IsNullOrEmpty(userId)
                ? $"heavy:user:{userId}"
                : $"heavy:ip:{httpContext.Connection.RemoteIpAddress}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    // 30 heavy requests per minute per user
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 3
                });
        });

        // ── 6. REJECTED REQUEST HANDLER ───────────────────────────────────────
        // Called when any rate limit or throttle queue is exceeded.
        // Returns consistent JSON matching your ApiResponse<T> error format.
        // Adds Retry-After header so client knows when to retry.
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            // Add Retry-After header — tells client when the window resets
            if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString();
            }

            // Return consistent error response matching ApiResponse<T> shape
            await context.HttpContext.Response.WriteAsync(
                """
            {
                "success": false,
                "message": "Too many requests. Please slow down and try again shortly.",
                "errors": null,
                "pagination": null
            }
            """,
                cancellationToken);
        };
    });


    // ── BUILD ─────────────────────────────────────────────────
    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SupportDesk Pro API v1");
        c.RoutePrefix = "swagger";
    });

    // ── MIDDLEWARE PIPELINE ORDER IS CRITICAL ─────────────────
    app.UseMiddleware<ExceptionMiddleware>(); // ← FIRST — catches all
    app.UseSerilogRequestLogging(options =>  // ← logs every HTTP request
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} " +
            "responded {StatusCode} in {Elapsed:0.0000}ms";
    });
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();//after authentication and authorization
    app.MapControllers();

    Log.Information(
        "SupportDeskPro API started successfully on {Environment}",
        builder.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    // Catches fatal startup errors
    Log.Fatal(ex, "SupportDeskPro API failed to start");
}
finally
{
    // Flush all pending log entries before shutdown
    Log.CloseAndFlush();
}