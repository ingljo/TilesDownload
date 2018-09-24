using System;
using System.Collections.Generic;
using System.Text;

namespace regObs.TilesDownload
{
    public class Range
    {
        public int From { get; private set; }
        public int To { get; private set; }

        public Range(int from, int to)
        {
            this.From = from;
            this.To = to;
        }
    }
}
