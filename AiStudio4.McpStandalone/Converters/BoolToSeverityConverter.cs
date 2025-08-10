using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace AiStudio4.McpStandalone.Converters
{
    public class BoolToSeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
            }
            return InfoBarSeverity.Informational;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}