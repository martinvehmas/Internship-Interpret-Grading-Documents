using Interpret_grading_documents.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Interpret_grading_documents.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GPTService _gptService;

        public HomeController(ILogger<HomeController> logger, GPTService gptService)
        {
            _logger = logger;
            _gptService = gptService;
        }

        public IActionResult Index()
        {
            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessText()
        {
            var extractedData = await _gptService.ProcessTextPrompt();

            return View("Index", extractedData);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
