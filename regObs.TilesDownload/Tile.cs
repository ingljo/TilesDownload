using System;
using System.Collections.Generic;
using System.Text;

namespace regObs.TilesDownload
{
    public class Tile : IEquatable<Tile>
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public int Z { get; private set; }

        public string GroupName { get; private set; }

        public string TileName { get; private set; }

        private string urlTemplate;

        public string Url
        {
            get
            {
                return urlTemplate.Replace("{z}", Z.ToString()).Replace("{x}", X.ToString()).Replace("{y}", Y.ToString());
            }
        }

        public Tile(int x, int y, int z, string tileName, string groupName, string urlTemplate)
        {
            X = x;
            Y = y;
            Z = z;
            TileName = tileName;
            GroupName = groupName;
            this.urlTemplate = urlTemplate;
        }

        public override string ToString()
        {
            return $"x:{X}, y:{Y}, z:{Z}, group: {GroupName}, name: {TileName}";
        }

        public bool Equals(Tile other)
        {
            return this.X == other.X && this.Y == other.Y && this.Z == other.Z && this.TileName == other.TileName && this.GroupName == other.GroupName;
        }
    }
}
