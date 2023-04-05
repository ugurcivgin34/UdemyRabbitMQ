// See https://aka.ms/new-console-template for more information

using RabbitMQ.Client;
using System.Text;

public class Program
{
    static void Main(string[] args)
    {
        ConnectionFactory factory = new ConnectionFactory();
        factory.Uri = new Uri("amqps://uidvbmmg:xYlxfwU1dk6FwGtwxNj8jm1qWjdlwj9U@shark.rmq.cloudamqp.com/uidvbmmg");

        using var connection = factory.CreateConnection();

         var channel = connection.CreateModel();

        //queue kuyruğun adı
        //durable false olursa rabbitmq de oluşan kuyruklar memory de tutulur ,rabbitmq  restart atılırsa kuyruklar memory de tutulduğu için memoryde kikuyruklar da gider.Eğer true olursa kuyruklar fiziksel olarak kaydedilir ,rabbitmq ye restart atılsa bile kuyruklar kaybolmaz
        //exclusive buradaki kuyruğa sadece burda oluşturduğumuz yani yukardaki kanal olan ifade üzerinden sadece bağlanabiliriz.Fakat consumer tarafındanda buraya farklı bir kanal üzerinden bağlanmak istiyoruz.Bu yüzden false demek gerekiyor öbür türlü true olması gerek.
        //autodelete ise son consumer da kuyruktan veri aldıktan sonra kuyruğun silinmesi isteniyorsa true olması gerekiyor, false olursa silmez .Yanlışıkla consumer down olabilir.Bu yüzden false olmasında fayda var.
        channel.QueueDeclare("hello-queue", true, false, false);

        Enumerable.Range(1, 50).ToList().ForEach(x =>
        {
            string message = $"message {x}";

            var messageBody = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(string.Empty, "hello-queue", null, messageBody); //hello-queue bilerek verdik ki aynı olsun ona göre maplesin route map'e göre

            Console.WriteLine($"Mesaj gönderilmiştir: {message}");

        });

        Console.ReadLine();
    }
}