using System;
using System.Collections.Generic;
using System.Text;

namespace SharedClasses
{
    public class BacktickHelper
    {
        public static readonly string ThreeTicks = new string('`', 3);

        public static readonly string ThreeTicksAndNewline = $"{ThreeTicks}{Environment.NewLine}";
    }
}
