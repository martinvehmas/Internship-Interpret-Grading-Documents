namespace Interpret_grading_documents.Models;

public class RequirementResult
{
    public string CourseName { get; set; }
    public string RequiredGrade { get; set; }
    public bool IsMet { get; set; }
    public string StudentGrade { get; set; }
    public bool MetByAlternativeCourse { get; set; }
    public string AlternativeCourseName { get; set; }
    public string AlternativeCourseGrade { get; set; }
    public bool MetByHigherLevelCourse { get; set; }
    public string HigherLevelCourseName { get; set; }
    public string HigherLevelCourseGrade { get; set; }
    public List<string> OtherGradesInAlternatives { get; set; }
}