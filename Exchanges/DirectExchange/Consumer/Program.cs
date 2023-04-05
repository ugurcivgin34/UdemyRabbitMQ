using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;


//Bu kod, RabbitMQ'dan belirtilen bir kuyrukta bekleyen mesajları dinleyen bir C# uygulamasıdır. Uygulama, bağlantı nesnesi, kanal nesnesi, consumer nesnesi ve kuyruk adı oluşturur. Daha sonra, BasicQos metodu kullanılarak mesajların adil şekilde dağıtılması sağlanır.

//Consumer nesnesi, Received event'i kullanılarak mesajların işlenmesini sağlar. Gelen mesajların içeriği byte[] formatında olduğu için, mesajın içeriği Encoding.UTF8.GetString metoduyla string'e dönüştürülür. Ardından, mesajın işlenmesi için 150 ms beklenir ve mesajın içeriği Console.WriteLine ile ekrana yazdırılır. Son olarak, mesajın başarılı bir şekilde işlendiği doğrulanır.

//Son olarak, BasicConsume metodu kullanılarak, mesajların dinleneceği kuyruk başlatılır ve konsol ekranında herhangi bir tuşa basılana kadar uygulama çalış
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

        // BasicQos, mesajların adil şekilde dağıtılmasını sağlayan bir ayarlamadır.
        channel.BasicQos(0, 1, false);

        // Mesajları alacak consumer nesnesi oluşturuluyor
        var consumer = new EventingBasicConsumer(channel);

        // Mesajları alınacak kuyruk belirleniyor
        var queueName = "kuyruk-adi";

        // Mesajların alınmaya başlandığı bilgisi ekrana yazdırılıyor
        Console.WriteLine("Mesajlar dinleniyor...");

        // Received event'i kullanılarak gelen mesajların işlenmesi sağlanıyor
        consumer.Received += (sender, e) =>
        {
            // Gelen mesajın byte[] formatındaki içeriği string'e çevriliyor
            var message = Encoding.UTF8.GetString(e.Body.ToArray());

            // Mesajın işlenmesi için 150 ms bekleniyor (Simüle ediliyor)
            Thread.Sleep(150);

            // Mesaj içeriği ekrana yazdırılıyor
            Console.WriteLine("Gelen Mesaj: " + message);

            // Mesajın başarılı bir şekilde işlendiği doğrulanıyor
            channel.BasicAck(e.DeliveryTag, false);
        };

        // Mesajların dinlendiği kuyruk başlatılıyor
        channel.BasicConsume(queueName, false, consumer);

        // Konsol ekranında herhangi bir tuşa basılana kadar uygulama çalışır
        Console.ReadLine();

    }
}
