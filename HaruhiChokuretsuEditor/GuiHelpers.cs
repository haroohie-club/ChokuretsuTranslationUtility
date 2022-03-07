using SkiaSharp;
using System.IO;
using System.Windows.Media.Imaging;

namespace HaruhiChokuretsuEditor
{
    public static class GuiHelpers
    {
        public static BitmapImage GetBitmapImageFromBitmap(SKBitmap bitmap)
        {
            BitmapImage bitmapImage = new();
            using (MemoryStream memoryStream = new())
            {
                bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, HaruhiChokuretsuLib.Archive.GraphicsFile.PNG_QUALITY);
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
