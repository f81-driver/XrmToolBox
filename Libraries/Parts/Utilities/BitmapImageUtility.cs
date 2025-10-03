using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Formula81.XrmToolBox.Libraries.Parts.Utilities
{
    public static class BitmapImageUtility
    {
        public enum BitmapImageScaleDimension { Width, Height }

        public static BitmapImage CreateFromUrl(string url)
        {
            var bytes = File.ReadAllBytes(url);
            var memoryStream = new MemoryStream(bytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public static BitmapImage CreateFromBitmap(Bitmap bitmap)
        {
            var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Jpeg);
            memoryStream.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public static BitmapImage CreateScaledFromByteArray(byte[] data, int scale)
        {
            return CreateScaledFromByteArray(data, scale, BitmapImageScaleDimension.Width);
        }

        public static BitmapImage CreateScaledFromByteArray(byte[] data, int scale, BitmapImageScaleDimension dimension)
        {
            BitmapImage bitmapImage = null;
            if (data != null && data.Length != 0)
            {
                bitmapImage = new BitmapImage();
                using (var memoryStream = new MemoryStream(data))
                {
                    memoryStream.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    switch (dimension)
                    {
                        case BitmapImageScaleDimension.Height:
                            bitmapImage.DecodePixelHeight = scale;
                            break;
                        case BitmapImageScaleDimension.Width:
                            bitmapImage.DecodePixelWidth = scale;
                            break;
                    }
                    bitmapImage.UriSource = null;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
            }
            return bitmapImage;
        }

        public static BitmapImage CreateFromByteArray(byte[] data)
        {
            BitmapImage bitmapImage = null;
            if (data != null && data.Length != 0)
            {
                bitmapImage = new BitmapImage();
                using (var memoryStream = new MemoryStream(data))
                {
                    memoryStream.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
            }
            return bitmapImage;
        }

        public static byte[] EncodeToByteArray(BitmapImage imageSource)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource));

            using (var memoryStream = new MemoryStream())
            {
                encoder.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
