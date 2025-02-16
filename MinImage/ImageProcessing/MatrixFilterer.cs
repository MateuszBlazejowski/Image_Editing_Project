using System;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MinImage.ImageProcessing
{
    public class MatrixFilterer
    {
        public event Action<int, int>? ProgressUpdated;
        private CancellationToken token;

        /// <summary>
        /// Color transformation: (r, g, b) -> (r^7/5, g, b^8/5)
        /// </summary>
        public void ApplyMatrixFilter(Image<Rgba32> image, int imageId, CancellationToken _token)
        {
            token = _token;

            if (image == null)
                throw new ArgumentNullException(nameof(image), "Image cannot be null.");

            var totalPixels = image.Width * image.Height;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var pixel = image[x, y];

                    // Apply the transformation
                    float r = MathF.Pow(pixel.R / 255f, 7f / 5f) ;
                    float g = pixel.G / 255f;
                    float b = MathF.Pow(pixel.B / 255f, 8f / 5f) ;

                    // Clamp values to [0, 255] and assign to the pixel
                    image[x, y] = new Rgba32(
                        (byte)(Math.Clamp(r * 255, 0, 255)),
                        (byte)(Math.Clamp(g * 255, 0, 255)),
                        (byte)(Math.Clamp(b * 255, 0, 255)),
                        255
                    );
                }

                // Report progress
                ProgressUpdated?.Invoke(imageId, (int)((float)y / image.Height * 100));
            }
        }
    }
}

