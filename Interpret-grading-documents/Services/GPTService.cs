using Interpret_grading_documents.Services;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Interpret_grading_documents.Services
{
    public class GPTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public class GraduationDocument
        {
            [JsonPropertyName("full_name")]
            public string FullName { get; set; }

            [JsonPropertyName("personal_id")]
            public string PersonalId { get; set; }

            [JsonPropertyName("graduation_date")]
            public string GraduationDate { get; set; }

            [JsonPropertyName("school_name")]
            public string SchoolName { get; set; }

            [JsonPropertyName("program_name")]
            public string ProgramName { get; set; }

            [JsonPropertyName("specialization")]
            public string Specialization { get; set; }

            [JsonPropertyName("subjects")]
            public List<Subject> Subjects { get; set; }
        }

        public class Subject
        {
            [JsonPropertyName("subject_name")]
            public string SubjectName { get; set; }

            [JsonPropertyName("course_code")]
            public string? CourseCode { get; set; }

            [JsonPropertyName("grade")]
            public string Grade { get; set; }

            [JsonPropertyName("points")]
            public string GymnasiumPoints { get; set; }
        }

        public GPTService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _apiKey = Environment.GetEnvironmentVariable("GPT_API_KEY");
            //_apiKey = "";

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API key for GPT is not set in the environment variables.");
            }
        }

        public async Task<GraduationDocument> ProcessTextPrompt()
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", "SlutBetyg_03.jpeg");

            var checker = new ImageReliabilityChecker();
            try
            {
                var result = checker.CheckImageReliability(imagePath);
                Console.WriteLine(result.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ett fel uppstod: {ex.Message}");
            }

            ChatClient client = new("gpt-4o-mini", _apiKey);
            using Stream imageStream = File.OpenRead(imagePath);

            

            BinaryData imageBytes = BinaryData.FromStream(imageStream);
            List<ChatMessage> messages = [
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Vänligen extrahera följande data från bilden, svara endast med JSON, formatera det inte med ` eller liknande. Säkerställ att alla betygen är korrekta och överenstämmer med deras ämne."),
                    ChatMessageContentPart.CreateTextPart(
                        "1. Fullständigt namn\n" +
                        "2. Personnummer\n" +
                        "3. Examensdatum\n" +
                        "4. Skolans namn\n" +
                        "5. Programnamn\n" +
                        "6. Specialisering och detaljer om utbildning\n" +
                        "7. Lista över ämnen med följande detaljer:\n" +
                        "   - Ämnesnamn\n" +
                        "   - Kurskod\n" +
                        "   - Betyg\n" +
                        "   - Poäng\n" +
                        "Vänligen se till att formatera svaret i JSON-format som detta:\n" +
                        "{\n" +
                        "   'full_name': 'Fullständigt Namn',\n" +
                        "   'personal_id': 'xxxxxx-xxxx'\n" +
                        "   'graduation_date': 'ÅÅÅÅ-MM-DD',\n" +
                        "   'school_name': 'Skolans Namn',\n" +
                        "   'program_name': 'Programnamn',\n" +
                        "   'specialization': 'Specialisering',\n" +
                        "   'subjects': [\n" +
                        "       {\n" +
                        "           'subject_name': 'Ämnesnamn',\n" +
                        "           'course_code': 'Kurskod',\n" +
                        "           'grade': 'Betyg',\n" +
                        "           'points': Poäng\n" +
                        "       },\n" +
                        "       ... fler ämnen\n" +
                        "   ]\n" +
                        "}"
                    ),
                    ChatMessageContentPart.CreateImagePart(imageBytes, "image/png"))
            ];

            var completionOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
            };

            ChatCompletion chatCompletion = await client.CompleteChatAsync(messages, completionOptions);

            
            var jsonResponse = chatCompletion.Content[0].Text;

            GraduationDocument document = JsonSerializer.Deserialize<GraduationDocument>(jsonResponse);

            return document;
        }
    }
}
