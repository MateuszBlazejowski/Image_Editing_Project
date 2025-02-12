using System;
using System.IO;
using ImSh = SixLabors.ImageSharp;

namespace MinImage.ImageProcessing
{
    /// <summary>
    /// Class responsible for generating gradient-based images using unsafe operations.
    /// </summary>
    public class GradientGenerator
    {
        public string GenerateGradient(int width, int height)
        {
            // Create an image with the specified dimensions
            ImSh.Image<ImSh.PixelFormats.Rgba32> image = new(width, height);

            // Get the single pixel memory block (unsafe operation)
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<ImSh.PixelFormats.Rgba32> memory))
            {
                Console.WriteLine("Failed to access pixel memory.");
                return null;
            }

            var span = memory.Span;

            // Fill the image with a gradient using unsafe pointers
            unsafe
            {
                fixed (ImSh.PixelFormats.Rgba32* ptr = span)
                {
                    for (int i = 0; i < width; i++)
                    {
                        int red = 255 * i / width;
                        for (int j = 0; j < height; j++)
                        {
                            int blue = 255 * j / height;

                            // Calculate the index and set pixel values
                            ImSh.PixelFormats.Rgba32* pixel = ptr + (j * width + i);
                            pixel->R = (byte)red;
                            pixel->G = (byte)(red * blue / 255);
                            pixel->B = (byte)blue;
                            pixel->A = 255;
                        }
                    }
                }
            }

            // Define the file name for the gradient image
            string fileName = $"GradientImage_{width}x{height}.jpeg";

            // Save the image as a JPEG file
            using FileStream fs = new(fileName, FileMode.Create, FileAccess.Write);
            ImSh.Formats.Jpeg.JpegEncoder encoder = new();
            encoder.Encode(image, fs);

            // Dispose the image to free resources
            image.Dispose();
            Console.WriteLine($"Saved: {fileName}");

            // Return the file path of the saved image
            return fileName;
        }
    }
}
