using System;
using SharedClasses;

namespace VSIXTest
{
    public static class MessageFormatter
    {
        public static string InsertFilenamedSelection(string message, string documentFilename, string selection)
        {
            return message.Replace(BacktickHelper.PrependHash(":selection:"), $"{BacktickHelper.ThreeTicks}{documentFilename}{Environment.NewLine}{selection}{Environment.NewLine}{BacktickHelper.ThreeTicksAndNewline}");
        }
    }
}