using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.FormRecognizer.Models;
using System.IO;
using System.Threading.Tasks;

namespace Interpret_grading_documents.Services
{
    public class FormRecognizerService
    {
        private readonly DocumentAnalysisClient _client;

        public FormRecognizerService(string endpoint, string apiKey)
        {
            var credential = new AzureKeyCredential(apiKey);
            _client = new DocumentAnalysisClient(new Uri(endpoint), credential);
        }

        public async Task<string> AnalyzeDocumentAsync(string imagePath)
        {
            using var stream = new FileStream(imagePath, FileMode.Open);

            // Updated method call with required parameters
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", stream);
            var result = operation.Value;

            string extractedText = string.Empty;

            foreach (var page in result.Pages)
            {
                extractedText += $"--- Page {page.PageNumber} ---\n";
                foreach (var line in page.Lines)
                {
                    extractedText += $"{line.Content}\n"; // Extract text directly from lines
                }
            }

            return extractedText;
        }
    }
}
