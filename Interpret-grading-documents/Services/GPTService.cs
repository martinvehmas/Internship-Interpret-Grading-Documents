using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI.Chat;

namespace Interpret_grading_documents.Services
{
    public class GPTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

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

        public async Task<string> ProcessTextPrompt()
        {
            ChatClient client = new("gpt-4o-mini", _apiKey);

            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", "SlutBetyg_02.png");
            using Stream imageStream = File.OpenRead(imagePath);
            BinaryData imageBytes = BinaryData.FromStream(imageStream);

            List<ChatMessage> messages = [
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Vänligen extrahera följande data från bilden, svara endast med JSON, formatera det inte med ` eller liknande:"),
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


            // made this call async
            ChatCompletion chatCompletion = await client.CompleteChatAsync(messages);

            return chatCompletion.Content[0].Text;
        }
    }
}
