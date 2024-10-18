using Interpret_grading_documents.Controllers;
using Interpret_grading_documents.Services;

namespace Interpret_grading_documents.Models;

public class RequirementCheckViewModel
{
    public GPTService.GraduationDocument Document { get; set; }
    public Dictionary<string, RequirementResult> RequirementResults { get; set; }
    public bool MeetsRequirement { get; set; }
}