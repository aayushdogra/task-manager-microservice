using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManager.Data;
using TaskManager.Endpoints;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Read connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("TasksDb")
                       ?? throw new InvalidOperationException("Connection string 'TasksDb' not found.");

builder.Services.AddDbContext<TasksDbContext>(options => options.UseNpgsql(connectionString));

// Register Task Service
builder.Services.AddScoped<ITaskService, DbTaskService>();

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

// Global Exception Handler Middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        Log.Error(exception, "Unhandled exception occurred");

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred.",
            details = exception?.Message // optional: remove in production
        });
    });
});

// Serilog request logging
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map endpoints
app.MapHealthEndpoints();
app.MapTaskEndpoints();

app.Run();