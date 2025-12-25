using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using TaskManager.Data;
using TaskManager.Dto;
using TaskManager.Endpoints;
using TaskManager.Helpers;
using TaskManager.Middleware;
using TaskManager.RateLimiting;
using TaskManager.Services;
using TaskManager.Validators;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

//builder.WebHost.UseUrls("http://localhost:5156", "https://localhost:7156");

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

    return ConnectionMultiplexer.Connect(new ConfigurationOptions
    {
        EndPoints = { redisConnectionString },
        AbortOnConnectFail = false
    });
});

builder.Services.AddHttpContextAccessor();

// Cache abstraction (Redis behind interface)
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

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
            var exception = context.Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

            var statusCode = StatusCodes.Status500InternalServerError;
            var errorCode = "INTERNAL_SERVER_ERROR";
            var errorMessage = "An unexpected error occurred.";

            if (exception is UnauthorizedAccessException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                errorCode = "UNAUTHORIZED";
                errorMessage = "Unauthorized.";
            }
            else if (exception is ArgumentException or InvalidOperationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorCode = "BAD_REQUEST";
                errorMessage = exception.Message;
            }

            Log.Error(exception, "Unhandled exception");

            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(new 
                ApiResponse<object>(
                false, 
                null, 
                new ApiError(errorCode, errorMessage, exception?.Message))
            );
        });
    });


    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
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

    // API v1 group
    var v1 = app.MapGroup("/api/v1");

    // Map endpoints: Versioned endpoints
    v1.MapHealthEndpoints();
    v1.MapAuthEndpoints();
    v1.MapTaskEndpoints();

    app.MapFallback(() => ApiResults.NotFound("ROUTE_NOT_FOUND", "The requested endpoint does not exist."));

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