using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.Api.Auth;
using OrderService.Business;
using OrderService.Business.Inventory;
using OrderService.Business.Services;
using OrderService.Persistence;
using OrderService.Persistence.Repositories;
using Serilog;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Serilog ----------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/orderservice.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------- CORS ----------------
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ---------------- MVC + Swagger ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderService API",
        Version = "v1",
        Description = "API for Orders, Authentication (JWT), and Inventory"
    });

    // JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------- EF + Persistence ----------------
var connStr = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr, npg => npg.MigrationsHistoryTable("__efmigrationshistory", "order")));

builder.Services.AddSingleton<IConnectionFactory>(_ => new NpgsqlConnectionFactory(connStr));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---------------- Business services ----------------
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ---------------- JWT Auth ----------------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtTokenFactory>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    options.AddPolicy("RequireManagerOrAdmin", p => p.RequireRole("Manager", "Admin"));
});
// Inventory (Week 2)
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

var app = builder.Build();

// ---------------- Pipeline (order matters) ----------------
app.UseSerilogRequestLogging();

// 1) Expose the OpenAPI JSON first (runtime-generated)
app.UseSwagger(c =>
{
    // standard route: /swagger/{documentName}/swagger.json -> /swagger/v1/swagger.json
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});

// 2) Serve the Swagger UI at /swagger
app.UseSwaggerUI(c =>
{
    // IMPORTANT: relative path (no leading slash)
    c.SwaggerEndpoint("v1/swagger.json", "OrderService API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "OrderService API";
});

// 3) Skip custom middleware for swagger requests (avoid interfering with JSON/UI)
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/swagger"), branch =>
{
    branch.UseMiddleware<OrderService.Api.Middleware.CorrelationIdMiddleware>();
});

// Root -> Swagger UI
//app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");      // after routing, before auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health
app.MapGet("/health/db", async (AppDbContext db) =>
    Results.Ok(new { canConnect = await db.Database.CanConnectAsync() }));

try
{
    Log.Information("OrderService.Api starting up");
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
