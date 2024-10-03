using System.Text.Json;
using FuzzySharp;
using System.Linq;
using System.Collections.Generic;
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
    private int matchThreshold = 80;
    private int minimumScore = 70;

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

    private string FindBestMatch(string subjectName)
    {
        string bestMatch = null;
        int bestScore = 0;

        foreach (var validationCourse in validationCourses.Keys)
        {
            int score = Fuzz.Ratio(subjectName, validationCourse);

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = validationCourse;
            }
        }

        if (bestScore >= minimumScore)
        {
            if (bestScore >= matchThreshold)
            {
                Console.WriteLine($"Fuzzy match found! Original: '{subjectName}' => Matched with: '{bestMatch}' (Score: {bestScore})");
            }
            else
            {
                Console.WriteLine($"Fuzzy match found, but score is below match threshold: '{subjectName}' => '{bestMatch}' (Score: {bestScore})");
            }
            return bestMatch;
        }

        Console.WriteLine($"No valid match found for '{subjectName}' (Best score: {bestScore}) - Below minimum score of {minimumScore}");
        return null;
    }

    public int GetTotalPoints()
    {
        int totalPoints = 0;

        foreach (var subject in graduationDocument.Subjects)
        {
            string bestMatch = FindBestMatch(subject.SubjectName);

            if (bestMatch != null && validationCourses.ContainsKey(bestMatch))
            {
                Console.WriteLine($"Adding points for subject '{subject.SubjectName}' with match '{bestMatch}' - Points: {validationCourses[bestMatch].Points}");
                totalPoints += validationCourses[bestMatch].Points ?? 0;
            }
        }

        return totalPoints;
    }

    public List<string> GetUnmatchedSubjects()
    {
        var unmatchedSubjects = new List<string>();

        foreach (var subject in graduationDocument.Subjects)
        {
            string bestMatch = FindBestMatch(subject.SubjectName);

            if (bestMatch == null)
            {
                unmatchedSubjects.Add(subject.SubjectName);
                Console.WriteLine($"Unmatched subject: '{subject.SubjectName}'");
            }
        }

        return unmatchedSubjects;
    }

    public GraduationDocument UpdateMatchedSubjects()
    {
        foreach (var subject in graduationDocument.Subjects)
        {
            // Normalize the subject name for comparison
            string subjectNameKey = subject.SubjectName.Trim();

            if (validationCourses.ContainsKey(subjectNameKey))
            {
                var validationCourse = validationCourses[subjectNameKey];
                subject.GymnasiumPoints = validationCourse.Points.ToString();
                subject.CourseCode = validationCourse.CourseCode;
            }
        }
        return graduationDocument;
    }

    public GraduationDocument CompareCourses()
    {
        int totalPoints = GetTotalPoints();
        List<string> unmatchedSubjects = GetUnmatchedSubjects();
        var updatedDocument = UpdateMatchedSubjects();

        Console.WriteLine($"Total Points of Matched Courses: {totalPoints}");
        Console.WriteLine("Subjects that did not match any courses:");

        foreach (var subject in unmatchedSubjects)
        {
            Console.WriteLine(subject);
        }

        return updatedDocument;
    }
}
