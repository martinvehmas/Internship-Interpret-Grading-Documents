using System.Reflection.Metadata;

namespace Interpret_grading_documents.Services;

public static class DegreeChecker
{
    public static bool IsValidExamCertificate(GPTService.GraduationDocument document)
    {
        
        if (!document.Title.Contains("Examensbevis", StringComparison.OrdinalIgnoreCase) ||
            document.Title.Contains("Gymnasieexamen", StringComparison.OrdinalIgnoreCase))
            return false;

        
        if (!(document.SchoolForm.Contains("Gymnasieskola", StringComparison.OrdinalIgnoreCase) ||
              document.SchoolForm.Contains("Kommunal vuxenutbildning", StringComparison.OrdinalIgnoreCase)||
              document.SchoolForm.Contains("Komvux utbildning"))) 
            return false;

        return true;
    }

   
}