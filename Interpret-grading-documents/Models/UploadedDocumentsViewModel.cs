using Interpret_grading_documents.Services;

namespace Interpret_grading_documents.Models
{
    public class UploadedDocumentsViewModel
    {
        public List<GPTService.GraduationDocument> Documents { get; set; }
        public GPTService.GraduationDocument MergedDocument { get; set; }
    }
}
