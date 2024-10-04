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

        public bool ValidateData(GraduationDocument document)
        {

            // Check if the document has a valid personal ID
            if (!PersonalIdChecker(document))
            {
                return false;
            }

            // Check if the document has first and last name
            if (string.IsNullOrWhiteSpace(document.FullName))
            {
                return false;
            }

            // check if the document has a program name
            if (string.IsNullOrWhiteSpace(document.ProgramName))
            {
                return false;
            }

            return true;
        }

        public bool PersonalIdChecker(GraduationDocument document)
        {
            string personalId = document.PersonalId;
            // Regex (YYMMDD-XXXX or YYYYMMDD-XXXX or without hyphen)
            string pattern = @"^(\d{6}|\d{8})(-|\d)?\d{4}$";
            // Check if personalId matches the pattern
            return Regex.IsMatch(personalId, pattern);
        }

        public static List<Mat> SegmentImageWithTableDetection(string imagePath, string outputDirectory)
        {
            // Load the image
            Mat img = Cv2.ImRead(imagePath, ImreadModes.Color);

            // Define a list to store the cropped sections
            List<Mat> croppedSections = new List<Mat>();

            // Get the dimensions of the image
            int imageHeight = img.Rows;
            int imageWidth = img.Cols;

            // Define the region for the top part (Header section)
            // Assuming the top portion is roughly the top 20-25% of the document
            int headerHeight = imageHeight / 4; // Adjust as needed
            Rect headerRect = new Rect(0, 0, imageWidth, headerHeight);

            // Crop the header section
            Mat headerSection = new Mat(img, headerRect);
            croppedSections.Add(headerSection);

            // Save the header section for inspection
            string headerFilePath = Path.Combine(outputDirectory, "header_segment.png");
            Cv2.ImWrite(headerFilePath, headerSection);

            // Process the rest of the image (bottom part)
            // Define the region for the table section (remaining 75% of the image)
            int tableStartY = headerHeight;
            Rect tableRect = new Rect(0, tableStartY, imageWidth, imageHeight - headerHeight);
            Mat tableSection = new Mat(img, tableRect);

            // Convert the table section to grayscale
            Mat gray = new Mat();
            Cv2.CvtColor(tableSection, gray, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(5, 5), 0);

            // Apply adaptive thresholding to handle varying lighting conditions
            Mat binary = new Mat();
            Cv2.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 21, 15);

            // Use morphological transformations to clean up the image
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(binary, binary, MorphTypes.Close, kernel);

            // Detect horizontal and vertical lines to find tables
            Mat horizontal = binary.Clone();
            int horizontalSize = horizontal.Cols / 20;
            Mat horizontalStructure = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(horizontalSize, 1));
            Cv2.Erode(horizontal, horizontal, horizontalStructure);
            Cv2.Dilate(horizontal, horizontal, horizontalStructure);

            Mat vertical = binary.Clone();
            int verticalSize = vertical.Rows / 20;
            Mat verticalStructure = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, verticalSize));
            Cv2.Erode(vertical, vertical, verticalStructure);
            Cv2.Dilate(vertical, vertical, verticalStructure);

            // Combine horizontal and vertical lines to create a table mask
            Mat tableMask = new Mat();
            Cv2.Add(horizontal, vertical, tableMask);

            // Find contours in the table mask
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(tableMask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            int counter = 0;
            foreach (var contour in contours)
            {
                Rect boundingRect = Cv2.BoundingRect(contour);

                // Optionally filter small areas to remove noise
                if (boundingRect.Width > 50 && boundingRect.Height > 50)
                {
                    // Adjust the bounding rectangle to take into account the starting Y offset from cropping
                    Rect adjustedBoundingRect = new Rect(boundingRect.X, boundingRect.Y + tableStartY, boundingRect.Width, boundingRect.Height);

                    // Crop the detected section from the original image (using adjusted coordinates)
                    Mat section = new Mat(img, adjustedBoundingRect);
                    croppedSections.Add(section);

                    // Save the cropped section to a file for inspection
                    string outputFilePath = Path.Combine(outputDirectory, $"segment_{counter}.png");
                    Cv2.ImWrite(outputFilePath, section);
                    counter++;
                }
            }

            return croppedSections;
        }


    }
}
