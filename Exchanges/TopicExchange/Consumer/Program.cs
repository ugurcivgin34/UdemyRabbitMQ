using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            // RabbitMQ bağlantı nesnesi oluşturuluyor
            var factory = new ConnectionFactory();
            factory.Uri = new Uri("amqps://kullanici_adi:sifre@rabbitmq_adresi/virtual_host");

            // Bağlantı nesnesi oluşturuluyor
            using var connection = factory.CreateConnection();

            // Kanal nesnesi oluşturuluyor
            var channel = connection.CreateModel();

            // Kanalın 1 mesajı işlemesi için ayar yapılıyor
            channel.BasicQos(0, 1, false);

            // Consumer nesnesi oluşturuluyor
            var consumer = new EventingBasicConsumer(channel);

            // Rastgele bir kuyruk adı oluşturuluyor
            var queueName = channel.QueueDeclare().QueueName;

            // "logs-topic" isimli topic exchange'e, Info.# route key'iyle bağlanıyor
            channel.QueueBind(queueName, "logs-topic", "Info.#");

            Console.WriteLine("Logları dinleniyor...");

            // Mesajların alınması sağlanıyor
            consumer.Received += (sender, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());

                Thread.Sleep(1500);
                Console.WriteLine("Gelen Mesaj: " + message);

                // Mesajın işlendiği doğrulanıyor
                channel.BasicAck(e.DeliveryTag, false);
            };

            channel.BasicConsume(queueName, false, consumer);

            Console.ReadLine();
        }
    }
}
