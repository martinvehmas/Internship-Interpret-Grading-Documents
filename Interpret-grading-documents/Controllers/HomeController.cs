using Interpret_grading_documents.Models;
using Interpret_grading_documents.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Interpret_grading_documents.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FormRecognizerService _formRecognizerService;

        public HomeController(ILogger<HomeController> logger, FormRecognizerService formRecognizerService)
        {
            _logger = logger;
            _formRecognizerService = formRecognizerService;
        }

        public async Task<IActionResult> Index()
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", "../SampleImages/SlutBetyg_01.png");
            var extractedText = await _formRecognizerService.AnalyzeDocumentAsync(imagePath);
            ViewData["ExtractedText"] = extractedText;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
