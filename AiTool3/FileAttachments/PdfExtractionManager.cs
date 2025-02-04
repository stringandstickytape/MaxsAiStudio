using System;
using System.Collections.Generic;
using PdfiumViewer;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.FileAttachments
{

    public class PdfExtractionManager
    { 

    public void ExtractFromPDFs()
    {
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Title = "Select PDF File(s)";
            openFileDialog.Multiselect = true; // Enable multiple file selection

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var converter = new PdfToBitmapConverter();
                int successCount = 0;
                int failureCount = 0;
                StringBuilder errorMessages = new StringBuilder();


                converter.DeleteOutputFile(openFileDialog.FileNames[0]);

                foreach (string selectedFilePath in openFileDialog.FileNames)
                {

                    // Create folder for output images
                    string outputDirectory = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(selectedFilePath),
                        System.IO.Path.GetFileNameWithoutExtension(selectedFilePath) + "_images"
                    );

                    // Create the output directory if it doesn't exist
                    Directory.CreateDirectory(outputDirectory);

                    try
                    {
                        // Convert PDF to images
                        converter.ConvertPdfToBitmaps(selectedFilePath, outputDirectory, 320);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        errorMessages.AppendLine($"Error converting {Path.GetFileName(selectedFilePath)}: {ex.Message}");
                    }
                }

                // Show summary message
                if (failureCount == 0)
                {
                    MessageBox.Show(
                        $"Successfully converted {successCount} PDF file(s)!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Completed with issues:\n" +
                        $"Successfully converted: {successCount}\n" +
                        $"Failed conversions: {failureCount}\n\n" +
                        $"Error details:\n{errorMessages}",
                        "Conversion Complete",
                        MessageBoxButtons.OK,
                        failureCount == openFileDialog.FileNames.Length ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
                }
            }
        }
    }
}
}

