using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TransactionRetrySystem.API.Common;
using TransactionRetrySystem.Application.Common;
using TransactionRetrySystem.Application.Services.TransactionConsumer;
using TransactionRetrySystem.Infrastructure.Common;
using TransactionRetrySystem.Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/transaction.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

//Add Mapster
builder.Services.RegisterMapper();

//Add RabbitMQ + Mass Transit
builder.Services.AddMassTransit(x =>
{
    x.AddDelayedMessageScheduler();

    x.AddConsumer<RetryTransactionConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");

        cfg.ReceiveEndpoint("transaction-retry-queue", e =>
        {
            e.ConfigureConsumer<RetryTransactionConsumer>(context);
            e.UseMessageRetry(r => r.Immediate(0));
        });
    });
});


// Add Redis Cache
builder.Services.AddDistributedMemoryCache();

// Register Services in DI Container
builder.Services.RegisterServices(builder.Environment, builder.Configuration);

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Database Seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var env = services.GetRequiredService<IHostEnvironment>();
    var config = services.GetRequiredService<IConfiguration>();

    await DbSeeder.Initialize(context, env, config);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();