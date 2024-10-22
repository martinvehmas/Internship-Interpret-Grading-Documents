using Interpret_grading_documents.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Interpret_grading_documents.Data;
using Interpret_grading_documents.Models;

namespace Interpret_grading_documents.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static List<GPTService.GraduationDocument> _analyzedDocuments = new List<GPTService.GraduationDocument>();

        private readonly string courseEquivalentsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "CourseEquivalents.json");

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var coursesWithAverageFlag = GetCoursesWithAverageFlag();
            ViewBag.CoursesWithAverageFlag = coursesWithAverageFlag;
            return View(_analyzedDocuments);
        }
        private List<(string MainCourse, List<string> AlternativeCourses, bool IncludedInAverage)> GetCoursesWithAverageFlag()
        {
            var coursesWithAverageFlag = new List<(string MainCourse, List<string> AlternativeCourses, bool IncludedInAverage)>();
            var courseEquivalents = LoadCourseEquivalents();

            if (courseEquivalents != null)
            {
                foreach (var subject in courseEquivalents.Subjects)
                {
                    foreach (var course in subject.Courses)
                    {
                        var alternativeCourses = course.Alternatives.Select(alt => $"{alt.Name} ({alt.Code})").ToList();
                        coursesWithAverageFlag.Add(($"{course.Name} ({course.Code})", alternativeCourses, course.IncludeInAverage));
                    }
                }
            }
            return coursesWithAverageFlag;
        }


        [HttpPost]
        public async Task<IActionResult> ProcessText(List<IFormFile> uploadedFiles)
        {
            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                ViewBag.Error = "Please upload valid documents.";
                return View("Index");
            }

            foreach (var uploadedFile in uploadedFiles)
            {
                var extractedData = await GPTService.ProcessTextPrompts(uploadedFile);
                _analyzedDocuments.Add(extractedData);
            }

            return RedirectToAction("ViewUploadedDocuments");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        public IActionResult ViewDocument(Guid id)
        {
            var document = _analyzedDocuments.Find(d => d.Id == id);
            if (document == null)
            {
                return NotFound();
            }
            return View(document);
        }

        public IActionResult ViewUploadedDocuments()
        {
            return View(_analyzedDocuments);
        }

        [HttpPost]
        public IActionResult RemoveDocument(Guid id)
        {
            var document = _analyzedDocuments.Find(d => d.Id == id);
            if (document != null)
            {
                _analyzedDocuments.Remove(document);

                Console.WriteLine($"Document {document.DocumentName} was successfully removed");
                Console.WriteLine($"\nDocuments remaining:");

                foreach (var doc in _analyzedDocuments)
                {
                    Console.WriteLine(doc.DocumentName);
                }

                return RedirectToAction("ViewUploadedDocuments");
            }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> CourseRequirementsManager()
        {
            var courseEquivalents = LoadCourseEquivalents() ?? new CourseEquivalents
            {
                Subjects = new List<Subject>()
            };

            var validationCourses = await ValidationData.GetCombinedCourses();

            var availableCourses = validationCourses.Values.Select(c => new AvailableCourse
            {
                CourseName = c.CourseName,
                CourseCode = c.CourseCode
            }).ToList();

            ViewBag.AvailableCourses = availableCourses;

            return View(courseEquivalents);
        }

        [HttpPost]
        public IActionResult SaveCourseEquivalents([FromBody] CourseEquivalents courseEquivalents)
        {
            SaveCourseEquivalentsToFile(courseEquivalents);
            return Json(new { success = true });
        }

        private CourseEquivalents LoadCourseEquivalents()
        {
            if (System.IO.File.Exists(courseEquivalentsFilePath))
            {
                var jsonContent = System.IO.File.ReadAllText(courseEquivalentsFilePath);
                return JsonSerializer.Deserialize<CourseEquivalents>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return null;
        }

        private void SaveCourseEquivalentsToFile(CourseEquivalents courseEquivalents)
        {
            var jsonContent = JsonSerializer.Serialize(courseEquivalents, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            });
            System.IO.File.WriteAllText(courseEquivalentsFilePath, jsonContent);
        }

        [HttpGet]
        public IActionResult CheckRequirements(Guid id)
        {
            var document = _analyzedDocuments.Find(d => d.Id == id);
            if (document == null)
            {
                return NotFound();
            }

            var requirementResults = RequirementChecker.DoesStudentMeetRequirement(document);

            // Determine if all requirements are met
            bool meetsAllRequirements = requirementResults.Values.All(r => r.IsMet);

            var model = new RequirementCheckViewModel
            {
                Document = document,
                RequirementResults = requirementResults,
                MeetsRequirement = meetsAllRequirements
            };

            return View(model);
        }
    }
}
