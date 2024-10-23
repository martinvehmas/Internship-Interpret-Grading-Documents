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
        private readonly IWebHostEnvironment _hostingEnvironment;
        private static List<GPTService.GraduationDocument> _analyzedDocuments = new List<GPTService.GraduationDocument>();

        private readonly string courseEquivalentsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "CourseEquivalents.json");

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
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
                return View("Index", _analyzedDocuments);
            }
            string existingPersonalId = _analyzedDocuments.FirstOrDefault()?.PersonalId;
            List<GPTService.GraduationDocument> newDocuments = new List<GPTService.GraduationDocument>();

            foreach (var uploadedFile in uploadedFiles)
            {
                var extractedData = await GPTService.ProcessTextPrompts(uploadedFile);

                if (string.IsNullOrEmpty(existingPersonalId))
                {
                    existingPersonalId = extractedData.PersonalId;
                }
                else
                {
                    if (extractedData.PersonalId != existingPersonalId)
                    {
                        ViewBag.Error = "One or more uploaded documents do not match the social security ID of previously uploaded documents.";
                        return View("Index", _analyzedDocuments);
                    }
                }

                newDocuments.Add(extractedData);
            }

            _analyzedDocuments.AddRange(newDocuments);

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
            if (_analyzedDocuments.Count == 0)
            {
                ViewBag.UserName = null;
                ViewBag.ExamStatus = null;
            }
            else
            {
                string highestExamStatus = GPTService.GetHighestExamStatus(_analyzedDocuments);

                string userName = _analyzedDocuments.FirstOrDefault()?.FullName ?? "Uploaded";

                ViewBag.UserName = userName;
                ViewBag.ExamStatus = highestExamStatus;
            }

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
            string jsonFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "CourseEquivalents.json");
            var document = _analyzedDocuments.Find(d => d.Id == id);
            if (document == null)
            {
                return NotFound();
            }

            var requirementResults = RequirementChecker.DoesStudentMeetRequirement(document, jsonFilePath);

            var test = RequirementChecker.CalculateAverageGrade(document, jsonFilePath);

            // Determine if all requirements are met
            bool meetsAllRequirements = requirementResults.Values.All(r => r.IsMet);

            var model = new RequirementCheckViewModel
            {
                Document = document,
                RequirementResults = requirementResults,
                MeetsRequirement = meetsAllRequirements
            };

            // Pass the jsonFilePath to the view via ViewBag
            ViewBag.JsonFilePath = jsonFilePath;

            return View(model);
        }

        public bool UserHasValidExam()
        {
            foreach (var document in _analyzedDocuments)
            {
                GPTService.ExamValidator(document);

                if (!string.IsNullOrEmpty(document.HasValidDegree) && document.HasValidDegree.Contains("examen", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
