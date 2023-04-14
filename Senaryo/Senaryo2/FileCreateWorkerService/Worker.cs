// Excel dosyalar� olu�turmak i�in kullan�l�r.
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Math;
// FileCreateWorkerService'deki modelleri kullanmak i�in.
using FileCreateWorkerService.Models;
// RabbitMQ ve di�er hizmetleri sa�lamak i�in.
using FileCreateWorkerService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
// RabbitMQ istemci nesnesi i�in.
using RabbitMQ.Client;
// RabbitMQ istemci olaylar� i�in kullan�lan RabbitMQ.Client nesnesi.
using RabbitMQ.Client.Events;
// DataTable, DataSet ve veritaban� i�lemleri i�in kullan�lan di�er nesnelerin tan�mlanmas� i�in kullan�l�r.
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
// Encoding.UTF8'� kullanmak i�in.
using System.Text;
// JSON serile�tirme/deserile�tirme i�lemleri i�in.
using System.Text.Json;
// SharedModels klas�r�ndeki modelleri kullanmak i�in.
using UdemyRabbitMQWeb.FileCreateWorkerService.Models.Shared;

namespace FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        // Worker'�n Logger nesnesi.
        private readonly ILogger<Worker> _logger;
        // RabbitMQ istemci hizmeti.
        private readonly RabbitMQClientService _rabbitMQClientService;
        // IServiceProvider, ba��ml�l�klar� ��zmek i�in kullan�l�r.
        private readonly IServiceProvider _serviceProvider;
        // RabbitMQ kanal�.
        private IModel _channel;
        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitMQClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitMQClientService = rabbitMQClientService;
            _serviceProvider = serviceProvider;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // RabbitMQ istemci nesnesi ba�lant�s� olu�turulur.
            _channel = _rabbitMQClientService.Connect();
            // �oklu i�lem yapmamak i�in �nemli bir ayar. 
            //Bu kod, RabbitMQ kanal� i�in kalite hizmetlerinin ayarlanmas�n� sa�lar. Bu ayarlar, BasicQos y�ntemi arac�l���yla yap�l�r ve �� parametre al�r:
            //prefetchSize: Bu parametre, �n bellek boyutunu belirler.Genellikle s�f�r olarak ayarlan�r, b�ylece �n bellek boyutu s�n�rland�r�lmaz.
            //prefetchCount: Bu parametre, ayn� anda al�nacak maksimum mesaj say�s�n� belirler. �rne�in, yukar�daki kodda 1 olarak ayarlanm��t�r, yani ayn� anda yaln�zca bir mesaj al�nabilir.
            //global: Bu parametre, �n bellek ayarlar�n�n channel d�zeyinde mi yoksa consumer d�zeyinde mi yap�laca��n� belirler. false olarak ayarland���nda, �n bellek ayarlar� channel d�zeyinde yap�l�r.
            //Bu kodda, BasicQos y�ntemi, yaln�zca bir mesaj�n ayn� anda i�lenmesine izin veren �n bellek ayarlar� yapar. Bu, bir i�lemi tamamlamadan �nce bir sonraki i�leme ge�menin �n�ne ge�erek, daha iyi bir mesaj i�leme performans� sa�lar.
            _channel.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // RabbitMQ istemcisi olu�turulur.
            var consumer = new AsyncEventingBasicConsumer(_channel);
            // Kuyruktan mesaj al�n�r.
            //Bu kod, RabbitMQ kanal� �zerinden bir t�ketici aboneli�i olu�turmak i�in kullan�l�r. Bu i�lem, BasicConsume y�ntemi arac�l���yla yap�l�r ve �� parametre al�r:
            //queue: Bu parametre, t�keticinin abone olaca�� kuyru�un ad�n� belirtir.Yukar�daki kodda, RabbitMQClientService.QueueName kullan�larak, RabbitMQClientService s�n�f� taraf�ndan sa�lanan kuyruk ad� belirtilir.
            //autoAck: Bu parametre, t�ketici taraf�ndan i�lenen mesajlar�n otomatik olarak i�lenip i�lenmeyece�ini belirler. false olarak ayarland���nda, mesajlar�n manuel olarak onaylanmas� gerekecektir.
            //consumer: Bu parametre, t�keticinin, mesajlar� i�lemek i�in kullan�lacak AsyncEventingBasicConsumer nesnesini belirtir.
            //Bu kodda, BasicConsume y�ntemi, RabbitMQClientService.QueueName adl� bir kuyru�a abone olan bir t�ketici olu�turur. false olarak ayarlanan autoAck parametresi, mesajlar�n manuel olarak onaylanmas� gerekti�ini belirtir.AsyncEventingBasicConsumer nesnesi, mesajlar�n i�lenmesi i�in gereken olaylar� ve i�levleri sa�lar.
            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);
            // Bir mesaj al�nd���nda �a�r�l�r.
            consumer.Received += Consumer_Received;

            return Task.CompletedTask;
        }
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            await Task.Delay(5000);

            // Al�nan mesaj� "CreateExcelMessage" nesnesine d�n��t�r�l�r.
            var createExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));
            // Bellek ak��� (MemoryStream) olu�turulur.
            using var ms = new MemoryStream();
            // Yeni bir Excel �al��ma kitab� (XLWorkbook) olu�turulur.
            var wb = new XLWorkbook();
            // Yeni bir veri k�mesi (DataSet) olu�turulur.
            var ds = new DataSet();
            // "products" adl� bir tablo i�eren veri k�mesi olu�turulur.
            ds.Tables.Add(GetTable("products"));
            // Yeni bir �al��ma sayfas� olu�turulur.
            wb.Worksheets.Add(ds);
            // Bellek ak���na (MemoryStream) Excel �al��ma kitab� kaydedilir
            wb.SaveAs(ms);
            // MultipartFormDataContent nesnesi olu�turulur.
            MultipartFormDataContent multipartFormDataContent = new();
            // Excel dosyas� MultipartFormDataContent nesnesine eklenir.
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid().ToString() + ".xlsx");
            // Web API servisinin URL'si tan�mlan�r.
            var baseUrl = "https://localhost:7170/api/files";

            using (var httpClient = new HttpClient())
            {
                // Excel dosyas� Web API servisine g�nderilir.
                var response = await httpClient.PostAsync($"{baseUrl}?fileId={createExcelMessage.FileId}", multipartFormDataContent);

                if (response.IsSuccessStatusCode)
                { // Excel dosyas� ba�ar�yla olu�turuldu�unda log mesaj� yaz�l�r.

                    _logger.LogInformation($"File ( Id : {createExcelMessage.FileId}) was created by successful");
                    // ��lenen mesaj�n i�lendi�i onaylan�r.
                    //Bu kod, RabbitMQ kanal�na, belirli bir mesaj�n i�lendi�inin onaylanmas�n� sa�lar. BasicAck y�ntemi arac�l���yla yap�l�r ve iki parametre al�r:
                    //deliveryTag: Bu parametre, onaylanacak mesaj�n teslim etme etiketidir. RabbitMQ, her bir mesaj i�in bir teslim etiketi atar ve bu etiketler, mesajlar�n do�ru bir �ekilde i�lendi�inin onaylanmas�nda kullan�l�r.
                    // multiple: Bu parametre, bir veya birden �ok mesaj�n onaylanmas�n� belirler. false olarak ayarland���nda, yaln�zca belirtilen deliveryTag parametresine sahip mesaj�n onaylanmas� sa�lan�r.
                    //Bu kodda, BasicAck y�ntemi, @event.DeliveryTag de�eriyle belirtilen bir mesaj�n ba�ar�yla i�lendi�inin onaylanmas�n� sa�lar. false olarak ayarlanan multiple parametresi, yaln�zca belirtilen teslim etiketi ile e�le�en bir mesaj�n onaylanmas�n� sa�lar.
                    _channel.BasicAck(@event.DeliveryTag, false);
                }
            }
        }
        private DataTable GetTable(string tableName)
        {
            // "Product" modellerinin listesi olu�turulur.
            List<FileCreateWorkerService.Models.Product> products;

            using (var scope = _serviceProvider.CreateScope())
            {
                // Veritaban�na ba�lanmak i�in bir context nesnesi olu�turulur.
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2019Context>();
                // "Product" modelleri veritaban�ndan al�n�r.
                products = context.Products.ToList();
            }
            // Yeni bir DataTable nesnesi olu�turulur.
            DataTable table = new DataTable { TableName = tableName };

            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Name", typeof(String));
            table.Columns.Add("ProductNumber", typeof(string));
            table.Columns.Add("Color", typeof(string));
            products.ForEach(x =>
            {
                // "Product" modelleri tabloya eklenir.
                table.Rows.Add(x.ProductId, x.Name, x.ProductNumber, x.Color);
            });
            return table;
        }
    }
}
//Yukar�daki kod, bir arka plan servisi olarak �al��an bir uygulamay� g�stermektedir. Bu servis, bir RabbitMQ kuyru�undan mesajlar� al�r, al�nan mesajlar kullan�larak bir Excel dosyas� olu�turur ve olu�turulan Excel dosyas�n� bir Web API servisine g�nderir.
//Kod, birka� farkl� k�t�phane ve hizmeti kullan�r. Bunlar aras�nda RabbitMQ.Client, ClosedXML.Excel ve System.Text.Json yer almaktad�r.
//Ayr�ca, kod, veritaban� i�lemleri ger�ekle�tirmek i�in bir EF Core context'i kullan�r. Bu EF Core context'i, IServiceProvider arac�l���yla servis olarak enjekte edilir.
//Kod, bir arka plan servisi olarak �al��t���ndan, bir "Worker" s�n�f� i�erir. Bu s�n�f, BackgroundService s�n�f�ndan t�retilir ve StartAsync ve ExecuteAsync metodlar� ile birlikte gelir. StartAsync metodu, RabbitMQ istemci nesnesi ba�lant�s�n� olu�turur ve ExecuteAsync metodu, RabbitMQ istemcisinden gelen mesajlar� i�ler.
//Worker s�n�f�n�n en �nemli metodu, Consumer_Received metodudur. Bu metod, RabbitMQ istemcisinden bir mesaj ald���nda �a�r�l�r. Mesaj, bir JSON nesnesi olarak RabbitMQ istemcisinden al�n�r ve daha sonra olu�turulan Excel dosyas� Web API servisine g�nderilir. Bu metod, ayr�ca i�lenen mesaj�n onaylanmas�n� da sa�lar.
//Sonu� olarak, yukar�daki kod, bir arka plan servisi olarak �al��an bir uygulamay� g�sterir ve RabbitMQ, Excel i�lemleri, Web API servisleri ve veritaban� i�lemlerini bir arada kullanarak olduk�a g��l� bir yap� olu�turur