public class PdfToBitmapConverter
{
    private readonly string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    public void DeleteOutputFile(string pdfFilePath)
    {
        File.Delete(Path.Combine(
                    Path.GetDirectoryName(pdfFilePath),
                    $"all_{timestamp}_ai.txt"
                ));
    }
    public void ConvertPdfToBitmaps(string pdfFilePath, string outputDirectory, int dpi = 300, int renderScale = 1)
    {
        const int MAX_BITMAP_SIZE = 30000; // Being conservative, half of 60000

        try
        {
            if (!File.Exists(pdfFilePath))
                throw new FileNotFoundException("PDF file not found.", pdfFilePath);

            Directory.CreateDirectory(outputDirectory);

            // Create StringBuilder instances to store text
            StringBuilder allText = new StringBuilder();
            StringBuilder aiText = new StringBuilder();

            using (var document = PdfDocument.Load(pdfFilePath))
            {
                // Extract text from each page
                for (int pageNumber = 0; pageNumber < document.PageCount; pageNumber++)
                {
                    allText.AppendLine($"--- Page {pageNumber + 1} ---");
                    allText.AppendLine(document.GetPdfText(pageNumber));
                    allText.AppendLine(); // Add blank line between pages
                }

                // Save all text to a single file
                string textFilePath = Path.Combine(outputDirectory,
                    $"{Path.GetFileNameWithoutExtension(pdfFilePath)}_text.txt");
                File.WriteAllText(textFilePath, allText.ToString());

                // Add this text file content to the AI text with proper markup
                aiText.AppendLine($"{(char)96}{(char)96}{(char)96}{Path.GetFileName(textFilePath)}\r\n");
                aiText.AppendLine(allText.ToString());
                aiText.AppendLine($"{(char)96}{(char)96}{(char)96}");
                aiText.AppendLine();

                // Process images
                for (int pageNumber = 0; pageNumber < document.PageCount; pageNumber++)
                {
                    var pageSize = document.PageSizes[pageNumber];

                    // Calculate target dimensions
                    int targetWidth = (int)((pageSize.Width / 72) * dpi);
                    int targetHeight = (int)((pageSize.Height / 72) * dpi);

                    // Calculate initial render scale
                    int actualRenderScale = renderScale;

                    // Check if dimensions would exceed bitmap limits and adjust if necessary
                    while (actualRenderScale > 1)
                    {
                        if (targetWidth * actualRenderScale > MAX_BITMAP_SIZE ||
                            targetHeight * actualRenderScale > MAX_BITMAP_SIZE)
                        {
                            actualRenderScale--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // If even scale 1 is too big, we need to reduce the target DPI
                    if (targetWidth > MAX_BITMAP_SIZE || targetHeight > MAX_BITMAP_SIZE)
                    {
                        float scale = MAX_BITMAP_SIZE / (float)Math.Max(targetWidth, targetHeight);
                        targetWidth = (int)(targetWidth * scale);
                        targetHeight = (int)(targetHeight * scale);
                        dpi = (int)(dpi * scale);
                        actualRenderScale = 1;
                    }

                    Console.WriteLine($"Using render scale: {actualRenderScale} for page {pageNumber + 1}");

                    // Calculate final render dimensions
                    int renderWidth = targetWidth * actualRenderScale;
                    int renderHeight = targetHeight * actualRenderScale;
                    float renderDpi = dpi * actualRenderScale;

                    // Render at calculated resolution
                    using (var highResImage = document.Render(pageNumber, renderWidth, renderHeight,
                                                            renderDpi, renderDpi, false))
                    {
                        string baseOutputPath = Path.Combine(outputDirectory,
                            $"{Path.GetFileNameWithoutExtension(pdfFilePath)}_page_{pageNumber + 1}");

                        // Create and save full resolution version
                        SaveResolutionVersion(highResImage, targetWidth, targetHeight, dpi, baseOutputPath, "full");

                        // Create and save half resolution version
                        SaveResolutionVersion(highResImage, targetWidth / 2, targetHeight / 2, dpi / 2, baseOutputPath, "half");

                        // Create and save quarter resolution version
                        SaveResolutionVersion(highResImage, targetWidth / 4, targetHeight / 4, dpi / 4, baseOutputPath, "quarter");
                    }
                }

                // Save the combined AI text file
                string aiFilePath = Path.Combine(
                    Path.GetDirectoryName(pdfFilePath),
                    $"all_{timestamp}_ai.txt"
                );
                File.AppendAllText(aiFilePath, aiText.ToString());
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // Helper method to create and save different resolution versions
    void SaveResolutionVersion(Image sourceImage, int width, int height, float resolution, string baseOutputPath, string suffix)
    {
        string outputPath = $"{baseOutputPath}_{suffix}.png";

        using (var bmp = new Bitmap(width, height))
        {
            bmp.SetResolution(resolution, resolution);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(sourceImage, 0, 0, width, height);
            }

            // Crop whitespace
            using (var croppedBmp = CropWhitespace(bmp))
            {
                croppedBmp.Save(outputPath, ImageFormat.Png);
            }
        }
    }

    private Bitmap CropWhitespace(Bitmap source)
    {
        const int INNER_MARGIN = 20; // Maintain 20-pixel inner margin
        const int TRIM_EDGE = 5;    // Always remove outer 5 pixels

        Rectangle bounds;
        int width = source.Width;
        int height = source.Height;

        // Start from the trimmed edges
        int left = TRIM_EDGE;
        int right = width - 1 - TRIM_EDGE;
        int top = TRIM_EDGE;
        int bottom = height - 1 - TRIM_EDGE;
        bool foundPixel;

        // Find left edge (starting from trimmed edge)
        foundPixel = false;
        for (int x = left; x < width - TRIM_EDGE; x++)
        {
            for (int y = top; y <= bottom; y++)
            {
                if (!IsWhite(source.GetPixel(x, y)))
                {
                    foundPixel = true;
                    left = x;
                    break;
                }
            }
            if (foundPixel) break;
        }

        // Find right edge (starting from trimmed edge)
        foundPixel = false;
        for (int x = right; x >= left; x--)
        {
            for (int y = top; y <= bottom; y++)
            {
                if (!IsWhite(source.GetPixel(x, y)))
                {
                    foundPixel = true;
                    right = x;
                    break;
                }
            }
            if (foundPixel) break;
        }

        // Find top edge (starting from trimmed edge)
        foundPixel = false;
        for (int y = top; y < height - TRIM_EDGE; y++)
        {
            for (int x = left; x <= right; x++)
            {
                if (!IsWhite(source.GetPixel(x, y)))
                {
                    foundPixel = true;
                    top = y;
                    break;
                }
            }
            if (foundPixel) break;
        }

        // Find bottom edge (starting from trimmed edge)
        foundPixel = false;
        for (int y = bottom; y >= top; y--)
        {
            for (int x = left; x <= right; x = x + 4)
            {
                if (!IsWhite(source.GetPixel(x, y + 4)))
                {
                    foundPixel = true;
                    bottom = y;
                    break;
                }
            }
            if (foundPixel) break;
        }

        // If the image is completely white within the trimmed area, return a trimmed version
        if (left >= right || top >= bottom)
        {
            bounds = new Rectangle(TRIM_EDGE, TRIM_EDGE,
                                 width - (2 * TRIM_EDGE),
                                 height - (2 * TRIM_EDGE));
        }
        else
        {
            // Add inner margins and ensure we don't exceed the trimmed boundaries
            left = Math.Max(TRIM_EDGE, left - INNER_MARGIN);
            top = Math.Max(TRIM_EDGE, top - INNER_MARGIN);
            right = Math.Min(width - 1 - TRIM_EDGE, right + INNER_MARGIN);
            bottom = Math.Min(height - 1 - TRIM_EDGE, bottom + INNER_MARGIN);

            // Create bounds rectangle
            bounds = new Rectangle(left, top, right - left + 1, bottom - top + 1);
        }

        // Create new bitmap from bounds
        Bitmap croppedBitmap = new Bitmap(bounds.Width, bounds.Height);
        croppedBitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);

        using (Graphics g = Graphics.FromImage(croppedBitmap))
        {
            g.DrawImage(source, 0, 0, bounds, GraphicsUnit.Pixel);
        }

        return croppedBitmap;
    }

    private bool IsWhite(Color color)
    {
        return color.R > 250 && color.G > 250 && color.B > 250;
    }
}
