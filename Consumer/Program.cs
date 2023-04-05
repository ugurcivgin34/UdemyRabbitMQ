using RabbitMQ.Client.Events;
using RabbitMQ.Client;
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

            // "header-exchange" isimli header exchange oluşturuluyor
            channel.ExchangeDeclare("header-exchange", durable: true, type: ExchangeType.Headers);

            // Mesajların adil şekilde dağıtılması sağlanıyor
            channel.BasicQos(0, 1, false);

            // EventingBasicConsumer nesnesi oluşturuluyor
            var consumer = new EventingBasicConsumer(channel);

            // channel.QueueDeclare metodu kullanılarak random bir kuyruk adı oluşturuluyor
            var queueName = channel.QueueDeclare().QueueName;

            // Bir header tanımlanıyor
            Dictionary<string, object> headers = new Dictionary<string, object>
            {
                { "format", "pdf" },
                { "shape", "a4" },
                { "x-match", "any" }
            };

            // Header exchange ile kuyruk arasında bağlantı kuruluyor
            channel.QueueBind(queueName, "header-exchange", String.Empty, headers);


            Console.WriteLine("Logları dinleniyor...");

            consumer.Received += (object sender, BasicDeliverEventArgs e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());

                Thread.Sleep(1500);
                Console.WriteLine("Gelen Mesaj:" + message);

                channel.BasicAck(e.DeliveryTag, false);
            };
            channel.BasicConsume(queueName, false, consumer);

            Console.ReadLine();
        }
    }
}