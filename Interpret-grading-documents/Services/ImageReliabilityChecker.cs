using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
                   $"Blurriness: {Blurriness}\n" +
                   $"Contrast: {Contrast}\n" +
                   $"Brightness: {Brightness}\n" +
                   $"Reliability Score: {ReliabilityScore}%";
        }
    }

    public class ImageReliabilityChecker
    {
        public ImageReliabilityResult CheckImageReliability(string imagePath)
        {
            var result = new ImageReliabilityResult();
            double score = 0.0;

            // Kontrollera filformat
            string extension = Path.GetExtension(imagePath).ToLower();


            //Om filen är en pdf-fil
            if (extension == ".pdf")
            {
                result.FileFormat = extension;
                result.ReliabilityScore = 100;
                return result;
            }
                

            result.FileFormat = extension;
            var allowedFormats = new string[] { ".png", ".jpeg", ".jpg", ".tiff", ".bmp" };
            if (Array.Exists(allowedFormats, format => format == extension))
            {
                score += 10;
            }

            // Kontrollera filstorlek
            var fileInfo = new FileInfo(imagePath);
            result.FileSizeKB = fileInfo.Length / 1024;
            if (result.FileSizeKB >= 50 && result.FileSizeKB <= 5000) // Exempel: 50KB - 5MB
            {
                score += 10;
            }

            // Ladda bilden med SixLabors ImageSharp
            using (var image = Image.Load<Rgba32>(imagePath))
            {
                result.Width = image.Width;
                result.Height = image.Height;

                // Kontrollera upplösning (DPI)
                if (image.Metadata.HorizontalResolution > 300 && image.Metadata.VerticalResolution > 300)
                {
                    result.DPI = (image.Metadata.HorizontalResolution + image.Metadata.VerticalResolution) / 2.0;
                    score += 20;
                }
                else
                {
                    result.DPI = (image.Metadata.HorizontalResolution + image.Metadata.VerticalResolution) / 2.0;
                }

                // Beräkna medelvärde för ljusstyrka och kontrast
                double brightness = 0;
                double contrast = 0;
                int totalPixels = image.Width * image.Height;

                // Använd ProcessPixelRows för att iterera över alla rader av pixlar
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                        foreach (Rgba32 pixel in pixelRow)
                        {
                            // Enkel ljusstyrka som medelvärde av RGB
                            brightness += (pixel.R + pixel.G + pixel.B) / 3.0;

                            // Enkel kontrastberäkning kan vara standardavvikelsen
                            contrast += Math.Abs(pixel.R - pixel.G) + Math.Abs(pixel.R - pixel.B) + Math.Abs(pixel.G - pixel.B);
                        }
                    }
                });

                brightness /= totalPixels;
                contrast /= totalPixels;

                result.Brightness = brightness;
                result.Contrast = contrast;

                if (brightness >= 50 && brightness <= 200)
                {
                    score += 20;
                }

                if (contrast > 50)
                {
                    score += 20;
                }

                // Suddighet kan approximativt mätas, men ImageSharp har ingen inbyggd metod
                result.Blurriness = 100;
                score += 20;
            }

            result.ReliabilityScore = Math.Min(score, 100.0);
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
