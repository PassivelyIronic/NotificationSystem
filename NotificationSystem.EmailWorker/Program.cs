// NotificationSystem.EmailWorker/Program.cs
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationSystem.Domain.Messages;
using NotificationSystem.Domain.Repositories;
using NotificationSystem.Infrastructure.Repositories;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddSingleton<INotificationRepository, MongoNotificationRepository>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailNotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("email-notifications", e =>
        {
            // Configure consumer with only one concurrent message
            e.PrefetchCount = 1;
            e.ConfigureConsumer<EmailNotificationConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();

public class EmailNotificationConsumer : IConsumer<SendEmailNotificationCommand>
{
    private readonly Random _random = new Random();
    private readonly INotificationRepository _repository;

    public EmailNotificationConsumer(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<SendEmailNotificationCommand> context)
    {
        Console.WriteLine($"Processing email notification to {context.Message.Recipient}: {context.Message.Content}");

        var notification = await _repository.GetByIdAsync(context.Message.Id);
        if (notification == null)
        {
            Console.WriteLine($"Notification with ID {context.Message.Id} not found");
            return;
        }

        // Simulate processing time
        await Task.Delay(1000);

        // Simulate 50% chance of success
        if (_random.NextDouble() < 0.5)
        {
            Console.WriteLine($"Successfully sent email notification to {context.Message.Recipient}");
            notification.Status = "Sent";
            await _repository.UpdateAsync(notification);
        }
        else
        {
            Console.WriteLine($"Failed to send email notification to {context.Message.Recipient}. Retrying...");
            notification.AttemptCount++;

            // If this is the third attempt, mark as failed
            if (notification.AttemptCount >= 3)
            {
                notification.Status = "Failed";
                await _repository.UpdateAsync(notification);
                Console.WriteLine($"Email notification to {context.Message.Recipient} failed after 3 attempts.");
            }
            else
            {
                await _repository.UpdateAsync(notification);
                throw new Exception("Email notification delivery failed (50% chance)");
            }
        }
    }
}