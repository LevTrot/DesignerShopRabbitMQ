using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory() { Uri = new Uri("amqps://bhbgbruq:yqO7tzqsBUzty3_4esZnY2y4f8yB3owl@possum.lmq.cloudamqp.com/bhbgbruq") };

            using (var connection = await factory.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(queue: "MyQueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Получено сообщение в Consumer: {message}");
                    await SendReplyAsync();
                    await Task.Yield();
                };
                await channel.BasicConsumeAsync(queue: "MyQueue",
                                 autoAck: true,
                                 consumerTag: " ",
                                 noLocal: false,
                                 exclusive: false,
                                 arguments: null,
                             consumer: (IAsyncBasicConsumer)consumer);
                Console.WriteLine(" [Consumer] Ожидание сообщений.");
                Console.ReadLine();
            }
        }
        public static async Task SendReplyAsync()
        {
            {
                var replyFactory = new ConnectionFactory() { Uri = new Uri("amqps://bhbgbruq:yqO7tzqsBUzty3_4esZnY2y4f8yB3owl@possum.lmq.cloudamqp.com/bhbgbruq") };

                var replyConn = await replyFactory.CreateConnectionAsync();
                try
                {
                    var replyChannel = await replyConn.CreateChannelAsync();
                    try
                    {
                        await replyChannel.QueueDeclareAsync("ConsumerReplyQueue", false, false, false, null);

                        var confirmMessage = "Сообщение успешно обработано";
                        var bodyReply = Encoding.UTF8.GetBytes(confirmMessage);

                        await replyChannel.BasicPublishAsync("", "ConsumerReplyQueue", body: bodyReply);

                        Console.WriteLine("Ответ отправлен в ConsumerReplyQueue");
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
        }
    }
}