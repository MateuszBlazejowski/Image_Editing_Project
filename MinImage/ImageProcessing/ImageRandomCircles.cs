using System;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MinImage.ImageProcessing
{
    public class ImageRandomCircles
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MyColor
        {
            public byte r, g, b, a;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Circle
        {
            public float x;       // X coordinate of the circle's center (normalized: 0 to 1)
            public float y;       // Y coordinate of the circle's center (normalized: 0 to 1)
            public float radius;  // Radius of the circle (normalized or in pixels)
        }

        private delegate bool ProgressCallback(float progress);
        private CancellationToken token;

        [DllImport("ImageGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DrawCircles(
            nint texture,       // Pointer to the image data
            int width,          // Image width
            int height,         // Image height
            Circle[] circles,   // Array of circles
            int circleCount,    // Number of circles
            ProgressCallback callback // Progress callback function
        );

        public event Action<int, int> ProgressUpdated; // Event for progress updates

        /// <summary>
        /// Draws random circles on the given image using the C++ library.
        /// </summary>
        public void ApplyRandomCircles(Image<Rgba32> image, int numCircles, int radius, int imageId, CancellationToken _token)
        {
            token = _token;

            if (image == null)
                throw new ArgumentNullException(nameof(image), "Image cannot be null.");
            if (numCircles <= 0)
                throw new ArgumentOutOfRangeException(nameof(numCircles), "Number of circles must be greater than zero.");
            if (radius <= 0 || radius > Math.Min(image.Width, image.Height) / 2)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive and fit within the image.");

            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
                throw new InvalidOperationException("Failed to access pixel memory.");

            var texture = Marshal.AllocHGlobal(Marshal.SizeOf<MyColor>() * image.Width * image.Height);

            try
            {
                // Initialize circle data
                var random = new Random();
                var circles = new Circle[numCircles];
                for (int i = 0; i < numCircles; i++)
                {
                    circles[i] = new Circle
                    {
                        x = (float)random.NextDouble(), // Random X coordinate (normalized)
                        y = (float)random.NextDouble(), // Random Y coordinate (normalized)
                        radius = radius / (float)Math.Max(image.Width, image.Height) // Normalize radius
                    };
                }

                unsafe
                {
                    fixed (Rgba32* srcPtr = memory.Span)
                    {
                        var destPtr = (MyColor*)texture.ToPointer();
                        for (int i = 0; i < image.Width * image.Height; i++)
                        {
                            destPtr[i].r = srcPtr[i].R;
                            destPtr[i].g = srcPtr[i].G;
                            destPtr[i].b = srcPtr[i].B;
                            destPtr[i].a = srcPtr[i].A;
                        }
                    }
                }

                ProgressCallback callback = progress =>
                {
                    if (token.IsCancellationRequested)
                        return false; // Stop processing

                    ProgressUpdated?.Invoke(imageId, (int)(progress * 100));
                    return true;
                };

                // Call the updated DrawCircles function
                DrawCircles(texture, image.Width, image.Height, circles, numCircles, callback);

                unsafe
                {
                    fixed (Rgba32* destPtr = memory.Span)
                    {
                        var srcPtr = (MyColor*)texture.ToPointer();
                        for (int i = 0; i < image.Width * image.Height; i++)
                        {
                            destPtr[i].R = srcPtr[i].r;
                            destPtr[i].G = srcPtr[i].g;
                            destPtr[i].B = srcPtr[i].b;
                            destPtr[i].A = srcPtr[i].a;
                        }
                    }
                }
            }
            finally
            {
                if (texture != nint.Zero)
                    Marshal.FreeHGlobal(texture);
            }
        }
    }
}
