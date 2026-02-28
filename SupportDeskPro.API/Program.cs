using Microsoft.OpenApi.Models;
using SupportDeskPro.Application;
using SupportDeskPro.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// ── ADD SERVICES ──────────────────────────────────────────────
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

// Application (MediatR, FluentValidation)
builder.Services.AddApplication();

// Infrastructure (DbContext, JWT, services)
builder.Services.AddInfrastructure(builder.Configuration);

// ── BUILD APP ─────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();    // ← must be before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();