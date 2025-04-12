using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RabbitMQ.Client.Events;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

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

public interface IRabbitMqService
{
    Task<string> SendToConsumerAndWaitAsync(string message);
    Task<string> SendToAccountingAndWaitAsync(string cardMessage);
    Task SendToDeliveryAsync(string cardMessage, string message);
}


public class RabbitMqService : IRabbitMqService
{
    private readonly string _uri = "amqps://bhbgbruq:yqO7tzqsBUzty3_4esZnY2y4f8yB3owl@possum.lmq.cloudamqp.com/bhbgbruq";

    public async Task<string> SendToConsumerAndWaitAsync(string message)
    {
        var factory = new ConnectionFactory() { Uri = new Uri(_uri) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Отправка
        await channel.QueueDeclareAsync("MyQueue", false, false, false, null);
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync("", "MyQueue", body: body);

        // Ожидание ответа
        var tcs = new TaskCompletionSource<string>();
        await channel.QueueDeclareAsync("ConsumerReplyQueue", false, false, false, null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var reply = Encoding.UTF8.GetString(ea.Body.ToArray());
            tcs.TrySetResult(reply);
            await Task.Yield();
        };
        await channel.BasicConsumeAsync("ConsumerReplyQueue", true, consumer);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        if (completed != tcs.Task)
        {
            throw new Exception("Нет ответа от Consumer");
        }

        return await tcs.Task;
    }

    public async Task<string> SendToAccountingAndWaitAsync(string cardMessage)
    {
        var factory = new ConnectionFactory() { Uri = new Uri(_uri) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Отправка
        await channel.QueueDeclareAsync("AccountingQueue", false, false, false, null);
        var body = Encoding.UTF8.GetBytes(cardMessage);
        await channel.BasicPublishAsync("", "AccountingQueue", body: body);

        // Ожидание ответа
        var tcs = new TaskCompletionSource<string>();
        await channel.QueueDeclareAsync("AccountingReplyQueue", false, false, false, null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var reply = Encoding.UTF8.GetString(ea.Body.ToArray());
            tcs.TrySetResult(reply);
            await Task.Yield();
        };
        await channel.BasicConsumeAsync("AccountingReplyQueue", true, consumer);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        if (completed != tcs.Task)
        {
            throw new Exception("Нет ответа от Accounting");
        }

        return await tcs.Task;
    }

    public async Task SendToDeliveryAsync(string message, string cardMessage)
    {
        var factory = new ConnectionFactory() { Uri = new Uri(_uri) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("DeliveryQueue", false, false, false, null);
        var payload = new
        {
            Message = message,
            Card = cardMessage     
        };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await channel.BasicPublishAsync("", "DeliveryQueue", body: body);
    }
}
