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
        public void SaveAllImages(ConcurrentDictionary<int, Picture> imageDictionary)
        {
            foreach (var kvp in imageDictionary)
            {
                var imageId = kvp.Key;
                var picture = kvp.Value;

                if (picture == null || picture.Image == null)
                {
                    Console.WriteLine($"Skipping uninitialized image ID: {imageId}");
                    continue;
                }

                var fileName = string.IsNullOrWhiteSpace(picture.FilePrefix)
                    ? $"default_{imageId + 1}.jpeg"
                    : $"{picture.FilePrefix}_{imageId + 1}.jpeg";

                SaveImage(picture.Image, fileName);
            }
        }

        private void SaveImage(Image<Rgba32> image, string fileName)
        {
            using var stream = File.OpenWrite(fileName);
            var encoder = new ImSh.Formats.Jpeg.JpegEncoder();
            encoder.Encode(image, stream);

            Console.WriteLine($"Saved: {fileName}");
        }
    }
}
