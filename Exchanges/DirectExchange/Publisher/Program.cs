using RabbitMQ.Client;
using System.Text;

public enum LogNames
{
    Critical = 1,
    Error = 2,
    Warning = 3,
    Info = 4
}


//Aşağıdaki C# kodu, RabbitMQ'ya log mesajları gönderen bir uygulamayı temsil eder. Kod, bir ConnectionFactory örneği oluşturur ve AMQP URI'sini ayarlar. Daha sonra, bir bağlantı ve bir kanal oluşturulur. channel.ExchangeDeclare metodu kullanılarak, "logs-direct" isimli bir exchange oluşturulur.

//Kod, LogNames enum'una ait tüm isimleri alır ve her biri için bir kuyruk adı ve route key oluşturur. Daha sonra, kuyruklar ve exchange arasında bağlantılar kurulur.

//Enumerable.Range(1, 50).ToList().ForEach metodu kullanılarak 50 adet log mesajı gönderilir. Log mesajları, bir route key ile birlikte "logs-direct" exchange'ine gönderilir.
public class Program
{
    private static void Main(string[] args)
    {
        // RabbitMQ bağlantı nesnesi oluşturuluyor
        var factory = new ConnectionFactory();

        // Bağlanılacak URI ayarlanıyor
        factory.Uri = new Uri("amqps://kullanici_adi:sifre@rabbitmq_adresi/virtual_host");

        // Bağlantı nesnesi oluşturuluyor
        using var connection = factory.CreateConnection();

        // Kanal nesnesi oluşturuluyor
        var channel = connection.CreateModel();

        // "logs-direct" isimli exchange oluşturuluyor
        channel.ExchangeDeclare("logs-direct", durable: true, type: ExchangeType.Direct);

        // LogNames enum'una ait tüm isimler alınarak, her biri için bir kuyruk adı ve route key oluşturuluyor
        Enum.GetNames(typeof(LogNames)).ToList().ForEach(x =>
        {
            var routeKey = $"route-{x}";
            var queueName = $"direct-queue-{x}";

            // Kuyruklar oluşturuluyor
            channel.QueueDeclare(queueName, true, false, false);

            // Exchange ve kuyruklar arasında bağlantılar kuruluyor
            channel.QueueBind(queueName, "logs-direct", routeKey, null);
        });

        // 50 adet log mesajı gönderiliyor
        Enumerable.Range(1, 50).ToList().ForEach(x =>
        {
            // Rastgele bir LogNames değeri seçiliyor
            LogNames log = (LogNames)new Random().Next(1, 5);

            // Log mesajı oluşturuluyor
            string message = $"log-type: {log}";

            var messageBody = Encoding.UTF8.GetBytes(message);

            var routeKey = $"route-{log}";

            // Log mesajı, route key ile birlikte "logs-direct" exchange'ine gönderiliyor
            channel.BasicPublish("logs-direct", routeKey, null, messageBody);

            Console.WriteLine($"Log gönderildi: {message}");
        });

        Console.ReadLine();
    }
}
