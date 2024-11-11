using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.IO;

namespace HaruhiChokuretsuEditor
{
    public static class GuiHelpers
    {
        public static BitmapImage GetBitmapImageFromBitmap(SKBitmap bitmap)
        {
            BitmapImage bitmapImage = new();
            if (bitmap.Pixels.Length == 0)
            {
                return null;
            }
            using (MemoryStream memoryStream = new())
            {
                bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                memoryStream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
    }
}
