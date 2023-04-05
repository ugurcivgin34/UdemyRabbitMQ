using RabbitMQ.Client;
using System.Text;

public class Program
{
    private static void Main(string[] args)
    {
        // RabbitMQ bağlantı nesnesi oluşturuluyor
        ConnectionFactory factory = new ConnectionFactory();

        // Bağlanılacak URI ayarlanıyor
        factory.Uri = new Uri("amqps://kullanici_adi:sifre@rabbitmq_adresi/virtual_host");

        // Bağlantı nesnesi oluşturuluyor
        using var connection = factory.CreateConnection();

        // Kanal nesnesi oluşturuluyor
        var channel = connection.CreateModel();

        // "logs-fanout" isimli fanout exchange oluşturuluyor

        // ExchangeDeclare metodu, bir exchange'in oluşturulmasını sağlar. Bu metot aşağıdaki üç parametre alır:
        //exchange: Exchange adı.
        //type: Exchange tipi. Fanout, Direct, Topic ve Headers tiplerinden biri olabilir.
        //durable: Exchange'in kalıcı olup olmayacağını belirler. True değeri verilirse, exchange değişikliklerden etkilenmez ve restartlardan sonra da varlığını sürdürür. False değeri verilirse, exchange sadece bir bağlantı tarafından kullanıldığında var olur ve bir restart sonrasında silinir. Varsayılan değeri false'dur.
        channel.ExchangeDeclare("logs-fanout", durable: true, type: ExchangeType.Fanout);

        // 50 adet mesaj gönderiliyor
        Enumerable.Range(1, 50).ToList().ForEach(x =>
        {
            string message = $"log {x}";

            var messageBody = Encoding.UTF8.GetBytes(message);

            // Mesajlar "logs-fanout" exchange üzerinden gönderiliyor
            // BasicPublish metodu, bir mesajın belirtilen exchange'e gönderilmesini sağlar. Bu metot aşağıdaki dört parametre alır:
            //exchange: Mesajın gönderileceği exchange adı.
            //routingKey: Mesajın yönlendirileceği kuyruk adı.
            //basicProperties: Mesajın özelliklerini içeren nesne. Örneğin mesajın kalıcılığı, öncelik seviyesi gibi özellikler bu nesne ile belirtilir. Bu parametre null olarak da bırakılabilir.
            //body: Mesajın içeriği. byte dizisi olarak verilir.
            channel.BasicPublish("logs-fanout", "", null, messageBody);

            Console.WriteLine($"Mesaj gönderildi: {message}");
        });
    }
}
