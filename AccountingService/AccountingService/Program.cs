using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();

var factory = new ConnectionFactory() 
{ 
    Uri = new Uri("amqps://bhbgbruq:yqO7tzqsBUzty3_4esZnY2y4f8yB3owl@possum.lmq.cloudamqp.com/bhbgbruq")
};

using (var connection = await factory.CreateConnectionAsync())
using (var channel = await connection.CreateChannelAsync())
{
    await channel.QueueDeclareAsync(queue: "AccountingQueue",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"Получено сообщение в Accounting: {message}");
        await SendReplyAsync(message);
        await Task.Yield();
    };
    await channel.BasicConsumeAsync(queue: "AccountingQueue",
                     autoAck: true,
                     consumerTag: " ",
                     noLocal: false,
                     exclusive: false,
                     arguments: null,
                 consumer: (IAsyncBasicConsumer)consumer);
    Console.WriteLine(" [Accounting] ожидание оплаты.");
    Console.ReadLine();
}
static async Task SendReplyAsync(string message)
{
    var replyFactory = new ConnectionFactory() { Uri = new Uri("amqps://bhbgbruq:yqO7tzqsBUzty3_4esZnY2y4f8yB3owl@possum.lmq.cloudamqp.com/bhbgbruq") };

    var replyConn = await replyFactory.CreateConnectionAsync();
    try
    {
        var replyChannel = await replyConn.CreateChannelAsync();
        try
        {
            await replyChannel.QueueDeclareAsync("AccountingReplyQueue", false, false, false, null);

            string replyMessage;
            if (message.Contains("1234"))
            {
                replyMessage = "Карта подтверждена";
            }
            else
            {
                replyMessage = "Карта отклонена";
            }

            var bodyReply = Encoding.UTF8.GetBytes(replyMessage);

            await replyChannel.BasicPublishAsync("", "AccountingReplyQueue", body: bodyReply);

            Console.WriteLine($"Ответ отправлен в AccountingReplyQueue: {replyMessage}");
        }
        finally
        {
            replyChannel?.Dispose();
        }
    }
    finally
    {
        replyConn?.Dispose();
    }
}