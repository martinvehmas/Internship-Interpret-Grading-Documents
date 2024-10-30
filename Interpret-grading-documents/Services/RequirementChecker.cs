using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Interpret_grading_documents.Controllers;
using Interpret_grading_documents.Models; // Add this using statement

namespace Interpret_grading_documents.Services
{
    public class CourseEquivalents
    {
        [JsonPropertyName("subjects")]
        public List<Subject> Subjects { get; set; }
    }

    public class Subject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("courses")]
        public List<Course> Courses { get; set; }
    }

    public class Course
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("alternatives")]
        public List<AlternativeCourse> Alternatives { get; set; } = new List<AlternativeCourse>();

        [JsonPropertyName("requiredGrade")]
        public string RequiredGrade { get; set; }

        [JsonPropertyName("includeInAverage")]
        public bool IncludeInAverage { get; set; }
    }

    public class AlternativeCourse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    // Removed RequirementResult class from here

    public static class RequirementChecker
    {
        private static readonly Dictionary<string, double> GradeMappings = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", 20 },
            { "B", 17.5 },
            { "C", 15 },
            { "D", 12.5 },
            { "E", 10 },
            { "F", 0 },

            { "MVG", 20 },
            { "VG", 15 },
            { "G", 10 },
            { "IG", 0 },

            { "5", 20 },
            { "4", 17.5 },
            { "3", 15 },
            { "2", 12.5 },
            { "1", 10 },
            { "0", 0 }
        };

        private static CourseEquivalents LoadCourseEquivalents(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("Course equivalents JSON file not found at " + jsonFilePath);
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<CourseEquivalents>(jsonContent);
        }

        private static List<CourseForAverage> LoadCoursesForAverage(string coursesForAverageFilePath)
        {
            if (System.IO.File.Exists(coursesForAverageFilePath))
            {
                var jsonContent = System.IO.File.ReadAllText(coursesForAverageFilePath);
                return JsonSerializer.Deserialize<List<CourseForAverage>>(jsonContent);
            }
            return null;
        }

        public static Dictionary<string, RequirementResult> DoesStudentMeetRequirement(GPTService.GraduationDocument document, string jsonFilePath)
        {
            var CourseEquivalents = LoadCourseEquivalents(jsonFilePath); // Load fresh data

            Console.WriteLine("Checking if the student meets all course requirements.");
            var allRequirementsMet = new Dictionary<string, RequirementResult>();

            foreach (var subject in CourseEquivalents.Subjects)
            {
                foreach (var course in subject.Courses)
                {
                    string requiredCourseNameOrCode = course.Name;
                    string requiredGrade = course.RequiredGrade;
                    int requiredLevel = course.Level; // Required course level

                    Console.WriteLine($"Checking requirement for course: {requiredCourseNameOrCode} with minimum grade: {requiredGrade}");

                    double requiredGradeValue = GetGradeValue(requiredGrade);
                    var equivalentCourses = GetEquivalentCourses(requiredCourseNameOrCode, jsonFilePath);

                    bool courseRequirementMet = false;
                    string highestGradeCourseName = null;
                    string highestGrade = null;
                    string originalCourseGrade = null;
                    double highestGradeValue = 0;
                    var otherAlternativeGrades = new List<string>();

                    foreach (var studentSubject in document.Subjects)
                    {
                        foreach (var equivalentCourse in equivalentCourses)
                        {
                            if (equivalentCourse.Name.Equals(studentSubject.SubjectName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                equivalentCourse.Code.Equals(studentSubject.CourseCode.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                double studentGradeValue = GetGradeValue(studentSubject.Grade.Trim());

                                if (requiredCourseNameOrCode.Equals(equivalentCourse.Name, StringComparison.OrdinalIgnoreCase) ||
                                    requiredCourseNameOrCode.Equals(equivalentCourse.Code, StringComparison.OrdinalIgnoreCase))
                                {
                                    originalCourseGrade = studentSubject.Grade;
                                }
                                else if (equivalentCourse.Level <= requiredLevel) // only include if it's the same or lower level
                                {
                                    otherAlternativeGrades.Add($"{studentSubject.SubjectName}: {studentSubject.Grade}");
                                }

                                if (studentGradeValue > highestGradeValue)
                                {
                                    highestGradeValue = studentGradeValue;
                                    highestGrade = studentSubject.Grade;
                                    highestGradeCourseName = studentSubject.SubjectName;
                                }

                                if (highestGradeValue >= requiredGradeValue)
                                {
                                    courseRequirementMet = true;
                                }
                            }
                        }
                    }

                    if (!courseRequirementMet)
                    {
                        Console.WriteLine($"Student does not meet the requirement for {requiredCourseNameOrCode}");
                    }

                    allRequirementsMet[requiredCourseNameOrCode] = new RequirementResult
                    {
                        CourseName = highestGradeCourseName ?? requiredCourseNameOrCode,
                        RequiredGrade = requiredGrade,
                        IsMet = courseRequirementMet,
                        StudentGrade = highestGrade ?? "N/A",
                        OriginalCourseGrade = originalCourseGrade ?? "N/A",
                        AlternativeCourseGrade = highestGradeCourseName != requiredCourseNameOrCode ? highestGrade : null,
                        OtherGradesInAlternatives = otherAlternativeGrades
                    };
                }
            }

            return allRequirementsMet;
        }


        public static double GetGradeValue(string grade)
        {
            if (GradeMappings.TryGetValue(grade.Trim().ToUpper(), out double value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        private static List<Course> GetEquivalentCourses(string courseNameOrCode, string jsonFilePath)
        {
            var CourseEquivalents = LoadCourseEquivalents(jsonFilePath); // Load fresh data
            var equivalentCourses = new List<Course>();

            if (CourseEquivalents != null && CourseEquivalents.Subjects != null)
            {
                foreach (var subject in CourseEquivalents.Subjects)
                {
                    // Try to find the required course in this subject
                    var requiredCourse = subject.Courses.FirstOrDefault(c =>
                        c.Name.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        c.Code.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        (c.Alternatives != null && c.Alternatives.Any(a =>
                            a.Name.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                            a.Code.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                    );

                    if (requiredCourse != null)
                    {
                        int requiredLevel = requiredCourse.Level;

                        // Get all courses in this subject with level >= requiredLevel
                        var higherLevelCourses = subject.Courses.Where(c => c.Level >= requiredLevel);

                        foreach (var course in higherLevelCourses)
                        {
                            equivalentCourses.Add(new Course
                            {
                                Name = course.Name,
                                Code = course.Code,
                                Level = course.Level,
                                Alternatives = null
                            });

                            if (course.Alternatives != null)
                            {
                                foreach (var alt in course.Alternatives)
                                {
                                    equivalentCourses.Add(new Course
                                    {
                                        Name = alt.Name,
                                        Code = alt.Code,
                                        Level = course.Level,
                                        Alternatives = null
                                    });
                                }
                            }
                        }
                        break;
                    }
                }
            }

            if (equivalentCourses.Count == 0)
            {
                Console.WriteLine($"No equivalent courses found for {courseNameOrCode}, adding the original course as its own equivalent");
                equivalentCourses.Add(new Course { Name = courseNameOrCode, Code = courseNameOrCode, Level = 0 });
            }

            return equivalentCourses;
        }

        public static double CalculateAverageGrade(GPTService.GraduationDocument document, string jsonFilePath)
        {
            var CourseEquivalents = LoadCourseEquivalents(jsonFilePath); // Load fresh data
            double totalWeightedGradePoints = 0;
            int totalCoursePoints = 0;

            foreach (var subject in CourseEquivalents.Subjects)
            {
                foreach (var course in subject.Courses)
                {
                    if (course.IncludeInAverage)
                    {
                        var equivalentCourses = GetEquivalentCourses(course.Name, jsonFilePath);

                        foreach (var studentSubject in document.Subjects)
                        {
                            if (equivalentCourses.Any(ec =>
                                    ec.Name.Equals(studentSubject.SubjectName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                    ec.Code.Equals(studentSubject.CourseCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                            {
                                double studentGradeValue = GetGradeValue(studentSubject.Grade.Trim());
                                int studentCoursePoints = int.Parse(studentSubject.GymnasiumPoints);

                                totalWeightedGradePoints += studentGradeValue * studentCoursePoints;
                                totalCoursePoints += studentCoursePoints;
                                Console.WriteLine($"Course Name: {studentSubject.SubjectName}");
                                Console.WriteLine($"Grade: {studentSubject.Grade}");
                                Console.WriteLine($"Points: {studentSubject.GymnasiumPoints}");
                                Console.WriteLine($"GradeValue: {studentGradeValue}");

                                Console.WriteLine($"{studentGradeValue} * {studentSubject.GymnasiumPoints} = {totalWeightedGradePoints} ");

                                break; // Move to the next course after a match
                            }
                        }
                    }
                }
            }

            if (totalCoursePoints == 0)
                return 0;

            double average = totalWeightedGradePoints / totalCoursePoints;
            Console.WriteLine($"Average points: {Math.Round(average, 2)}");

            return Math.Round(average, 2); // Round to 2 decimal places if desired
        }

        public static Dictionary<string, MeritPointResult> GetCourseMeritPoints(GPTService.GraduationDocument document, string jsonFilePathForAverage)
        {
            var courseMeritPoints = new Dictionary<string, MeritPointResult>();
            var coursesForAverage = LoadCoursesForAverage(jsonFilePathForAverage);
            if (coursesForAverage == null) return courseMeritPoints;

            foreach (var courseForAverage in coursesForAverage)
            {
                // Create a list of equivalent course codes (original + alternatives)
                var equivalentCourses = new List<string> { courseForAverage.Code };
                equivalentCourses.AddRange(courseForAverage.AlternativeCourses.Select(alt => alt.Code));

                foreach (var studentSubject in document.Subjects)
                {
                    // Check if the student's course code matches any equivalent course code
                    if (equivalentCourses.Contains(studentSubject.CourseCode.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        double studentGradeValue = RequirementChecker.GetGradeValue(studentSubject.Grade.Trim());
                        int studentCoursePoints = int.Parse(studentSubject.GymnasiumPoints);
                        double meritPoints = studentGradeValue;

                        // Create a MeritPointResult object for the course
                        var meritPointResult = new MeritPointResult
                        {
                             CourseName = studentSubject.SubjectName,
                             StudentGrade = studentSubject.Grade,
                             MeritPoint = meritPoints,
                             OriginalCourseGrade = courseForAverage.Name,
                             AlternativeCourseGrade = courseForAverage.AlternativeCourses.FirstOrDefault()?.Name,
                             OtherGradesInAlternatives = courseForAverage.AlternativeCourses.Select(alt => alt.Name).ToList()
                        };

                        // Add the course result to the dictionary
                        courseMeritPoints[studentSubject.SubjectName] = meritPointResult;
                        break; // Move to the next course after a match
                    }
                }
            }

            return courseMeritPoints;
        }
    }
}
