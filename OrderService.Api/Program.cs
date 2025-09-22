using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
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

// Log automatic 400s (model validation failures) with the exact fields/messages
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ModelValidation");

        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors?.Count > 0)
            .Select(kvp => new
            {
                Field = kvp.Key,
                Messages = kvp.Value!.Errors.Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? e.Exception?.Message
                        : e.ErrorMessage)
            });

        var path = context.HttpContext.Request.Path.ToString();
        var method = context.HttpContext.Request.Method;
        var user = context.HttpContext.User?.Identity?.Name ?? "anonymous";

        logger.LogWarning(
            "VALIDATION FAIL | {Method} {Path} | User={User} | Errors={ErrorsJson}",
            method, path, user, JsonSerializer.Serialize(errors)
        );

        return new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
    };
});

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

//search
builder.Services.AddScoped<IItemSearchRepository, ItemSearchRepository>();
builder.Services.AddScoped<IItemSearchService, ItemSearchService>();


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

// ---------------- Inventory services ----------------
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

var app = builder.Build();

// ---------------- Pipeline (order matters) ----------------
// 1) OpenAPI JSON & UI
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "OrderService API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "OrderService API";
});

// 2) HTTPS, routing, CORS
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");

// 3) Authenticate so HttpContext.User is available to later middleware
app.UseAuthentication();

// 4) Correlation/User enrichment for non-swagger requests
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/swagger"), branch =>
{
    branch.UseMiddleware<OrderService.Api.Middleware.CorrelationIdMiddleware>();
});

// 5) Request logging AFTER correlation so logs contain CorrelationId/User
app.UseSerilogRequestLogging();

// 6) Authorization
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
