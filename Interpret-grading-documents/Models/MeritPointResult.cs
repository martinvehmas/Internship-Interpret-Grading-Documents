namespace Interpret_grading_documents.Models;

public class MeritPointResult
{
    public string CourseName { get; set; }
    public string StudentGrade { get; set; }
    public double MeritPoint { get; set; }
    public string OriginalCourseGrade { get; set; }
    public string AlternativeCourseGrade { get; set; }
    public List<string> OtherGradesInAlternatives { get; set; }
}