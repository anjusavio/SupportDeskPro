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