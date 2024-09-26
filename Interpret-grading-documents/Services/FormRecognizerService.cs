using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Text.RegularExpressions;

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
        
        public async Task<Dictionary<string, List<string>>> AnalyzeDocumentForKeyValuesAsync(string imagePath)
        {
            using var stream = new FileStream(imagePath, FileMode.Open);

            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
            var result = operation.Value;

            var keyValuePairs = new Dictionary<string, List<string>>();

            // Define the keys to filter using HashSet
            var targetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "efternamn, tilltalsnamn",
                "personnummer",
                "program",
                "programmets omfattning"
            };

            foreach (var documentField in result.KeyValuePairs)
            {
                var key = documentField.Key?.Content;
                var value = documentField.Value?.Content;

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {

                    // Use HashSet to check if the key is one of the target keys
                    if (targetKeys.Contains(key))
                    {

                        // avoid duplicate keys
                        if (!keyValuePairs.ContainsKey(key))
                        {
                            keyValuePairs[key] = new List<string> { value };
                        }
                    }
                }
            }

            var courseCodePattern = new Regex(@"^[A-Z]{2,6}\d{2}$");
            foreach (var table in result.Tables)
            {
                foreach (var row in table.Cells.GroupBy(cell => cell.RowIndex))
                {
                    string courseCode = null;
                    string courseName = null;
                    var rowValues = new List<string>();

                    foreach (var cell in row)
                    {
                        var content = cell.Content.Trim();

                        if (string.IsNullOrEmpty(content)) continue;

                        if (courseCodePattern.IsMatch(content))
                        {
                            courseCode = content;
                        }
                        else
                        {
                            if (courseName == null)
                            {
                                courseName = content;
                            }
                        }

                        rowValues.Add(content);
                    }

                    if (!string.IsNullOrEmpty(courseCode) && rowValues.Count >= 3 && !string.IsNullOrEmpty(courseName))
                    {
                        var key = courseName + "(COURSE DATA)";

                        if (!keyValuePairs.ContainsKey(key))
                        {
                            keyValuePairs[key] = rowValues;
                        }
                    }
                }
            }


            return keyValuePairs;
        }
    }
}
