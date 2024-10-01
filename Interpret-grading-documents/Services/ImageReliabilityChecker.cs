using System;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using OpenCvSharp;
using static Interpret_grading_documents.Services.GPTService;

namespace Interpret_grading_documents.Services
{
    public class ImageReliabilityResult
    {
        public string FileFormat { get; set; }
        public long FileSizeKB { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double DPI { get; set; }
        public double Blurriness { get; set; }
        public double Contrast { get; set; }
        public double Brightness { get; set; }
        public double ReliabilityScore { get; set; }

        public override string ToString()
        {
            return $"FileFormat: {FileFormat}\n" +
                   $"FileSize: {FileSizeKB} KB\n" +
                   $"Dimensions: {Width}x{Height}\n" +
                   $"DPI: {DPI}\n" +
                   $"Blurriness (Variance of Laplacian): {Blurriness:F2}\n" +
                   $"Contrast: {Contrast:F2}\n" +
                   $"Brightness: {Brightness:F2}\n" +
                   $"Reliability Score: {ReliabilityScore:F2}%";
        }
    }

    public class ImageReliabilityChecker
    {
        // Define weights for each metric
        private const double FormatWeight = 10;
        private const double SizeWeight = 10;
        private const double DPIWeight = 20;
        private const double BrightnessWeight = 20;
        private const double ContrastWeight = 20;
        private const double BlurrinessWeight = 20;

        // Expected ranges for normalization
        private const double BlurrinessMin = 20.0;  // Lower bound for blurriness variance
        private const double BlurrinessMax = 150.0; // Upper bound for blurriness variance

        private const double BrightnessMin = 50.0;
        private const double BrightnessMax = 200.0;

        private const double ContrastMin = 30.0;
        private const double ContrastMax = 100.0;

        private const double DPIMin = 72.0;   // Common default DPI
        private const double DPIMax = 600.0;  // Arbitrary upper bound for DPI

        public ImageReliabilityResult CheckImageReliability(string imagePath)
        {
            var result = new ImageReliabilityResult();
            double score = 0.0;

            try
            {
                // Check file format
                string extension = Path.GetExtension(imagePath).ToLower();
                result.FileFormat = extension;
                var allowedFormats = new string[] { ".png", ".jpeg", ".jpg", ".tiff", ".bmp" };
                if (Array.Exists(allowedFormats, format => format == extension))
                {
                    score += FormatWeight;
                }

                // Check file size
                var fileInfo = new FileInfo(imagePath);
                result.FileSizeKB = fileInfo.Length / 1024;
                if (result.FileSizeKB >= 50 && result.FileSizeKB <= 5000) // Example: 50KB - 5MB
                {
                    score += SizeWeight;
                }

                // Load image with OpenCvSharp for advanced analysis
                using (var mat = Cv2.ImRead(imagePath, ImreadModes.Color))
                {
                    if (mat.Empty())
                    {
                        throw new Exception("Unable to load image.");
                    }

                    result.Width = mat.Width;
                    result.Height = mat.Height;

                    // Get DPI from metadata if possible
                    using (var imageSharpImage = Image.Load<Rgba32>(imagePath))
                    {
                        // Default DPI values if not set
                        double horizontalDpi = imageSharpImage.Metadata.HorizontalResolution > 0
                            ? imageSharpImage.Metadata.HorizontalResolution
                            : DPIMin; // Common default DPI
                        double verticalDpi = imageSharpImage.Metadata.VerticalResolution > 0
                            ? imageSharpImage.Metadata.VerticalResolution
                            : DPIMin; // Common default DPI

                        result.DPI = (horizontalDpi + verticalDpi) / 2.0;

                        // Normalize DPI between 0 and 1
                        double normalizedDpi = Math.Clamp((result.DPI - DPIMin) / (DPIMax - DPIMin), 0.0, 1.0);
                        score += normalizedDpi * DPIWeight;
                    }

                    // Calculate brightness and contrast
                    using (var gray = new Mat())
                    {
                        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                        Cv2.MeanStdDev(gray, out Scalar mean, out Scalar stddev);
                        result.Brightness = mean.Val0;
                        result.Contrast = stddev.Val0 * 100.0; // Scale contrast for scoring

                        // Normalize brightness
                        double normalizedBrightness = 0.0;
                        if (result.Brightness < BrightnessMin)
                        {
                            normalizedBrightness = Math.Clamp((result.Brightness - (BrightnessMin - 50)) / 50.0, 0.0, 1.0);
                        }
                        else if (result.Brightness > BrightnessMax)
                        {
                            normalizedBrightness = Math.Clamp((BrightnessMax + 50 - result.Brightness) / 50.0, 0.0, 1.0);
                        }
                        else
                        {
                            normalizedBrightness = 1.0;
                        }
                        score += normalizedBrightness * BrightnessWeight;

                        // Normalize contrast
                        double normalizedContrast = Math.Clamp((result.Contrast - ContrastMin) / (ContrastMax - ContrastMin), 0.0, 1.0);
                        score += normalizedContrast * ContrastWeight;

                        // Calculate blurriness with Variance of Laplacian
                        using (var laplacian = new Mat())
                        {
                            Cv2.Laplacian(gray, laplacian, MatType.CV_64F, ksize: 3);
                            // Calculate the variance of the Laplacian
                            Cv2.MeanStdDev(laplacian, out Scalar lapMean, out Scalar lapStdDev);
                            double laplacianVar = lapStdDev.Val0 * lapStdDev.Val0; // Variance = stddev^2
                            result.Blurriness = laplacianVar;

                            // Normalize blurriness
                            double normalizedBlurriness = Math.Clamp((laplacianVar - BlurrinessMin) / (BlurrinessMax - BlurrinessMin), 0.0, 1.0);
                            score += normalizedBlurriness * BlurrinessWeight;
                        }
                    }
                }

                // Final score, capping at 100
                result.ReliabilityScore = Math.Min(score, 100.0);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error processing image: {ex.Message}");
                result.ReliabilityScore = 0.0;
            }

            return result;
        }

        public bool PersonalIdChecker(GraduationDocument document)
        {
            string personalId = document.PersonalId;
            // Regex (YYMMDD-XXXX or YYYYMMDD-XXXX or without hyphen)
            string pattern = @"^(\d{6}|\d{8})(-|\d)?\d{4}$";
            // Check if personalId matches the pattern
            return Regex.IsMatch(personalId, pattern);
        }
    }
}
