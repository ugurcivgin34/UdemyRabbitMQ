using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Consumer
{
    //    Bu C# kodu, RabbitMQ'dan mesajları dinleyen bir consumer uygulamasını temsil eder. Kod, bir ConnectionFactory örneği oluşturur ve AMQP URI'sini ayarlar. Daha sonra, bir bağlantı ve bir kanal oluşturulur.

    //Kod, channel.QueueDeclare metodu kullanılarak bir kuyruk adı oluşturur.Ancak bu kuyruk adı, randomQueueName değişkeniyle tutulur.Bu, her seferinde farklı bir kuyruk adı alınmasını sağlar.

    //Kod, channel.QueueBind metodu kullanılarak bir exchange ve kuyruk arasında bağlantı kurar. Bu durumda, "logs-fanout" isimli bir exchange kullanılır ve herhangi bir route key belirtilmez.

    //Kod, BasicQos metodu kullanılarak mesajların adil şekilde dağıtılmasını sağlar. Daha sonra, bir EventingBasicConsumer örneği oluşturulur ve channel.BasicConsume metodu kullanılarak mesajların dinleneceği kuyruk belirtilir.

    //consumer.Received event'i kullanılarak, gelen mesajların işlenmesi sağlanır. Gelen mesajın içeriği byte[] formatında olduğu için, mesajın içeriği Encoding.UTF8.GetString metoduyla string'e dönüştürülür. Mesajın işlenmesi tamamlandığında BasicAck metodu kullanılarak mesajın işlendiği doğrulanır.
    public class Program
    {
        private static void Main(string[] args)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqps://uidvbmmg:xYlxfwU1dk6FwGtwxNj8jm1qWjdlwj9U@shark.rmq.cloudamqp.com/uidvbmmg");

            using var connection = factory.CreateConnection();

            var channel = connection.CreateModel();

            // channel.QueueDeclare metodu kullanılarak random bir kuyruk adı oluşturuluyor
            var randomQueueName = channel.QueueDeclare().QueueName;

            //var randomQueueName = "log-database-save-queue";
            //channel.QueueDeclare(randomQueueName, true, false, false);//exchande consumer kuyruk oluşturduktan sonra kalıcı kuyruk olmasını istiyorsak QueueDeclare yapabiliriz.Consumer her down olduğunda kaldığı yerden devam edebilir

            //Bu şekilde ilgili exchange e bağlı olarka kuyruk oluşur ve mesja alınır,consumer down olunca kuyrukta silinmiş olur
            // "logs-fanout" isimli exchange ile kuyruk arasında bağlantı kuruluyor
            channel.QueueBind(randomQueueName, "logs-fanout", "", null);

            channel.BasicQos(0, 1, true);

            var consumer = new EventingBasicConsumer(channel);


            channel.BasicConsume(randomQueueName, false, consumer);

            Console.WriteLine("Logları dinleniyor...");

            consumer.Received += (sender, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());
                Console.WriteLine("Gelen mesaj:" + message);

                // Mesajın işlendiği doğrulanıyor
                channel.BasicAck(e.DeliveryTag, false);
            };

            Console.ReadLine();
        }
    }
}