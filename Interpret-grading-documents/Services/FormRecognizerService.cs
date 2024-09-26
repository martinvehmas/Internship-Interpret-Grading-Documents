using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

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

            foreach (var table in result.Tables)
            {
                foreach (var cell in table.Cells)
                {
                    if (cell.ColumnIndex == 0)
                    {
                        var key = cell.Content;

                        if (!keyValuePairs.ContainsKey(key))
                        {
                            var values = new List<string>();

                            for (int colIndex = 1; colIndex < table.ColumnCount; colIndex++)
                            {
                                var value = table.Cells.FirstOrDefault(c => c.RowIndex == cell.RowIndex && c.ColumnIndex == colIndex)?.Content;
                                if (!string.IsNullOrEmpty(value))
                                {
                                    values.Add(value);
                                }
                            }

                            if (!string.IsNullOrEmpty(key) && values.Count > 0)
                            {
                                keyValuePairs[key] = values;
                            }
                        }
                    }
                }
            }

            return keyValuePairs;
        }
    }
}
