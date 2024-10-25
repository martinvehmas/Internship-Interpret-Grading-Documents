namespace Interpret_grading_documents.Models;

public class RequirementResult
{
    public string CourseName { get; set; }
    public string RequiredGrade { get; set; }
    public bool IsMet { get; set; }
    public string StudentGrade { get; set; }
    public string OriginalCourseGrade { get; set; }
    public string AlternativeCourseGrade { get; set; }
    public List<string> OtherGradesInAlternatives { get; set; }
}