using Interpret_grading_documents.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Interpret_grading_documents.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GPTService _gptService;
        
        private List<GPTService.GraduationDocument> _analyzedDocuments = new List<GPTService.GraduationDocument>();

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
        public async Task<IActionResult> ProcessText(List<IFormFile> uploadedFiles)
        {
            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                // Optionally, add a ModelState error or a TempData message to inform the user
                ViewBag.Error = "Please upload valid documents.";
                return View("Index", null);
            }

            foreach (var uploadedFile in uploadedFiles)
            {
                var extractedData = await _gptService.ProcessTextPrompts(uploadedFile);
                _analyzedDocuments.Add(extractedData);
            }

            

            return View("Index", _analyzedDocuments);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
        //public IActionResult ViewFDocument(Guid id)
        //{
        //    var document = _analyzedDocuments.Find(d => d.Id == id);
        //    if (document == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(document);
        //}
    }
}
