using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Domain.Entities;


namespace SupportDeskPro.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly ICurrentTenantService _tenantService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentTenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        // ── DbSets (one per table) ────────────────────────────────
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<SLAPolicy> SLAPolicies => Set<SLAPolicy>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketComment> TicketComments => Set<TicketComment>();
        public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
        public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();
        public DbSet<TicketAssignmentHistory> TicketAssignmentHistories => Set<TicketAssignmentHistory>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<BackgroundJobLog> BackgroundJobLogs => Set<BackgroundJobLog>();
        public DbSet<AIInteractionLog> AIInteractionLogs => Set<AIInteractionLog>();
        public DbSet<TicketNumberSequence> TicketNumberSequences => Set<TicketNumberSequence>();


        // Configures the entity model and relationships for the database.
        // This method is used to customize table mappings, relationships,
        // query filters, and other model configurations.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        { 
        
            base.OnModelCreating(modelBuilder);


            // Automatically discovers and applies all entity configuration classes
            // (IEntityTypeConfiguration<T>) from this assembly. This keeps entity mappings
            // separate from DbContext logic and improves maintainability.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ── GLOBAL QUERY FILTERS ──────────────────────────────
            // These automatically add WHERE TenantId = @current
            // to EVERY query. You never need to filter manually.
            // SuperAdmin bypasses all filters (TenantId is null)

            var tenantId = _tenantService.TenantId;

            modelBuilder.Entity<User>()
                .HasQueryFilter(u =>_tenantService.IsSuperAdmin ||
                                u.TenantId == _tenantService.TenantId &&
                                !u.IsDeleted);

            modelBuilder.Entity<Ticket>()
                .HasQueryFilter(t =>_tenantService.IsSuperAdmin ||
                                t.TenantId == _tenantService.TenantId &&
                                !t.IsDeleted);

            modelBuilder.Entity<TicketComment>()
                .HasQueryFilter(c =>_tenantService.IsSuperAdmin ||
                c.TenantId == _tenantService.TenantId &&
                !c.IsDeleted);

            modelBuilder.Entity<TicketAttachment>()
                .HasQueryFilter(a =>_tenantService.IsSuperAdmin ||
                a.TenantId == _tenantService.TenantId &&
                !a.IsDeleted);

            modelBuilder.Entity<Category>()
                .HasQueryFilter(c => _tenantService.IsSuperAdmin ||
                c.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<SLAPolicy>()
                .HasQueryFilter(s =>_tenantService.IsSuperAdmin ||
                s.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<Notification>()
                .HasQueryFilter(n =>_tenantService.IsSuperAdmin ||
                n.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<TenantSettings>()
                .HasQueryFilter(ts =>_tenantService.IsSuperAdmin ||
                ts.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<TicketNumberSequence>(b =>
            {
                b.ToTable("TicketNumberSequences");//This forces explicit table name (no pluralization magic).
                b.HasKey(t => t.TenantId);
            });
            
            modelBuilder.Entity<AIInteractionLog>(b =>
            {
                b.ToTable("AIInteractionLogs");
                b.HasKey(a => a.Id);
                b.Property(a => a.EstimatedCostUSD)
                    .HasPrecision(10, 6);   // up to $9999.999999
            });

            modelBuilder.Entity<RefreshToken>()
                .HasQueryFilter(r => _tenantService.IsSuperAdmin ||
                r.User.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<PasswordResetToken>()
                .HasQueryFilter(r =>_tenantService.IsSuperAdmin ||
                r.User.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<TicketStatusHistory>()
                .HasQueryFilter(t => _tenantService.IsSuperAdmin ||
                t.TenantId == _tenantService.TenantId);

            modelBuilder.Entity<TicketAssignmentHistory>()
                .HasQueryFilter(t => _tenantService.IsSuperAdmin ||
                t.TenantId == _tenantService.TenantId);
        }

        // ── AUTO UPDATE TIMESTAMPS ────────────────────────────────
        // Automatically sets UpdatedAt before every save
        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            // Get current logged in userId from JWT
            var currentUserId = _tenantService.CurrentUserId;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.CreatedBy = currentUserId; 
                        entry.Entity.UpdatedBy = currentUserId; 
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
