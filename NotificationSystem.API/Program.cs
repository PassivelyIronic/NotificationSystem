using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationSystem.Domain.Repositories;
using NotificationSystem.Infrastructure.Consumers;
using NotificationSystem.Infrastructure.Repositories;
using NotificationSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDB");
    return new MongoClient(connectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDB:DatabaseName"];
    return client.GetDatabase(databaseName);
});

// Register repositories and services
builder.Services.AddSingleton<INotificationRepository, MongoNotificationRepository>();
builder.Services.AddScoped<NotificationService>();
//builder.Services.AddScoped<INotificationService, NotificationService>();


// Configure MassTransit with RabbitMQ
// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendNotificationConsumer>();

    // Add this line to register the message scheduler
    x.AddMessageScheduler(new Uri("rabbitmq://rabbitmq/quartz"));

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        // Configure the message scheduler endpoint
        cfg.UseMessageScheduler(new Uri("rabbitmq://rabbitmq/quartz"));

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();