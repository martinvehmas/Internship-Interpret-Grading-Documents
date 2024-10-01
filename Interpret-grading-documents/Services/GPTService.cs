using Interpret_grading_documents.Services;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageMagick;

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

            public string ImageReliability { get; set; }
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
            var inputFileName = "SlutBetyg_01.png";
            var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", inputFileName);
            string extension = Path.GetExtension(inputPath).ToLower();
            string processedImagePath = inputPath; // Initialize with original path
            string contentType;

            if (extension == ".pdf")
            {
                processedImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

                using (var images = new MagickImageCollection())
                {
                    images.Read(inputPath);
                    if (images.Count > 0)
                    {
                        images[0].Format = MagickFormat.Jpeg;
                        images[0].Write(processedImagePath);
                        contentType = "image/jpeg";
                    }
                    else
                    {
                        throw new InvalidOperationException("No images found in PDF.");
                    }
                }
            }
            else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                processedImagePath = inputPath;

                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                        contentType = "image/jpeg";
                        break;
                    case ".png":
                        contentType = "image/png";
                        break;
                    default:
                        throw new NotSupportedException("Unsupported image format.");
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported file type. Please provide a PDF, JPG, or PNG file.");
            }

            var checker = new ImageReliabilityChecker();
            string reliabilityResult;
            try
            {
                reliabilityResult = checker.CheckImageReliability(processedImagePath).ToString();
            }
            catch (Exception ex)
            {
                reliabilityResult = $"An error occurred: {ex.Message}";
            }

            ChatClient client = new("gpt-4o-mini", _apiKey);

            byte[] imageBytes = await File.ReadAllBytesAsync(processedImagePath);
            BinaryData binaryImageData = BinaryData.FromBytes(imageBytes);

            List<ChatMessage> messages = new List<ChatMessage>
    {
        new UserChatMessage(
            ChatMessageContentPart.CreateTextPart("Vänligen extrahera följande data från bilden, svara endast med JSON, formatera det inte med <pre> eller liknande. Säkerställ att alla betygen är korrekta och överenstämmer med deras ämne."),
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
                "   'personal_id': 'xxxxxx-xxxx',\n" +
                "   'graduation_date': 'ÅÅÅÅ-MM-DD',\n" +
                "   'school_name': 'Skolans Namn',\n" +
                "   'program_name': 'Programnamn',\n" +
                "   'specialization': 'Specialisering',\n" +
                "   'subjects': [\n" +
                "       {\n" +
                "           'subject_name': 'Ämnesnamn',\n" +
                "           'course_code': 'Kurskod',\n" +
                "           'grade': 'Betyg',\n" +
                "           'points': 'Poäng'\n" +
                "       },\n" +
                "       ... fler ämnen\n" +
                "   ]\n" +
                "}"
            ),
            ChatMessageContentPart.CreateImagePart(binaryImageData, contentType))
    };

            var completionOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
            };

            ChatCompletion chatCompletion = await client.CompleteChatAsync(messages, completionOptions);

            var jsonResponse = chatCompletion.Content[0].Text;

            GraduationDocument document = JsonSerializer.Deserialize<GraduationDocument>(jsonResponse);

            var isPersonalIdValid = checker.PersonalIdChecker(document);

            // Add reliability score to 0 if personal ID is invalid
            if (!isPersonalIdValid)
            {
                document.ImageReliability = $"Reliability Score: 0.0\n" +
                                            $"Betygsdokumentet är inte tillförlitligt, personnummer finns ej eller stämmer inte\n";
                                            
                return document;
            }

            document.ImageReliability = reliabilityResult;

            if (extension == ".pdf")
            {
                try
                {
                    File.Delete(processedImagePath);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Kunde inte ta bort temporärfilen: {ex.Message}");
                }
            }

            return document;
        }
    }
}