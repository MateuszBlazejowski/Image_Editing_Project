using System;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MinImage
{
    public class ImageGammaCorrector
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MyColor
        {
            public byte r, g, b, a;
        }

        private delegate bool ProgressCallback(float progress);
        private CancellationToken token;

        [DllImport("ImageGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GammaCorrection(nint texture, int width, int height, float gamma, ProgressCallback callback);

        public event Action<int, int> ProgressUpdated;

        public void ApplyGammaCorrection(Image<Rgba32> image, float gamma, int imageId, CancellationToken _token)
        {
            token = _token;

            if (image == null)
                throw new ArgumentNullException(nameof(image), "Image cannot be null.");

            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
                throw new InvalidOperationException("Failed to access pixel memory.");

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
                        return false;

                    ProgressUpdated?.Invoke(imageId, (int)(progress * 100));
                    return true;
                };

                GammaCorrection(texture, image.Width, image.Height, gamma, callback);

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
