using System;
using System.Globalization;
using System.Windows.Data;

namespace Formula81.XrmToolBox.Libraries.Parts.Converters
{
    public class UIElementSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is double width 
                && parameter is string transformation)
            {
                if (int.TryParse(transformation.Substring(0, transformation.Length - 1), out int size))
                {
                    var operation = transformation[transformation.Length - 1];
                    switch (operation)
                    {
                        case '-': return width - size;
                        case '*': return (width * size) / 100;
                    }
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
