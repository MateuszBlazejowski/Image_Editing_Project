using MinImage.CommandProcessing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImSh = SixLabors.ImageSharp;
using System.Collections.Concurrent;
using MinImage;

namespace MinImage.AdditionalFuncionalities
{
    public class Saver
    {
        public void SaveAllImages(ConcurrentDictionary<int, Picture> imageDictionary, string? folderPath = null)
        {
            // Use the provided folder or default to the current directory
            string saveDirectory = string.IsNullOrWhiteSpace(folderPath) ? Directory.GetCurrentDirectory() : folderPath;

            // Ensure the folder exists
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            foreach (var kvp in imageDictionary)
            {
                var imageId = kvp.Key;
                var picture = kvp.Value;

                if (picture == null || picture.Image == null)
                {
                    Console.WriteLine($"Skipping uninitialized image ID: {imageId}");
                    continue;
                }

                // Construct the full file path
                var fileName = string.IsNullOrWhiteSpace(picture.FilePrefix)
                    ? $"default_{imageId + 1}.jpeg"
                    : $"{picture.FilePrefix}_{imageId + 1}.jpeg";

                var filePath = Path.Combine(saveDirectory, fileName);

                SaveImage(picture.Image, filePath);
            }
        }

        private void SaveImage(Image<Rgba32> image, string filePath)
        {
            using var stream = File.OpenWrite(filePath);
            var encoder = new ImSh.Formats.Jpeg.JpegEncoder();
            encoder.Encode(image, stream);

            Console.WriteLine($"Saved: {filePath}");
        }


    }
}
