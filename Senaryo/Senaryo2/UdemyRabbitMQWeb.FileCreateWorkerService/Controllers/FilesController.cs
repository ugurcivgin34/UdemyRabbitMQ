using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UdemyRabbitMQWeb.FileCreateWorkerService.Hubs;
using UdemyRabbitMQWeb.FileCreateWorkerService.Models;

namespace UdemyRabbitMQWeb.FileCreateWorkerService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<MyHub> _hubContext;
        public FilesController(AppDbContext context, IHubContext<MyHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int fileId)
        {
            // 1. Dosya boş mu diye kontrol ediyoruz
            if (file is not { Length: > 0 }) return BadRequest();
            // 2. İlgili dosya kaydını veritabanından alıyoruz
            var userFile = await _context.UserFiles.FirstAsync(x => x.Id == fileId);
            // 3. Yüklenen dosyanın adı ve uzantısı kullanılarak, kaydedileceği konumun tam yolu oluşturuluyor
            var filePath = userFile.FileName + Path.GetExtension(file.FileName);
            // 4. Dosya yolu oluşturuluyor
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files", filePath);
            // 5. FileStream nesnesi oluşturularak dosyanın verileri bu dosyaya kaydediliyor
            using FileStream stream = new(path, FileMode.Create);
            // 6. Dosya kaydının güncellenmesi
            await file.CopyToAsync(stream);
            userFile.CreatedDate = DateTime.Now;
            userFile.FilePath = filePath;
            userFile.FileStatus = FileStatus.Completed;
            await _context.SaveChangesAsync();
            //Sisteme giriş yapan kullanıcının id sini alarak mesaj gönderiyor.BU sayede o kişiye ait mesajlar gitmiş olacak
            await _hubContext.Clients.User(userFile.UserId).SendAsync("CompletedFile");
            // 7. HTTP 200 Ok yanıtı döndürülüyor
            return Ok();
        }
    }
}
