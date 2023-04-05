// See https://aka.ms/new-console-template for more information

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class Program
{
    static void Main(string[] args)
    {
        ConnectionFactory factory = new ConnectionFactory();
        factory.Uri = new Uri("amqps://uidvbmmg:xYlxfwU1dk6FwGtwxNj8jm1qWjdlwj9U@shark.rmq.cloudamqp.com/uidvbmmg");

        using var connection = factory.CreateConnection();

        var channel = connection.CreateModel();

        //channel.QueueDeclare("hello-queue", true, false, false); //publisher kısmında hello-queue isminde kuyruk oluşturduğumuz için tekrar burda yazmaya gerek,yazsak da sorun olmaz fakat herşey aynı olması lazım, consumer dan da kuyruk oluşturuabiliriz

        //ilk parametre 0 vererek bana herhangi bir boyuttaki mesajı gönderebilirsin anlamı katar 
        //ikinci parametre kaç kaç mesajlar gelsin demek , bizde 1 diyerek 1'er 1'er gelsin demiş olduk
        //Şimdi şöyle eğer  0,6,false şeklinde yaparsak kaçtane consumer varsa hepsine 6 6 gönderir,eğer true yaparsak 2 tane consumer varsa 3 3 , 3 tane soncumar varsa 2 2 2 şeklinde yani toplan consumer kaç tane varsa o kadar eşite böyler.
        channel.BasicQos(0, 2, true);

        var consumer = new EventingBasicConsumer(channel);

        //ilki kuyruk adı 
        //ikinci parametre autoAck olan , trabbitmq consumere mesaj gönderdiğinde bu mesaj doğru da işlense yanlış da işlense kuyruktan siler,eğer false olursa consumer mesajı alır ve işledikten sonra düzgün biçince yani silebilirsin şeklinde publishea haber verecek.
        channel.BasicConsume("hello-queue", false, consumer);

        consumer.Received += (object sender, BasicDeliverEventArgs e) =>
        {
            var message = Encoding.UTF8.GetString(e.Body.ToArray());
            Console.WriteLine("Gelen mesaj:" + message);

            //Oluşan tagı rabbitmq e gönderiyoruz,ulaşan mesja hangi tagla ulaştıysa ilgili mesajı kuyruktan bulup siliyor
            //İkinci parametre o anda memoryde işlenmiş ama rabbitmq e gitmemiş başka mesajlar da varsa onun bilgilerini  rabbitmq e haberder eder eğer true dersek tabi. tek mesajı işlediğimiz için false verdik
            channel.BasicAck(e.DeliveryTag, false);
        };
       
        Console.ReadLine();
    }

  
}