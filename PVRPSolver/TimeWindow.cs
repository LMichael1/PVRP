using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public class TimeWindow
    {
        public int Start { get; set; }
        public int End { get; set; }

        public TimeWindow(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}
