using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Infrastructure.Persistence;
using SupportDeskPro.Infrastructure.Services;

namespace SupportDeskPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DATABASE ──────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(
                    typeof(ApplicationDbContext).Assembly.FullName)));

        // ── HTTP CONTEXT (needed for tenant resolution) ───────
        services.AddHttpContextAccessor();

        // ── TENANT SERVICE ────────────────────────────────────
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        return services;
    }
}