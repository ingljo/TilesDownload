using System;
using System.Collections.Generic;
using System.Text;

namespace regObs.TilesDownload
{
    public class DownloadTilesProgressReport
    {
       

        public long ElapsedMilliseconds { get; set; }
        public long Total { get; set; }
        public long Complete { get; set; }

        public long Left
        {
            get
            {
                return Total - Complete;
            }
        }

        public double Percentage { get
            {
                if(Complete > 0 && Total > 0)
                {
                    return (double)Complete / (double)Total;
                }
                else
                {
                    return 0.0;
                }           
            }
        }

        public long EstimatedMillisecondsLeft
        {
            get
            {
                var millisecondsPerTile = (double)ElapsedMilliseconds / (double)Complete;
                return (long)Math.Round(Left * millisecondsPerTile);
            }
        }

        public override string ToString()
        {
            return $"{Math.Round(this.Percentage * 100)} % Complete. Estimated time left: {TimeSpan.FromMilliseconds(EstimatedMillisecondsLeft)}. Elapsed: {TimeSpan.FromMilliseconds(this.ElapsedMilliseconds)}";
        }


    }
}
