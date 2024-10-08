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

        public static async Task<Dictionary<string, CourseDetail>> GetCoursesFromApi()
        {
            string url = "https://api.skolverket.se/syllabus/v1/courses?timespan=LATEST";
            string outputPath = Path.Combine("Data", "kurserApi.json");

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    
                    string responseBody = await response.Content.ReadAsStringAsync();


                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    CourseApiResponse apiResponse = JsonSerializer.Deserialize<CourseApiResponse>(responseBody, options);

                    
                    var courseDetails = apiResponse.Courses.Select(c => new CourseDetail
                    {
                        CourseCode = c.CourseCode,
                        CourseName = c.CourseName,
                        Points = int.TryParse(c.CoursePoints, out int points) ? (int?)points : null
                    }).ToList();

                    
                    string courseDetailsJson = JsonSerializer.Serialize(courseDetails, new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(outputPath, courseDetailsJson);

                    Console.WriteLine("Courses have been successfully saved to kurserApi.Json.");

                    
                    string validationJson = File.ReadAllText(outputPath);
                    var validationCoursesApi = JsonSerializer.Deserialize<Dictionary<string, CourseDetail>>(validationJson);

                    // Assign CourseName to each CourseDetail (since the key is the course name)
                    foreach (var kvp in validationCoursesApi)
                    {
                        kvp.Value.CourseCode = kvp.Key;
                    }
                    return validationCoursesApi;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred: {e.Message}");
                }
            }
            return null;
        }
    }
}

