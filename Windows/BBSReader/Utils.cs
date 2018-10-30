using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBSReader
{
    class Utils
    {
        public static long GetTimestamp(string s)
        {
            DateTime t = DateTime.Parse(s);
            DateTime t0 = new DateTime(1970, 1, 1, 0, 0, 0);
            return (t.Ticks - t0.Ticks) / 1000 / 10000;
        }
    }
}
