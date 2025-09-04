using Microsoft.EntityFrameworkCore;
using OrderService.Persistence;
using OrderService.Business;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // reads from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/orderservice.log", rollingInterval: RollingInterval.Day)

    .CreateLogger();

builder.Host.UseSerilog();

// CORS
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF + Dapper + Repos + UoW
var connStr = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr, npg => npg.MigrationsHistoryTable("__efmigrationshistory", "order")));
builder.Services.AddSingleton<IConnectionFactory>(_ => new NpgsqlConnectionFactory(connStr));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business service
builder.Services.AddScoped<IOrdersService, OrdersService>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<OrderService.Api.Middleware.CorrelationIdMiddleware>();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Quick DB health
app.MapGet("/health/db", async (AppDbContext db) => Results.Ok(new { canConnect = await db.Database.CanConnectAsync() }));

try
{
    Log.Information("Test log is working");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
