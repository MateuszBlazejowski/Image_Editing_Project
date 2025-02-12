using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImSh = SixLabors.ImageSharp;
using System.Collections.Concurrent;

namespace MinImage.CommandProcessing
{
    public class Picture
    {
        public Image<Rgba32>? Image { get; set; }
        public string? FilePrefix { get; set; }

        public Picture()
        {
            
        }
        public Picture(Image<Rgba32> image, string? filePrefix = "Pic")
        {
            Image = image;
            FilePrefix = filePrefix;
        }
    }
}