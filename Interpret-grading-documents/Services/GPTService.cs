﻿using OpenAI.Chat;
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

            public ImageReliabilityResult ImageReliability { get; set; }
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
            public string? GymnasiumPoints { get; set; }
        }

        public GPTService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = Environment.GetEnvironmentVariable("GPT_API_KEY");

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API key for GPT is not set in the environment variables.");
            }
        }

        public async Task<GraduationDocument> ProcessTextPrompt(IFormFile uploadedFile)
        {
            string tempFilePath = await SaveUploadedFileAsync(uploadedFile);
            string processedImagePath = null;
            string contentType;

            try
            {
                (processedImagePath, contentType) = await ProcessUploadedFileAsync(tempFilePath);
                ImageReliabilityResult reliabilityResult = CheckImageReliability(processedImagePath);

                ChatClient client = InitializeChatClient();

                byte[] imageBytes = await File.ReadAllBytesAsync(processedImagePath);
                BinaryData binaryImageData = BinaryData.FromBytes(imageBytes);

                List<ChatMessage> messages = PrepareChatMessages(binaryImageData, contentType);

                ChatCompletion chatCompletion = await GetChatCompletionAsync(client, messages);
                GraduationDocument document = DeserializeResponse(chatCompletion.Content[0].Text);

                var updatedDocument = CompareCourses(document);
                ValidateDocument(updatedDocument, reliabilityResult);

                return updatedDocument;
            }
            finally
            {
                CleanUpTempFiles(tempFilePath, processedImagePath); 
            }
        }


        #region Private Methods

        private async Task<string> SaveUploadedFileAsync(IFormFile uploadedFile)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(uploadedFile.FileName).ToLower()}");
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(stream);
            }
            return tempFilePath;
        }

        private async Task<(string processedImagePath, string contentType)> ProcessUploadedFileAsync(string tempFilePath)
        {
            string extension = Path.GetExtension(tempFilePath).ToLower();
            string processedImagePath = tempFilePath;
            string contentType;

            if (extension == ".pdf")
            {
                processedImagePath = await ConvertPdfToJpgAsync(tempFilePath);
                contentType = "image/jpeg";
            }
            else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                contentType = GetImageContentType(extension);
            }
            else
            {
                throw new NotSupportedException("Unsupported file type. Please provide a PDF, JPG, or PNG file.");
            }

            return (processedImagePath, contentType);
        }

        private async Task<string> ConvertPdfToJpgAsync(string pdfPath)
        {
            string jpgPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

            using (var images = new MagickImageCollection())
            {
                images.Read(pdfPath);
                if (images.Count > 0)
                {
                    images[0].Format = MagickFormat.Jpeg;
                    await Task.Run(() => images[0].Write(jpgPath));
                }
                else
                {
                    throw new InvalidOperationException("No images found in PDF.");
                }
            }

            return jpgPath;
        }

        private string GetImageContentType(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => throw new NotSupportedException("Unsupported image format.")
            };
        }

        private ImageReliabilityResult CheckImageReliability(string imagePath)
        {
            var checker = new ImageReliabilityChecker();
            try
            {
                return checker.CheckImageReliability(imagePath);
            }
            catch (Exception)
            {
                return new ImageReliabilityResult { ReliabilityScore = 0, FileFormat = "Unknown" };
            }
        }

        private ChatClient InitializeChatClient()
        {
            return new ChatClient("gpt-4o-mini", _apiKey);
        }

        private List<ChatMessage> PrepareChatMessages(BinaryData imageData, string contentType)
        {
            return new List<ChatMessage>
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
                        "   - Betyg\n" +
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
                        "           'grade': 'Betyg',\n" +
                        "       },\n" +
                        "       ... fler ämnen\n" +
                        "   ]\n" +
                        "}"),
                    ChatMessageContentPart.CreateImagePart(imageData, contentType)
                )
            };
        }

        private async Task<ChatCompletion> GetChatCompletionAsync(ChatClient client, List<ChatMessage> messages)
        {
            var completionOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
            };
            return await client.CompleteChatAsync(messages, completionOptions);
        }

        private GraduationDocument DeserializeResponse(string jsonResponse)
        {
            return JsonSerializer.Deserialize<GraduationDocument>(jsonResponse);
        }

        private GraduationDocument CompareCourses(GraduationDocument document)
        {
            string validationJsonPath = Path.Combine("Data", "kurser.json");
            CourseComparator courseComparator = new CourseComparator(document, validationJsonPath);
            var updatedDocument = courseComparator.CompareCourses();

            return updatedDocument;
        }

        private void ValidateDocument(GraduationDocument document, ImageReliabilityResult reliabilityResult)
        {
            var checker = new ImageReliabilityChecker();
            bool isDataValid = checker.ValidateData(document);

            if (!isDataValid)
            {
                reliabilityResult.ReliabilityScore = 0;
            }

            document.ImageReliability = reliabilityResult;
        }

        private void CleanUpTempFiles(string tempFilePath, string processedImagePath)
        {
            try
            {
                File.Delete(tempFilePath);
                if (!tempFilePath.Equals(processedImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(processedImagePath);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Kunde inte ta bort temporärfilen: {ex.Message}");
            }
        }

        #endregion
    }
}