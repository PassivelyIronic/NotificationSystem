using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationSystem.Domain.Messages;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PushNotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("push-notifications", e =>
        {
            // Configure consumer with only one concurrent message
            e.PrefetchCount = 1;
            e.ConfigureConsumer<PushNotificationConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();

public class PushNotificationConsumer : IConsumer<SendPushNotificationCommand>
{
    private readonly Random _random = new Random();

    public async Task Consume(ConsumeContext<SendPushNotificationCommand> context)
    {
        Console.WriteLine($"Processing push notification to {context.Message.Recipient}: {context.Message.Content}");

        // Simulate processing time
        await Task.Delay(500);

        // Simulate 50% chance of success
        if (_random.NextDouble() < 0.5)
        {
            Console.WriteLine($"Successfully sent push notification to {context.Message.Recipient}");
        }
        else
        {
            Console.WriteLine($"Failed to send push notification to {context.Message.Recipient}. Retrying...");
            throw new Exception("Push notification delivery failed (50% chance)");
        }
    }
}