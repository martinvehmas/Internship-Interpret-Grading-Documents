using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Interpret_grading_documents.Controllers;
using Interpret_grading_documents.Models;

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
        public string RequiredGrade { get; set; } // New property for required grade
    }

    public class AlternativeCourse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public static class RequirementChecker
    {
        private static CourseEquivalents CourseEquivalents = LoadCourseEquivalents();

        private static readonly Dictionary<string, int> GradeMappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", 5 },
            { "B", 4 },
            { "C", 3 },
            { "D", 2 },
            { "E", 1 },
            { "F", 0 },

            { "MVG", 5 },
            { "VG", 3 },
            { "G", 1 },
            { "IG", 0 },

            { "5", 5 },
            { "4", 4 },
            { "3", 3 },
            { "2", 2 },
            { "1", 1 },
            { "0", 0 }
        };

        private static CourseEquivalents LoadCourseEquivalents()
        {
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CourseEquivalents.json");
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("Course equivalents JSON file not found.");
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<CourseEquivalents>(jsonContent);
        }

        public static Dictionary<string, RequirementResult> DoesStudentMeetRequirement(GPTService.GraduationDocument document)
        {
            Console.WriteLine("Checking if the student meets all course requirements.");
            var allRequirementsMet = new Dictionary<string, RequirementResult>();

            foreach (var subject in CourseEquivalents.Subjects)
            {
                foreach (var course in subject.Courses)
                {
                    string requiredCourseNameOrCode = course.Name;
                    string requiredGrade = course.RequiredGrade;

                    Console.WriteLine($"Checking requirement for course: {requiredCourseNameOrCode} with minimum grade: {requiredGrade}");

                    int requiredGradeValue = GetGradeValue(requiredGrade);
                    var equivalentCourses = GetEquivalentCourses(requiredCourseNameOrCode);

                    bool courseRequirementMet = false;
                    string highestGradeCourseName = null;
                    string highestGrade = null;
                    string originalCourseGrade = null;
                    int highestGradeValue = 0;
                    var otherAlternativeGrades = new List<string>();

                    foreach (var studentSubject in document.Subjects)
                    {
                        foreach (var equivalentCourse in equivalentCourses)
                        {
                            if (equivalentCourse.Name.Equals(studentSubject.SubjectName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                equivalentCourse.Code.Equals(studentSubject.CourseCode.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                int studentGradeValue = GetGradeValue(studentSubject.Grade.Trim());

                                if (requiredCourseNameOrCode.Equals(equivalentCourse.Name, StringComparison.OrdinalIgnoreCase) ||
                                    requiredCourseNameOrCode.Equals(equivalentCourse.Code, StringComparison.OrdinalIgnoreCase))
                                {
                                    originalCourseGrade = studentSubject.Grade;
                                }
                                else
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

        private static int GetGradeValue(string grade)
        {
            if (GradeMappings.TryGetValue(grade.Trim().ToUpper(), out int value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        private static List<Course> GetEquivalentCourses(string courseNameOrCode)
        {
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
    }
}
