using Interpret_grading_documents.Models;
using Interpret_grading_documents.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
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
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", "examensbevis-gymnasieskola-yrkes-el.pdf");
            var keyValuePairs = await _formRecognizerService.AnalyzeDocumentForKeyValuesAsync(imagePath);

            ViewData["KeyValuePairs"] = keyValuePairs;

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
