using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UdemyRabbitMQWeb.FileCreateWorkerService.Models;
using UdemyRabbitMQWeb.FileCreateWorkerService.Models.Shared;
using UdemyRabbitMQWeb.FileCreateWorkerService.Services;

namespace UdemyRabbitMQWeb.FileCreateWorkerService.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {

        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public ProductController(AppDbContext context, UserManager<IdentityUser> userManager, RabbitMQPublisher rabbitMQPublisher)
        {
            _context = context;
            _userManager = userManager;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CreateProductExcel()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var fileName = $"product-excel-{Guid.NewGuid().ToString().Substring(1, 10)}"; //Dosya ismi

            UserFile userfile = new()
            {
                UserId = user.Id,
                FileName = fileName,
                FileStatus = FileStatus.Creating
            };

            await _context.UserFiles.AddAsync(userfile);

            await _context.SaveChangesAsync();

            _rabbitMQPublisher.Publish(new CreateExcelMessage() { FileId = userfile.Id, UserId = user.Id });
            TempData["StartCreatingExcel"] = true; //Bir requestten diğer bir requeste data taşımak için TempData kullandık

            return RedirectToAction(nameof(Files));

        }

        public async Task<IActionResult> Files()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = await _context.UserFiles.Where(x => x.UserId == user.Id).ToListAsync();
            return View(result);
        }
    }
}
