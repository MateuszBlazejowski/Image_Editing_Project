using System;
using System.Runtime.InteropServices;
using ImSh = SixLabors.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MinImage.ImageGenerating
{
    public class ImageGenerator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MyColor
        {
            public byte r, g, b, a;
        }

        CancellationToken token;
        public event Action<int, int>? ProgressUpdated; // Reports progress: imageId, progress%

        [DllImport("ImageGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GenerateImage(nint texture, int width, int height, ProgressCallback callback);

        private delegate bool ProgressCallback(float progress);

        /// <summary>
        /// Generates a single image and returns it.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <returns>The generated image.</returns>
        public Image<Rgba32> GenerateSingleImage(int width, int height, int imageId, CancellationToken _token)
        {
            var texture = Marshal.AllocHGlobal(Marshal.SizeOf<MyColor>() * width * height);
            token = _token;

            try
            {
                var image = new Image<Rgba32>(width, height);

                ProgressCallback callback = progress =>
                {
                    if (token.IsCancellationRequested)
                        return false; // Stop processing

                    ProgressUpdated?.Invoke(imageId, (int)(progress * 100));
                    return true;
                };

                // Generate the image using the native method
                GenerateImage(texture, width, height, callback);

                // Fill the ImageSharp object with generated pixel data
                CopyTextureToImage(texture, image);

                // Trigger progress update for 100% completion
                ProgressUpdated?.Invoke(imageId, 100);

                return image;
            }
            finally
            {
                Marshal.FreeHGlobal(texture);
            }
        }

        /// <summary>
        /// Copies pixel data from the unmanaged texture to an ImageSharp image.
        /// </summary>
        /// <param name="texture">The unmanaged texture pointer.</param>
        /// <param name="image">The ImageSharp image to populate.</param>
        private void CopyTextureToImage(nint texture, Image<Rgba32> image)
        {
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
            {
                throw new InvalidOperationException("Failed to access pixel memory.");
            }

            unsafe
            {
                fixed (Rgba32* destPtr = memory.Span)
                {
                    var srcPtr = (MyColor*)texture.ToPointer();
                    for (int i = 0; i < image.Width * image.Height; i++)
                    {
                        destPtr[i] = new Rgba32(srcPtr[i].r, srcPtr[i].g, srcPtr[i].b, srcPtr[i].a);
                    }
                }
            }
        }
    }
}
