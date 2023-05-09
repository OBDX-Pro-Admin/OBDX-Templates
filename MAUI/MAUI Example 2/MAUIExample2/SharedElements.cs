using OBDXMAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUIExample
{
    public static class SharedElements
    {
        public static Scantool MyScantool = new Scantool();

        public static J2534 MyJ2534Scantool = new J2534();
    }
}
