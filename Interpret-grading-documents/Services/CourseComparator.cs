using System.Text.Json;
using FuzzySharp;
using System.Linq;
using System.Collections.Generic;
using static Interpret_grading_documents.Services.GPTService;
using Interpret_grading_documents.Data;

namespace Interpret_grading_documents.Services;

public static class CourseComparator
{
    private const int matchThreshold = 80;

    private static (string BestMatch, int BestScore, string GradeType) FindBestMatch(Dictionary<string, CourseDetail> validationCourses, string subjectName, Dictionary<string, CourseDetail> json)
    {
        string bestMatch = null;
        int bestScore = 0;

        string bestMatchJson = null;
        int bestScoreJson = 0;

        string gradeType = "Betygsdokument 2011 eller senare";

        foreach (var validationCourse in validationCourses.Keys)
        {
            int score = Fuzz.Ratio(subjectName.ToLower(), validationCourse.ToLower());

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = validationCourse;
            }
        }


        if (bestScore < 100)
        {
            foreach (var jsonCourse in json.Keys)
            {
                int score = Fuzz.Ratio(subjectName.ToLower(), jsonCourse.ToLower());

                if (score > bestScoreJson)
                {
                    bestScoreJson = score;
                    bestMatchJson = jsonCourse;
                }
            }
        }

        if (bestScoreJson > bestScore)
        {
            bestScore = bestScoreJson;
            bestMatch = bestMatchJson;
            gradeType = "Betygsdokument före 2011";
        }

        if (bestScore >= matchThreshold)
        {
            Console.WriteLine($"Fuzzy match found! Original: '{subjectName}' => Matched with: '{bestMatch}' (Score: {bestScore})");
            return (bestMatch, bestScore, gradeType);
        }

        Console.WriteLine($"No valid match found for '{subjectName}' (Best score: {bestScore}) - Below match threshold of {matchThreshold}");
        return (null, bestScore, null);
    }

    public static int GetTotalPoints(Dictionary<string, CourseDetail> validationCourses, GraduationDocument graduationDocument , Dictionary<string, CourseDetail> json)
    {
        int totalPoints = 0;

        foreach (var subject in graduationDocument.Subjects)
        {
            var (bestMatch, bestScore, gradeType) = FindBestMatch(validationCourses, subject.SubjectName, json);
            subject.FuzzyMatchScore = bestScore;  // Set the fuzzy match score for the subject

            if (bestMatch != null && validationCourses.ContainsKey(bestMatch))
            {
                Console.WriteLine($"Adding points for subject '{subject.SubjectName}' with match '{bestMatch}' - Points: {validationCourses[bestMatch].Points}");

                // Update the subject details
                subject.GymnasiumPoints = validationCourses[bestMatch].Points.ToString();
                subject.CourseCode = validationCourses[bestMatch].CourseCode;

                totalPoints += validationCourses[bestMatch].Points ?? 0;
            }
            else if (bestMatch != null && json.ContainsKey(bestMatch))
            {
                Console.WriteLine($"Adding points for subject '{subject.SubjectName}' with match '{bestMatch}' - Points: {json[bestMatch].Points}");

                // Update the subject details
                subject.GymnasiumPoints = json[bestMatch].Points.ToString();
                subject.CourseCode = json[bestMatch].CourseCode;

                totalPoints += json[bestMatch].Points ?? 0;
            }
        }

        return totalPoints;
    }

    public static List<string> GetUnmatchedSubjects(Dictionary<string, CourseDetail> validationCourses, GraduationDocument graduationDocument, Dictionary<string, CourseDetail> json)
    {
        var unmatchedSubjects = new List<string>();

        foreach (var subject in graduationDocument.Subjects)
        {
            var (bestMatch, bestScore, gradeType) = FindBestMatch(validationCourses, subject.SubjectName, json);
            subject.FuzzyMatchScore = bestScore;  // Set the fuzzy match score for the subject

            if (bestMatch == null)
            {
                unmatchedSubjects.Add(subject.SubjectName);
                Console.WriteLine($"Unmatched subject: '{subject.SubjectName}'");
            }
        }

        return unmatchedSubjects;
    }

    public static GraduationDocument UpdateMatchedSubjects(Dictionary<string, CourseDetail> validationCourses, GraduationDocument graduationDocument, Dictionary<string, CourseDetail> json)
    {
        foreach (var subject in graduationDocument.Subjects)
        {
            var (bestMatch, bestScore,gradeType) = FindBestMatch(validationCourses, subject.SubjectName, json);
            graduationDocument.Curriculum = gradeType; 

            subject.FuzzyMatchScore = bestScore;  // Set the fuzzy match score for the subject

            if (bestMatch != null && validationCourses.ContainsKey(bestMatch))
            {
                var validationCourse = validationCourses[bestMatch];

                subject.SubjectName = bestMatch;
                subject.GymnasiumPoints = validationCourse.Points.ToString();
                subject.CourseCode = validationCourse.CourseCode;
            }
            else if(bestMatch != null && json.ContainsKey(bestMatch))
            {
                var jsonCourse = json[bestMatch];

                subject.SubjectName = bestMatch;
                subject.GymnasiumPoints = jsonCourse.Points.ToString();
                subject.CourseCode = jsonCourse.CourseCode;
            }
        }
        return graduationDocument;
    }
    public static GraduationDocument CompareCourses(Dictionary<string, CourseDetail> validationCourses, GraduationDocument graduationDocument, Dictionary<string, CourseDetail> json)
    {
        int totalPoints = GetTotalPoints(validationCourses, graduationDocument, json);
        List<string> unmatchedSubjects = GetUnmatchedSubjects(validationCourses, graduationDocument, json);
        var updatedDocument = UpdateMatchedSubjects(validationCourses, graduationDocument, json);

        Console.WriteLine($"Total Points of Matched Courses: {totalPoints}");
        Console.WriteLine("Subjects that did not match any courses:");

        foreach (var subject in unmatchedSubjects)
        {
            Console.WriteLine(subject);
        }

        return updatedDocument;
    }
}