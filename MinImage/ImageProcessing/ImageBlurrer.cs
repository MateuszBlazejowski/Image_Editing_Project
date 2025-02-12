using System;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MinImage.ImageProcessing
{
    public class ImageBlurrer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MyColor
        {
            public byte r, g, b, a;
        }

        private delegate bool ProgressCallback(float progress);
        CancellationToken token;

        [DllImport("ImageGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Blur(nint texture, int width, int height, int blurWidth, int blurHeight, ProgressCallback callback);

        public event Action<int, int> ProgressUpdated; // Event for progress updates

        /// <summary>
        /// Blurs the given image using the C++ library.
        /// </summary>
        /// <param name="image">The image to be blurred.</param>
        /// <param name="blurWidth">The blur width.</param>
        /// <param name="blurHeight">The blur height.</param>
        public void ApplyBlur(Image<Rgba32> image, int blurWidth, int blurHeight, int imageId, CancellationToken _token)
        {
            token = _token;
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image), "Image cannot be null.");
            }

            if (blurWidth <= 0 || blurHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blurWidth), "Blur dimensions must be greater than zero.");
            }

            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
            {
                throw new InvalidOperationException("Failed to access pixel memory.");
            }

            var texture = Marshal.AllocHGlobal(Marshal.SizeOf<MyColor>() * image.Width * image.Height);

            try
            {
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

                Blur(texture, image.Width, image.Height, blurWidth, blurHeight, callback);

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
                Marshal.FreeHGlobal(texture);
            }
        }
    }
}
