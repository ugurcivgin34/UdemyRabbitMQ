using RabbitMQ.Client;
using System.Text;

namespace Publisher
{
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
            // RabbitMQ bağlantı nesnesi oluşturuluyor
            var factory = new ConnectionFactory();
            factory.Uri = new Uri("amqps://kullanici_adi:sifre@rabbitmq_adresi/virtual_host");

            // Bağlantı nesnesi oluşturuluyor
            using var connection = factory.CreateConnection();

            // Kanal nesnesi oluşturuluyor
            var channel = connection.CreateModel();

            // "header-exchange" isimli header exchange oluşturuluyor
            channel.ExchangeDeclare("header-exchange", durable: true, type: ExchangeType.Headers);

            // Header bilgileri dictionary nesnesiyle tanımlanıyor
            Dictionary<string, object> headers = new Dictionary<string, object>
            {
                { "format", "pdf" },
                { "shape2", "a4" }
            };

            // BasicProperties nesnesi oluşturuluyor
            var properties = channel.CreateBasicProperties();
            properties.Headers = headers;

            //properties.Persistent özelliği, bir mesajın kalıcı olup olmayacağını belirleyen bir özelliktir.Eğer bu özellik true olarak ayarlanırsa, mesaj kalıcı olarak işaretlenir ve RabbitMQ, mesajı disk üzerinde saklar, böylece bir sunucu yeniden başlatıldığında veya bir bağlantı kaybedildiğinde bile mesajlar korunur.Eğer bu özellik false olarak ayarlanırsa, mesaj geçici olarak işaretlenir ve yalnızca bir sunucu tarafından kullanıldığında var olur.Varsayılan olarak, Persistent özelliği false olarak ayarlanmıştır.
            //properties.Persistent = true; 


            // Mesaj gönderiliyor
            channel.BasicPublish("header-exchange", string.Empty, properties, Encoding.UTF8.GetBytes("header mesajım"));

            Console.WriteLine("Mesaj gönderilmiştir.");

            Console.ReadLine();
        }
    }
}