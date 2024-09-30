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

            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleImages", "SlutBetyg_01.png");
            using Stream imageStream = File.OpenRead(imagePath);
            BinaryData imageBytes = BinaryData.FromStream(imageStream);

            List<ChatMessage> messages = [
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Please extract the following data from this image, only respond with plain JSON, do not format it with ` or similar:"),
                    ChatMessageContentPart.CreateTextPart(
                        "1. Full name\n" +
                        "2. Personal identification number\n" +
                        "3. Graduation date\n" +
                        "4. School name\n" +
                        "5. Program name\n" +
                        "6. Specialization and vocational education details\n" +
                        "7. List of subjects with the following details:\n" +
                        "   - Subject name\n" +
                        "   - Course code\n" +
                        "   - Grade\n" +
                        "   - Gymnasium points\n" +
                        "Please make sure to format the output in JSON format like this:\n" +
                        "{\n" +
                        "   'full_name': 'Full Name',\n" +
                        "   'personal_id': 'xxxxxx-xxxx'\n" +
                        "   'graduation_date': 'YYYY-MM-DD',\n" +
                        "   'school_name': 'School Name',\n" +
                        "   'program_name': 'Program Name',\n" +
                        "   'specialization': 'Specialization',\n" +
                        "   'subjects': [\n" +
                        "       {\n" +
                        "           'subject_name': 'Subject',\n" +
                        "           'course_code': 'Code',\n" +
                        "           'grade': 'Grade',\n" +
                        "           'gymnasium_points': Points\n" +
                        "       },\n" +
                        "       ... more subjects\n" +
                        "   ]\n" +
                        "}"
                    ),
                    ChatMessageContentPart.CreateImagePart(imageBytes, "image/png"))
            ];

            ChatCompletion chatCompletion = client.CompleteChat(messages);

            return chatCompletion.Content[0].Text;
        }
    }
}
