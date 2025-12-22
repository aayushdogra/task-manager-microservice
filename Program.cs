using TaskManager.Data;
using TaskManager.Endpoints;
using TaskManager.Services;
using TaskManager.Validators;
using TaskManager.Middleware;
using TaskManager.RateLimiting;
using Serilog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.WebHost.UseUrls("http://localhost:5156", "https://localhost:7156");

// Add services to the container.
builder.Services.AddOpenApi();

// Read connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("TasksDb")
                       ?? throw new InvalidOperationException("Connection string 'TasksDb' not found.");

builder.Services.AddDbContext<TasksDbContext>(options => options.UseNpgsql(connectionString));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnectionString = builder.Configuration
        .GetSection("Redis")
        .GetValue<string>("ConnectionString");

    if (string.IsNullOrWhiteSpace(redisConnectionString))
        throw new InvalidOperationException("Redis connection string is not configured.");

    return ConnectionMultiplexer.Connect(redisConnectionString);
});

builder.Services.AddHttpContextAccessor();

// Register Task Service
builder.Services.AddScoped<ITaskService, DbTaskService>();
builder.Services.AddScoped<DbTaskService>(); // Required for debug endpoint

builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt["Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

// Auth services
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Rate Limiting
builder.Services.AddSingleton<InMemoryRateLimitStore>();

builder.Services.Configure<RateLimitOptions>(options =>
{
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(10);
});

builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RateLimitOptions>>().Value);

try
{
    var app = builder.Build();

    // Global Exception Handler Middleware
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

            if (exception is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Unknown error" });
                return;
            }

            var statusCode = StatusCodes.Status500InternalServerError;
            var errorMessage = "An unexpected error occurred.";

            switch (exception)
            {
                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    errorMessage = "Unauthorized.";
                    break;

                case InvalidOperationException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorMessage = exception.Message;
                    break;

                case ArgumentException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorMessage = exception.Message;
                    break;
            }

            Log.Error(exception, $"Unhandled exception occurred during request execution. StatusCode: {statusCode}");

            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                details = exception?.Message
            });
        });
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Serilog request logging
    app.UseSerilogRequestLogging();

    // Rate Limiting Middleware
    app.UseMiddleware<RateLimitingMiddleware>();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapHealthEndpoints();
    app.MapAuthEndpoints();
    app.MapTaskEndpoints();

    app.MapFallback(() =>
        Results.NotFound(new
        {
            error = "Route not found"
        })
    );

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = app.Urls?.Any() == true ? string.Join(", ", app.Urls) : "(none)";
        Log.Information("Application started. Environment: {Env}; Listening on: {Urls}",
            app.Environment.EnvironmentName,
            urls);
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}