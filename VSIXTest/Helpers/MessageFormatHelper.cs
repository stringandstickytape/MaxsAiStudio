using System;
using SharedClasses;

namespace VSIXTest
{
    public static class MessageFormatHelper
    {
        public static string InsertFilenamedSelection(string message, string documentFilename, string selection)
        {
            return message.Replace(BacktickHelper.PrependHash(":selection:"), FormatFile(documentFilename, selection));
        }

        public  static string FormatFile(string filename, string selection)
        {
            return $"{BacktickHelper.ThreeTicks}{filename}{Environment.NewLine}{selection}{Environment.NewLine}{BacktickHelper.ThreeTicksAndNewline}";
        }
    }
}