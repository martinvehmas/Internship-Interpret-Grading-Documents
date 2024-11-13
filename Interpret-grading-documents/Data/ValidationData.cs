using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Interpret_grading_documents.Data
{
    public class CourseDetail
    {
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int? Points { get; set; }
    }

    public class CourseApiResponse
    {
        [JsonPropertyName("courses")]
        public List<CourseFromApi> Courses { get; set; }
    }

    public class CourseFromApi
    {
        [JsonPropertyName("courseCode")]
        public string CourseCode { get; set; }

        [JsonPropertyName("courseName")]
        public string CourseName { get; set; }

        [JsonPropertyName("coursePoints")]
        public string CoursePoints { get; set; }
    }

    public static class ValidationData
    {

        private static Dictionary<string, CourseDetail> _coursesCache;
        private static Dictionary<string, CourseDetail> _coursesFromApiCache;
        private static Dictionary<string, CourseDetail> _combinedCoursesCache;
        private static readonly object _lock = new object();
        private static DateTime _lastCacheUpdateTime;
        private static TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
        public static Dictionary<string, CourseDetail> GetCourses()
        {
            if (_coursesCache != null)
                return _coursesCache;

            lock (_lock)
            {
                if (_coursesCache != null)
                    return _coursesCache;

                string validationJsonPath = Path.Combine("Data", "kurser.json");
                string validationJson = File.ReadAllText(validationJsonPath);
                var validationCourses = JsonSerializer.Deserialize<Dictionary<string, CourseDetail>>(validationJson);

                foreach (var kvp in validationCourses)
                {
                    kvp.Value.CourseName = kvp.Key;
                }

                _coursesCache = validationCourses;
                return _coursesCache;
            }
        }

        public static Dictionary<string, CourseDetail> GetCoursesFromApi()
        {
            if (_coursesFromApiCache != null)
                return _coursesFromApiCache;

            lock (_lock)
            {
                if (_coursesFromApiCache != null)
                    return _coursesFromApiCache;

                string outputPath = Path.Combine("Data", "kurserApi.json");

                string courseDetailsJson = File.ReadAllText(outputPath);
                var courseDetails = JsonSerializer.Deserialize<List<CourseDetail>>(courseDetailsJson);

                _coursesFromApiCache = courseDetails
                    .GroupBy(c => c.CourseName)
                    .ToDictionary(group => group.Key, group => group.First());

                return _coursesFromApiCache;
            }
        }


        public static Dictionary<string, CourseDetail> GetCombinedCourses()
        {
            if (_combinedCoursesCache != null && DateTime.UtcNow - _lastCacheUpdateTime < _cacheExpiration)
                return _combinedCoursesCache;

            var coursesFromJson = GetCourses();
            var coursesFromApi = GetCoursesFromApi();

            var combinedCourses = coursesFromJson
                .Concat(coursesFromApi)
                .GroupBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

            _combinedCoursesCache = combinedCourses;
            return _combinedCoursesCache;
        }
    }
}