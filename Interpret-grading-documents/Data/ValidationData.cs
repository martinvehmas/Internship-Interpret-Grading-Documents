using System.Text.Json;

namespace Interpret_grading_documents.Data
{
    public class CourseDetail
    {
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int? Points { get; set; }
    }

    public static class ValidationData
    {
        public static Dictionary<string, CourseDetail> GetCourses()
        {
            string validationJsonPath = Path.Combine("Data", "kurser.json");
            string validationJson = File.ReadAllText(validationJsonPath);
            var validationCourses = JsonSerializer.Deserialize<Dictionary<string, CourseDetail>>(validationJson);

            // Assign CourseName to each CourseDetail (since the key is the course name)
            foreach (var kvp in validationCourses)
            {
                kvp.Value.CourseName = kvp.Key;
            }
            return validationCourses;
        }
    }
}
