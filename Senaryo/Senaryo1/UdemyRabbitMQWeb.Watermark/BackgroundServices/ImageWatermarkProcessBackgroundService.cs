using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Drawing;
using System.Text;
using System.Text.Json;
using UdemyRabbitMQWeb.Watermark.Services;

namespace UdemyRabbitMQWeb.Watermark.BackgroundServices
{
    // Bu kod, bir arka plan görevi olarak çalışan bir servis tanımlar ve bir RabbitMQ mesaj kuyruğundan gelen mesajları dinleyerek görüntülere filigran ekler.
    public class ImageWatermarkProcessBackgroundService : BackgroundService
    {
        private readonly RabbitMQClientService _rabbitMQClientService; // RabbitMQ istemcisini enjekte eden bir bağımlılık.
        private readonly ILogger<ImageWatermarkProcessBackgroundService> _logger; // ILogger ile loglama yapmak için kullanılan bir bağımlılık.
        private IModel _channel; // RabbitMQ kanalı nesnesi.
        public ImageWatermarkProcessBackgroundService(RabbitMQClientService rabbitMQClientService, ILogger<ImageWatermarkProcessBackgroundService> logger)
        {
            _rabbitMQClientService = rabbitMQClientService;
            _logger = logger;
        }

        // Servis başlatıldığında RabbitMQ sunucusuna bağlanır.
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitMQClientService.Connect(); // RabbitMQ istemcisine bağlanır.
            _channel.BasicQos(0, 1, false); // Bu özellik, birden fazla işçi işlemi çalıştırdığında her bir işçiye sadece bir mesajın dağıtılmasını sağlar.
            return base.StartAsync(cancellationToken);
        }

        // Servis çalışırken, RabbitMQ kuyruğundan mesajları dinler ve Consumer_Received yöntemine iletir.
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel); // Consumer nesnesi, RabbitMQ kuyruğundan mesajları almak için kullanılır.
            consumer.Received += Consumer_Received; // Yeni bir mesaj alındığında, Consumer_Received yöntemi çağrılır.
            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer); // Kuyruğu belirtilen isimle dinler.

            return Task.CompletedTask;
        }

        // RabbitMQ kuyruğundan yeni bir mesaj geldiğinde çağrılan yöntem.
        private Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                var productImageCreatedEvent = JsonSerializer.Deserialize<productImageCreatedEvent>(Encoding.UTF8.GetString(@event.Body.ToArray())); // Gelen mesajın içeriği, JSON nesnesine dönüştürülür.
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", productImageCreatedEvent.ImageName); // Dosya yolu oluşturulur.
                var siteName = "wwww.mysite.com"; // Filigran olarak eklenecek site adı.
                using var img = Image.FromFile(path); // Görüntü dosyası açılır.
                using var graphic = Graphics.FromImage(img); // Görüntü üzerinde grafik işlemleri yapmak için graphic nesnesi oluşturulur.
                var font = new Font(FontFamily.GenericMonospace, 40, FontStyle.Bold, GraphicsUnit.Pixel); // Filigran metni için kullanılacak font tanımlanır.
                var textSize = graphic.MeasureString(siteName, font); // Font boyutu ölçülür.
                var color = Color.FromArgb(128, 255, 255, 255); // Filigranın arka plan rengi tanımlanır.
                var brush = new SolidBrush(color); // Brush nesnesi, filigran rengini tutmak için kullanılır.
                var position = new Point(img.Width - ((int)textSize.Width + 30), img.Height - ((int)textSize.Height + 30)); // Filigranın konumu belirlenir.
                graphic.DrawString(siteName, font, brush, position); // Filigran görüntüye çizilir.
                img.Save("wwwroot/Images/watermarks/" + productImageCreatedEvent.ImageName); // Görüntü, yeni filigranlı görüntü olarak kaydedilir.
                img.Dispose(); // Kullanılan grafik nesneleri silinir.
                graphic.Dispose();
                _channel.BasicAck(@event.DeliveryTag, false); // RabbitMQ'ya mesajın işlendiğine dair onay gönderilir.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.CompletedTask;
        }

        // Servis durdurulduğunda çağrılan yöntem.
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}

