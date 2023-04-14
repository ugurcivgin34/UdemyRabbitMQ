// Excel dosyalarý oluþturmak için kullanýlýr.
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Math;
// FileCreateWorkerService'deki modelleri kullanmak için.
using FileCreateWorkerService.Models;
// RabbitMQ ve diðer hizmetleri saðlamak için.
using FileCreateWorkerService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
// RabbitMQ istemci nesnesi için.
using RabbitMQ.Client;
// RabbitMQ istemci olaylarý için kullanýlan RabbitMQ.Client nesnesi.
using RabbitMQ.Client.Events;
// DataTable, DataSet ve veritabaný iþlemleri için kullanýlan diðer nesnelerin tanýmlanmasý için kullanýlýr.
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
// Encoding.UTF8'ý kullanmak için.
using System.Text;
// JSON serileþtirme/deserileþtirme iþlemleri için.
using System.Text.Json;
// SharedModels klasöründeki modelleri kullanmak için.
using UdemyRabbitMQWeb.FileCreateWorkerService.Models.Shared;

namespace FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        // Worker'ýn Logger nesnesi.
        private readonly ILogger<Worker> _logger;
        // RabbitMQ istemci hizmeti.
        private readonly RabbitMQClientService _rabbitMQClientService;
        // IServiceProvider, baðýmlýlýklarý çözmek için kullanýlýr.
        private readonly IServiceProvider _serviceProvider;
        // RabbitMQ kanalý.
        private IModel _channel;
        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitMQClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitMQClientService = rabbitMQClientService;
            _serviceProvider = serviceProvider;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // RabbitMQ istemci nesnesi baðlantýsý oluþturulur.
            _channel = _rabbitMQClientService.Connect();
            // Çoklu iþlem yapmamak için önemli bir ayar. 
            //Bu kod, RabbitMQ kanalý için kalite hizmetlerinin ayarlanmasýný saðlar. Bu ayarlar, BasicQos yöntemi aracýlýðýyla yapýlýr ve üç parametre alýr:
            //prefetchSize: Bu parametre, ön bellek boyutunu belirler.Genellikle sýfýr olarak ayarlanýr, böylece ön bellek boyutu sýnýrlandýrýlmaz.
            //prefetchCount: Bu parametre, ayný anda alýnacak maksimum mesaj sayýsýný belirler. Örneðin, yukarýdaki kodda 1 olarak ayarlanmýþtýr, yani ayný anda yalnýzca bir mesaj alýnabilir.
            //global: Bu parametre, ön bellek ayarlarýnýn channel düzeyinde mi yoksa consumer düzeyinde mi yapýlacaðýný belirler. false olarak ayarlandýðýnda, ön bellek ayarlarý channel düzeyinde yapýlýr.
            //Bu kodda, BasicQos yöntemi, yalnýzca bir mesajýn ayný anda iþlenmesine izin veren ön bellek ayarlarý yapar. Bu, bir iþlemi tamamlamadan önce bir sonraki iþleme geçmenin önüne geçerek, daha iyi bir mesaj iþleme performansý saðlar.
            _channel.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // RabbitMQ istemcisi oluþturulur.
            var consumer = new AsyncEventingBasicConsumer(_channel);
            // Kuyruktan mesaj alýnýr.
            //Bu kod, RabbitMQ kanalý üzerinden bir tüketici aboneliði oluþturmak için kullanýlýr. Bu iþlem, BasicConsume yöntemi aracýlýðýyla yapýlýr ve üç parametre alýr:
            //queue: Bu parametre, tüketicinin abone olacaðý kuyruðun adýný belirtir.Yukarýdaki kodda, RabbitMQClientService.QueueName kullanýlarak, RabbitMQClientService sýnýfý tarafýndan saðlanan kuyruk adý belirtilir.
            //autoAck: Bu parametre, tüketici tarafýndan iþlenen mesajlarýn otomatik olarak iþlenip iþlenmeyeceðini belirler. false olarak ayarlandýðýnda, mesajlarýn manuel olarak onaylanmasý gerekecektir.
            //consumer: Bu parametre, tüketicinin, mesajlarý iþlemek için kullanýlacak AsyncEventingBasicConsumer nesnesini belirtir.
            //Bu kodda, BasicConsume yöntemi, RabbitMQClientService.QueueName adlý bir kuyruða abone olan bir tüketici oluþturur. false olarak ayarlanan autoAck parametresi, mesajlarýn manuel olarak onaylanmasý gerektiðini belirtir.AsyncEventingBasicConsumer nesnesi, mesajlarýn iþlenmesi için gereken olaylarý ve iþlevleri saðlar.
            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);
            // Bir mesaj alýndýðýnda çaðrýlýr.
            consumer.Received += Consumer_Received;

            return Task.CompletedTask;
        }
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            await Task.Delay(5000);

            // Alýnan mesajý "CreateExcelMessage" nesnesine dönüþtürülür.
            var createExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));
            // Bellek akýþý (MemoryStream) oluþturulur.
            using var ms = new MemoryStream();
            // Yeni bir Excel çalýþma kitabý (XLWorkbook) oluþturulur.
            var wb = new XLWorkbook();
            // Yeni bir veri kümesi (DataSet) oluþturulur.
            var ds = new DataSet();
            // "products" adlý bir tablo içeren veri kümesi oluþturulur.
            ds.Tables.Add(GetTable("products"));
            // Yeni bir çalýþma sayfasý oluþturulur.
            wb.Worksheets.Add(ds);
            // Bellek akýþýna (MemoryStream) Excel çalýþma kitabý kaydedilir
            wb.SaveAs(ms);
            // MultipartFormDataContent nesnesi oluþturulur.
            MultipartFormDataContent multipartFormDataContent = new();
            // Excel dosyasý MultipartFormDataContent nesnesine eklenir.
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid().ToString() + ".xlsx");
            // Web API servisinin URL'si tanýmlanýr.
            var baseUrl = "https://localhost:7170/api/files";

            using (var httpClient = new HttpClient())
            {
                // Excel dosyasý Web API servisine gönderilir.
                var response = await httpClient.PostAsync($"{baseUrl}?fileId={createExcelMessage.FileId}", multipartFormDataContent);

                if (response.IsSuccessStatusCode)
                { // Excel dosyasý baþarýyla oluþturulduðunda log mesajý yazýlýr.

                    _logger.LogInformation($"File ( Id : {createExcelMessage.FileId}) was created by successful");
                    // Ýþlenen mesajýn iþlendiði onaylanýr.
                    //Bu kod, RabbitMQ kanalýna, belirli bir mesajýn iþlendiðinin onaylanmasýný saðlar. BasicAck yöntemi aracýlýðýyla yapýlýr ve iki parametre alýr:
                    //deliveryTag: Bu parametre, onaylanacak mesajýn teslim etme etiketidir. RabbitMQ, her bir mesaj için bir teslim etiketi atar ve bu etiketler, mesajlarýn doðru bir þekilde iþlendiðinin onaylanmasýnda kullanýlýr.
                    // multiple: Bu parametre, bir veya birden çok mesajýn onaylanmasýný belirler. false olarak ayarlandýðýnda, yalnýzca belirtilen deliveryTag parametresine sahip mesajýn onaylanmasý saðlanýr.
                    //Bu kodda, BasicAck yöntemi, @event.DeliveryTag deðeriyle belirtilen bir mesajýn baþarýyla iþlendiðinin onaylanmasýný saðlar. false olarak ayarlanan multiple parametresi, yalnýzca belirtilen teslim etiketi ile eþleþen bir mesajýn onaylanmasýný saðlar.
                    _channel.BasicAck(@event.DeliveryTag, false);
                }
            }
        }
        private DataTable GetTable(string tableName)
        {
            // "Product" modellerinin listesi oluþturulur.
            List<FileCreateWorkerService.Models.Product> products;

            using (var scope = _serviceProvider.CreateScope())
            {
                // Veritabanýna baðlanmak için bir context nesnesi oluþturulur.
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2019Context>();
                // "Product" modelleri veritabanýndan alýnýr.
                products = context.Products.ToList();
            }
            // Yeni bir DataTable nesnesi oluþturulur.
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
//Yukarýdaki kod, bir arka plan servisi olarak çalýþan bir uygulamayý göstermektedir. Bu servis, bir RabbitMQ kuyruðundan mesajlarý alýr, alýnan mesajlar kullanýlarak bir Excel dosyasý oluþturur ve oluþturulan Excel dosyasýný bir Web API servisine gönderir.
//Kod, birkaç farklý kütüphane ve hizmeti kullanýr. Bunlar arasýnda RabbitMQ.Client, ClosedXML.Excel ve System.Text.Json yer almaktadýr.
//Ayrýca, kod, veritabaný iþlemleri gerçekleþtirmek için bir EF Core context'i kullanýr. Bu EF Core context'i, IServiceProvider aracýlýðýyla servis olarak enjekte edilir.
//Kod, bir arka plan servisi olarak çalýþtýðýndan, bir "Worker" sýnýfý içerir. Bu sýnýf, BackgroundService sýnýfýndan türetilir ve StartAsync ve ExecuteAsync metodlarý ile birlikte gelir. StartAsync metodu, RabbitMQ istemci nesnesi baðlantýsýný oluþturur ve ExecuteAsync metodu, RabbitMQ istemcisinden gelen mesajlarý iþler.
//Worker sýnýfýnýn en önemli metodu, Consumer_Received metodudur. Bu metod, RabbitMQ istemcisinden bir mesaj aldýðýnda çaðrýlýr. Mesaj, bir JSON nesnesi olarak RabbitMQ istemcisinden alýnýr ve daha sonra oluþturulan Excel dosyasý Web API servisine gönderilir. Bu metod, ayrýca iþlenen mesajýn onaylanmasýný da saðlar.
//Sonuç olarak, yukarýdaki kod, bir arka plan servisi olarak çalýþan bir uygulamayý gösterir ve RabbitMQ, Excel iþlemleri, Web API servisleri ve veritabaný iþlemlerini bir arada kullanarak oldukça güçlü bir yapý oluþturur