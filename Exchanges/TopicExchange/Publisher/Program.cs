using RabbitMQ.Client;
using System.Text;

namespace Publisher
{
    // LogNames adında bir enum tanımlanmıştır.
    public enum LogNames
    {
        Critical = 1,
        Error = 2,
        Warning = 3,
        Info = 4
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Bağlantı oluşturmak için ConnectionFactory sınıfı kullanılmıştır.
            var factory = new ConnectionFactory();
            factory.Uri = new Uri("amqps://uidvbmmg:xYlxfwU1dk6FwGtwxNj8jm1qWjdlwj9U@shark.rmq.cloudamqp.com/uidvbmmg");

            // Bağlantı üzerinden bir kanal oluşturulmuştur.
            using var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            // Bir topic exhange tanımlanmıştır.
            channel.ExchangeDeclare("logs-topic", durable: true, type: ExchangeType.Topic);

            // Rastgele sayılar oluşturulup, bu sayılara karşılık gelen loglar seçilmiştir.
            Random rnd = new Random();
            Enumerable.Range(1, 50).ToList().ForEach(x =>
            {
                LogNames log1 = (LogNames)rnd.Next(1, 5);
                LogNames log2 = (LogNames)rnd.Next(1, 5);
                LogNames log3 = (LogNames)rnd.Next(1, 5);

                // Seçilen loglardan bir routeKey oluşturulmuştur.
                var routeKey = $"{log1}.{log2}.{log3}";

                // Mesaj içeriği hazırlanmıştır.
                string message = $"log-type: {log1}-{log2}-{log3}";
                var messageBody = Encoding.UTF8.GetBytes(message);

                // Mesaj, BasicPublish() metodu kullanılarak gönderilmiştir.
                channel.BasicPublish("logs-topic", routeKey, null, messageBody);

                // Gönderilen her mesaj için bir log gönderildiği ekrana yazdırılmıştır.
                Console.WriteLine($"Log gönderilmiştir : {message}");
            });

            // Console.ReadLine() kullanılarak programın sonlanması beklenmiştir.
            Console.ReadLine();
        }
    }
}
