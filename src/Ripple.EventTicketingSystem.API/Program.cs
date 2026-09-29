using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Options;
using Ripple.EventTicketingSystem.Application.Services;
using Ripple.EventTicketingSystem.Infrastructure.Data;
using Ripple.EventTicketingSystem.Infrastructure.Repositories;
using RippleEventTracking.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Entity Framework Core / SQL Server
builder.Services.AddDbContext<TicketingDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("RippleEventTracking"));
});
builder.Services.Configure<TicketingOptions>(
    builder.Configuration.GetSection("Ticketing"));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IPricingTierRepository, PricingTierRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
