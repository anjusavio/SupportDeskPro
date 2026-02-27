using SupportDeskPro.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── ADD SERVICES ──────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure (DbContext, services)
builder.Services.AddInfrastructure(builder.Configuration);

// ── BUILD APP ─────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();