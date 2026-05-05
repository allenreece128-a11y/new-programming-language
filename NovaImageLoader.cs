using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NovaScript.Core
{
    public static class NovaImageLoader
    {
        public static (int Width, int Height, byte[] PixelsRgba) LoadRgba(string path)
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(path);
            byte[] rgba = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(rgba);
            return (image.Width, image.Height, rgba);
        }
    }
}
