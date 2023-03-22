// See https://aka.ms/new-console-template for more information
using RabbitMQ.Client;
using System.Text;

namespace Publisher
{
    public class Program
    {
        private static void Main(string[] args)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqps://yjvxzyim:8SIohLh_WS7SCqCKNVJr66-BY1gQgrlh@woodpecker.rmq.cloudamqp.com/yjvxzyim");

            using var connection = factory.CreateConnection();

            var channel = connection.CreateModel();

            channel.ExchangeDeclare("logs-fanout",durable:true,type:ExchangeType.Fanout);

            Enumerable.Range(1, 50).ToList().ForEach(x =>
            {
                string message = $"log {x}";

                var messageBody = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("logs-fanout", "", null, messageBody);

                Console.WriteLine($"Mesaj gönderilmiştir: {message}");

            });

            Console.ReadLine();
        }
    }
}