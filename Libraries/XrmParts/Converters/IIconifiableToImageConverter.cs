using Formula81.XrmToolBox.Libraries.Xrm.Resources;
using Formula81.XrmToolBox.Libraries.XrmParts.Components;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Formula81.XrmToolBox.Libraries.XrmParts.Converters
{
    public class IIconifiableToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage image;
            var iconifiable = value as IIconifiable;
            if ((iconifiable?.IconData?.Length ?? 0) > 0)
            {
                image = new BitmapImage();
                using (var memoryStream = new MemoryStream(iconifiable.IconData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = memoryStream;
                    image.EndInit();
                    image.Freeze();
                }
            }
            else
            {
                image = new BitmapImage(XrmResourceRepository.GetEntityIconUri(iconifiable?.ObjectTypeCode));
            }
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
