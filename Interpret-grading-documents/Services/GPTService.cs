using Interpret_grading_documents.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI.Assistants;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Files;
using System.ClientModel;
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
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", "examensbevis-gymnasieskola-yrkes-el.pdf");
            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension == ".pdf")
            {
                // Assistants is a beta API and subject to change; acknowledge its experimental status by suppressing the matching warning.
                #pragma warning disable OPENAI001
                OpenAIClient openAIClient = new(_apiKey);
                FileClient fileClient = openAIClient.GetFileClient();
                AssistantClient assistantClient = openAIClient.GetAssistantClient();

                OpenAIFile pdfFile = fileClient.UploadFile(filePath,
                    FileUploadPurpose.Assistants);

                AssistantCreationOptions assistantOptions = new()
                {
                Name = "PDF-GPT",
                Instructions =
                "You are an assistant that extracts data from this PDF-file"
                    + " on user queries. Extract only the data that the user is asking about from the PDF-file."
                    + " Please make sure to format the response: 'full_name': 'Full Name', 'personal_id': 'xxxxxx-xxxx'",
                Tools =
                {
                    new FileSearchToolDefinition(),
                    new CodeInterpreterToolDefinition(),
                },
                ToolResources = new()
                {
                FileSearch = new()
                {
                NewVectorStores =
                {
                    new VectorStoreCreationHelper([pdfFile.Id]),
                }
            }
        },
    };

    Assistant assistant = assistantClient.CreateAssistant("gpt-4o-mini", assistantOptions);

    ThreadCreationOptions threadOptions = new()
    {
        InitialMessages = { "What is the full name and the personal id say in the PDF-file?" }
    };

    ThreadRun threadRun = assistantClient.CreateThreadAndRun(assistant.Id, threadOptions);

    do
    {
        Thread.Sleep(TimeSpan.FromSeconds(1));
        threadRun = assistantClient.GetRun(threadRun.ThreadId, threadRun.Id);
    } while (!threadRun.Status.IsTerminal);

    CollectionResult<ThreadMessage> messages
        = assistantClient.GetMessages(threadRun.ThreadId, new MessageCollectionOptions() { Order = MessageCollectionOrder.Ascending });

    foreach (ThreadMessage message in messages)
    {
        Console.Write($"[{message.Role.ToString().ToUpper()}]: ");
        foreach (MessageContent contentItem in message.Content)
        {
            if (!string.IsNullOrEmpty(contentItem.Text))
            {
                Console.WriteLine($"{contentItem.Text}");

                if (contentItem.TextAnnotations.Count > 0)
                {
                    Console.WriteLine();
                }

            }
        }
        Console.WriteLine();
        }
        #pragma warning disable OPENAI001
            }
            else if (fileExtension == ".png" || fileExtension == ".jpg" || fileExtension == ".jpeg")
            {
                // Process Image
                var checker = new ImageReliabilityChecker();
            try
            {
                var result = checker.CheckImageReliability(filePath);
                Console.WriteLine(result.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ett fel uppstod: {ex.Message}");
            }

            ChatClient client = new("gpt-4o-mini", _apiKey);
            using Stream imageStream = File.OpenRead(filePath);

            

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
                    ChatMessageContentPart.CreateImagePart(imageBytes, $"image/png"))
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
            else
            {
                throw new InvalidOperationException("Unsupported file type.");
            }
            return null;
        }
    }
}
