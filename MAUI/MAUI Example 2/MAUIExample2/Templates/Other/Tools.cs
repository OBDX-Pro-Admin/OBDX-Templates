using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUIExample
{
    public class Tools
    {
        public class Time
        {
            static double starttime = Stopwatch.GetTimestamp();
            public static uint GetMicroseconds()
            {
                double timestamp = Stopwatch.GetTimestamp(); //- starttime

                double microseconds = 1_000_000.0 * timestamp / Stopwatch.Frequency;

                return (uint)microseconds;
            }

        }
    }
}
