using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Consumer
{
    public class Program
    {
        private static void Main(string[] args)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqps://yjvxzyim:8SIohLh_WS7SCqCKNVJr66-BY1gQgrlh@woodpecker.rmq.cloudamqp.com/yjvxzyim");

            using var connection = factory.CreateConnection();

            var channel = connection.CreateModel();

            //Rastgele kuyruk ismi vermiş oldu
            var randomQueueName = channel.QueueDeclare().QueueName;

            //var randomQueueName = "log-database-save-queue";
            //channel.QueueDeclare(randomQueueName, true, false, false);//exchande consumer kuyruk oluşturduktan sonra kalıcı kuyruk olmasını istiyorsak QueueDeclare yapabiliriz.Consumer her down olduğunda kaldığı yerden devam edebilir

            //Bu şekilde ilgili exchange e bağlı olarka kuyruk oluşur ve mesja alınır,consumer down olunca kuyrukta silinmiş olur
            channel.QueueBind(randomQueueName, "logs-fanout", "", null);

            channel.BasicQos(0, 1, true);

            var consumer = new EventingBasicConsumer(channel);


            channel.BasicConsume(randomQueueName, false, consumer);

            Console.WriteLine("Logları dinleniyor...");

            consumer.Received += (sender, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());
                Console.WriteLine("Gelen mesaj:" + message);

                channel.BasicAck(e.DeliveryTag, false);
            };

            Console.ReadLine();
        }
    }
}