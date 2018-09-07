using System;
using System.Collections.Generic;
using System.Text;

namespace regObs.TilesDownload
{
    public class Tile
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public int Z { get; private set; }

        public string GroupName { get; set; }

        private string urlTemplate;

        public string Url
        {
            get
            {
                return urlTemplate.Replace("{z}", Z.ToString()).Replace("{x}", X.ToString()).Replace("{y}", Y.ToString());
            }
        }

        public Tile(int x, int y, int z, string urlTemplate)
        {
            X = x;
            Y = y;
            Z = z;
            this.urlTemplate = urlTemplate;
        }
    }
}
