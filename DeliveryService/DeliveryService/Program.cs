using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DeliveryMemoryStorage>();

var app = builder.Build();

//// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

_ = Task.Run(async () =>
{
    var factory = new ConnectionFactory()
    {
        Uri = new Uri("amqps://bhbgbruq:yqO7tzqsBUzty3_4esZnY2y4f8yB3owl@possum.lmq.cloudamqp.com/bhbgbruq")
    };

    var connection = await factory.CreateConnectionAsync();
    var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync("DeliveryQueue", false, false, false, null);

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);
        var data = JsonSerializer.Deserialize<DeliveryMessage>(json);

        Console.WriteLine("Получено в доставку");
        Console.WriteLine($"Сообщние об оплате: {data?.Message}");
        Console.WriteLine($"Оплачено картой: {data?.Card}");
        Console.WriteLine("Начата доставка товаров.");

        var memory = app.Services.GetRequiredService<DeliveryMemoryStorage>();
        memory.LastMessage = data;
        memory.ReceivedAt = DateTime.UtcNow;

        await Task.Yield();
    };

    await channel.BasicConsumeAsync(
        queue: "DeliveryQueue",
        autoAck: true,
        consumer: consumer);
});

app.Run();

public record DeliveryMessage(string Message, string Card);

public class DeliveryMemoryStorage
{
    public DeliveryMessage? LastMessage { get; set; }
    public DateTime? ReceivedAt { get; set; }
}


