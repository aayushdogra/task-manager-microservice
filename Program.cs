using TaskManager.Data;
using TaskManager.Endpoints;
using TaskManager.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("TasksDb")
                       ?? "Host=localhost;Port=5432;Database=tasks_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<TasksDbContext>(options => options.UseNpgsql(connectionString));

// Register DbTaskService as a service so [FromServices] can resolve it
builder.Services.AddScoped<ITaskService, DbTaskService>();

var app = builder.Build();

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