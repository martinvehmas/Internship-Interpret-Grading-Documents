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

        public async Task<Dictionary<string, string>> AnalyzeDocumentForKeyValuesAsync(string imagePath)
        {
            using var stream = new FileStream(imagePath, FileMode.Open);

            // Updated method call with required parameters
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
            var result = operation.Value;

            var keyValuePairs = new Dictionary<string, string>();

            foreach (var table in result.Tables)
            {
                //extractedText += $"--- Page {page.PageNumber} ---\n";
                foreach (var cell in table.Cells)
                {
                    if (cell.ColumnIndex == 0)
                    {
                        var key = cell.Content;
                        var value = table.Cells.FirstOrDefault(c => c.RowIndex == cell.RowIndex && c.ColumnIndex == 1)?.Content;
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            keyValuePairs[key] = value;
                        }
                    }
                }
            }

            return keyValuePairs;
        }
    }
}
