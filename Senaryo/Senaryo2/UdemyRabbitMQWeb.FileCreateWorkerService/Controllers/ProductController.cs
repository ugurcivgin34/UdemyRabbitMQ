using Microsoft.AspNetCore.Mvc;

namespace UdemyRabbitMQWeb.FileCreateWorkerService.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
