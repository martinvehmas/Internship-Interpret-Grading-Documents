using System.Text.Json;
using static Interpret_grading_documents.Services.GPTService;

namespace Interpret_grading_documents.Services;

public class CourseDetail
{
    public string CourseName { get; set; }
    public string CourseCode { get; set; }
    public int? Points { get; set; }
}
public class CourseComparator
{
    private Dictionary<string, CourseDetail> validationCourses;
    private GraduationDocument graduationDocument;

    public CourseComparator(GraduationDocument graduationDocument, string validationJsonPath)
    {
        this.graduationDocument = graduationDocument;

        // Read and parse the validation JSON using System.Text.Json
        string validationJson = File.ReadAllText(validationJsonPath);
        validationCourses = JsonSerializer.Deserialize<Dictionary<string, CourseDetail>>(validationJson);

        // Assign CourseName to each CourseDetail (since the key is the course name)
        foreach (var kvp in validationCourses)
        {
            kvp.Value.CourseName = kvp.Key;
        }
    }

    public int GetTotalPoints()
    {
        var matchedSubjects = graduationDocument.Subjects
            .Where(subject => validationCourses.ContainsKey(subject.SubjectName));

        int totalPoints = matchedSubjects
                .Sum(subject => validationCourses[subject.SubjectName].Points ?? 0);

        return totalPoints;
    }

    public List<string> GetUnmatchedSubjects()
    {
        var unmatchedSubjects = graduationDocument.Subjects
            .Where(subject => !validationCourses.ContainsKey(subject.SubjectName))
            .Select(subject => subject.SubjectName)
            .ToList();

        return unmatchedSubjects;
    }

    public void CompareCourses()
    {
        int totalPoints = GetTotalPoints();
        List<string> unmatchedSubjects = GetUnmatchedSubjects();

        Console.WriteLine($"Total Points of Matched Courses: {totalPoints}");
        Console.WriteLine("Subjects that did not match any courses:");

        foreach (var subject in unmatchedSubjects)
        {
            Console.WriteLine(subject);
        }
    }
}

