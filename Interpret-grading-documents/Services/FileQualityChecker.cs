using System.Drawing;

namespace Interpret_grading_documents.Services;

public class FileQualityChecker
{
    public int FileScore(string filePath)
    {

        int fileScore = 0;

        // Check file size
        FileInfo fileInfo = new FileInfo(filePath);
        long fileSizeInBytes = fileInfo.Length;
        Console.WriteLine($"File Size: {fileSizeInBytes / 1024.0} KB");

        // Determine if the file is an image or PDF
        string fileExtension = Path.GetExtension(filePath).ToLower();

        if (fileExtension == ".pdf")
        {
            Console.WriteLine();
        }
        else if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".webp" || fileExtension == ".gif")
        {
            // Load the image
            using (Image image = Image.FromFile(filePath))
            {
                // Check resolution (DPI)
                float horizontalDpi = image.HorizontalResolution;
                float verticalDpi = image.VerticalResolution;
                Console.WriteLine($"Horizontal DPI: {horizontalDpi}");
                Console.WriteLine($"Vertical DPI: {verticalDpi}");

                // Check image dimensions
                int width = image.Width;
                int height = image.Height;
                Console.WriteLine($"Image Dimensions: {width}x{height}");

                // Check image format
                if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                    Console.WriteLine("Image Type: JPEG");
                else if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png))
                    Console.WriteLine("Image Type: PNG");
                else if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp))
                    Console.WriteLine("Image Type: BMP");
                else if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                    Console.WriteLine("Image Type: GIF");
                else
                    Console.WriteLine("Image Type: Other");
            }
        }
        else
        {
            Console.WriteLine("Unsupported file format.");
                
        }

        return fileScore;
    }
}