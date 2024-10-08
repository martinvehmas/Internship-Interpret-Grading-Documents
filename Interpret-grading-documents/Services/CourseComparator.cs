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

    private (string BestMatch, int BestScore) FindBestMatch(string subjectName)
    {
        string bestMatch = null;
        int bestScore = 0;

        foreach (var validationCourse in validationCourses.Keys)
        {
            int score = Fuzz.Ratio(subjectName.ToLower(), validationCourse.ToLower());

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = validationCourse;
            }
        }

        if (bestScore >= matchThreshold)
        {
            Console.WriteLine($"Fuzzy match found! Original: '{subjectName}' => Matched with: '{bestMatch}' (Score: {bestScore})");
            return (bestMatch, bestScore);
        }

        Console.WriteLine($"No valid match found for '{subjectName}' (Best score: {bestScore}) - Below match threshold of {matchThreshold}");
        return (null, bestScore);
    }

    public int GetTotalPoints()
    {
        int totalPoints = 0;

        foreach (var subject in graduationDocument.Subjects)
        {
            var (bestMatch, bestScore) = FindBestMatch(subject.SubjectName);
            subject.FuzzyMatchScore = bestScore;  // Set the fuzzy match score for the subject

            if (bestMatch != null && validationCourses.ContainsKey(bestMatch))
            {
                Console.WriteLine($"Adding points for subject '{subject.SubjectName}' with match '{bestMatch}' - Points: {validationCourses[bestMatch].Points}");

                // Update the subject details
                subject.GymnasiumPoints = validationCourses[bestMatch].Points.ToString();
                subject.CourseCode = validationCourses[bestMatch].CourseCode;

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
            var (bestMatch, bestScore) = FindBestMatch(subject.SubjectName);
            subject.FuzzyMatchScore = bestScore;  // Set the fuzzy match score for the subject

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
            var (bestMatch, bestScore) = FindBestMatch(subject.SubjectName);
            subject.FuzzyMatchScore = bestScore;  // Set the fuzzy match score for the subject

            if (bestMatch != null && validationCourses.ContainsKey(bestMatch))
            {
                var validationCourse = validationCourses[bestMatch];

                subject.SubjectName = bestMatch;
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
