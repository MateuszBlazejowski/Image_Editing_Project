using System;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MinImage.ImageProcessing
{
    public class ImageRoomDrawer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MyColor
        {
            public byte r, g, b, a;
        }

        private delegate bool ProgressCallback(float progress);
        private delegate MyColor ModifyPixelCallback(float x, float y, MyColor existingColor);

        [DllImport("ImageGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ProcessPixels_Custom(
            nint texture, int width, int height,
            ModifyPixelCallback modifyCallback,
            ProgressCallback progressCallback);

        public event Action<int, int> ProgressUpdated;

        private CancellationToken _token;

        public void DrawRoom(Image<Rgba32> image, float x1, float y1, float x2, float y2, int imageId, CancellationToken token)
        {
            _token = token;

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

                ProgressCallback progressCallback = progress =>
                {
                    if (_token.IsCancellationRequested)
                        return false;

                    ProgressUpdated?.Invoke(imageId, (int)(progress * 100));
                    return true;
                };

                ModifyPixelCallback modifyCallback = (normalizedX, normalizedY, existingColor) =>
                {
                    // Check if the pixel lies within the rectangle
                    if (normalizedX >= x1 && normalizedX <= x2 && normalizedY >= y1 && normalizedY <= y2)
                    {
                        return new MyColor
                        {
                            r = 255,
                            g = 255,
                            b = 255,
                            a = 255
                        };
                    }

                    return existingColor; // Leave the pixel unchanged
                };


                ProcessPixels_Custom(texture, image.Width, image.Height, modifyCallback, progressCallback);

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
