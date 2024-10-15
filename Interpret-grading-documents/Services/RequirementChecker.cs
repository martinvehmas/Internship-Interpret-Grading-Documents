using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Interpret_grading_documents.Services
{
    public class CourseEquivalent
    {
        [JsonPropertyName("group_name")]
        public string GroupName { get; set; }

        [JsonPropertyName("courses")]
        public List<Course> Courses { get; set; }
    }

    public class Course
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public static class RequirementChecker
    {
        private static List<CourseEquivalent> CourseEquivalents = LoadCourseEquivalents();

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

        private static List<CourseEquivalent> LoadCourseEquivalents()
        {
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CourseEquivalents.json");
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("Course equivalents JSON file not found.");
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<List<CourseEquivalent>>(jsonContent);
        }

        public static bool DoesStudentMeetRequirement(GPTService.GraduationDocument document, string requiredCourseNameOrCode, string requiredMinimumGrade)
        {
            Console.WriteLine($"Checking if student meets the requirement for {requiredCourseNameOrCode} with minimum grade {requiredMinimumGrade}");

            int requiredGradeValue = GetGradeValue(requiredMinimumGrade);
            Console.WriteLine($"Required grade value for {requiredMinimumGrade}: {requiredGradeValue}");

            var equivalentCourses = GetEquivalentCourses(requiredCourseNameOrCode);
            Console.WriteLine($"Equivalent courses for {requiredCourseNameOrCode}: {string.Join(", ", equivalentCourses.Select(c => c.Name + " (" + c.Code + ")"))}");

            foreach (var studentSubject in document.Subjects)
            {
                Console.WriteLine($"Checking student's subject: {studentSubject.SubjectName.Trim()} ({studentSubject.CourseCode.Trim()}) with grade {studentSubject.Grade}");

                if (equivalentCourses.Any(ec =>
                    ec.Name.Equals(studentSubject.SubjectName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    ec.Code.Equals(studentSubject.CourseCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    int studentGradeValue = GetGradeValue(studentSubject.Grade.Trim());
                    Console.WriteLine($"Student's grade value for {studentSubject.SubjectName.Trim()}: {studentGradeValue}");

                    if (studentGradeValue >= requiredGradeValue)
                    {
                        Console.WriteLine($"Student meets the requirement for {requiredCourseNameOrCode} with grade {studentSubject.Grade}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Student's grade {studentSubject.Grade} is lower than the required grade {requiredMinimumGrade}");
                    }
                }
                else
                {
                    Console.WriteLine($"No match found for student's subject {studentSubject.SubjectName.Trim()} in equivalent courses.");
                }
            }

            Console.WriteLine($"Student does not meet the requirement for {requiredCourseNameOrCode}");
            return false;
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


            if (CourseEquivalents != null && CourseEquivalents.Count > 0)
            {
                // Check all groups and their names
                foreach (var group in CourseEquivalents)
                {
                    if (group?.GroupName != null && group.GroupName.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (group.Courses != null)
                        {
                            equivalentCourses.AddRange(group.Courses);
                        }
                        break;
                    }
                }

                if (equivalentCourses.Count == 0)
                {
                    // Check within the courses for name/code matches
                    foreach (var group in CourseEquivalents)
                    {
                        if (group?.Courses != null)
                        {
                            foreach (var course in group.Courses)
                            {
                                if (course.Name.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                    course.Code.Equals(courseNameOrCode.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    equivalentCourses.Add(course);
                                }
                            }
                        }
                    }
                }
            }

            if (equivalentCourses.Count == 0)
            {
                Console.WriteLine($"No equivalent courses found for {courseNameOrCode}, adding the original course as its own equivalent");
                equivalentCourses.Add(new Course { Name = courseNameOrCode, Code = courseNameOrCode });
            }

            return equivalentCourses;
        }
    }
}
