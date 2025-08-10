using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace AiStudio4.McpStandalone.Converters
{
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.Circle24;
            }
            return SymbolRegular.Circle24;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}