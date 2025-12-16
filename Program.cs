using TaskManager.Data;
using TaskManager.Endpoints;
using TaskManager.Services;
using TaskManager.Validators;
using Serilog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Register Task Service
builder.Services.AddScoped<ITaskService, DbTaskService>();
builder.Services.AddScoped<DbTaskService>(); // Required for debug endpoint

builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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

try
{
    var app = builder.Build();

    // Global Exception Handler Middleware
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

            Log.Error(exception, "Unhandled exception occurred during request execution");

            context.Response.StatusCode = 500;
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

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapHealthEndpoints();
    app.MapAuthEndpoints();
    app.MapTaskEndpoints();

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