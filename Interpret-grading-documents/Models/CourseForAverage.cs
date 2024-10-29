namespace Interpret_grading_documents.Models
{
    public class CourseForAverage
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public List<AlternativeCourse> AlternativeCourses { get; set; } = new List<AlternativeCourse>();
    }

    public class CoursesForAverageViewModel
    {
        public List<CourseForAverage> CoursesForAverage { get; set; }
    }

    public class AlternativeCourse
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
