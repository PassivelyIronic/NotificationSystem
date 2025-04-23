using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationSystem.API.Services;
using NotificationSystem.Domain.Repositories;
using NotificationSystem.Infrastructure.Consumers;
using NotificationSystem.Infrastructure.Repositories;
using NotificationSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddScoped<NotificationService>();
//builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddHostedService<NotificationProcessingService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendNotificationConsumer>();

    x.AddMessageScheduler(new Uri("rabbitmq://rabbitmq/quartz"));

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.UseMessageScheduler(new Uri("rabbitmq://rabbitmq/quartz"));

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();