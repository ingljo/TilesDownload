using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using DotSpatial.Topology;

namespace regObs.TilesDownload
{
    public static class ExtensionMethods
    {
        public const double MinLatitude = -85.05112878;
        public const double MaxLatitude = 85.05112878;
        public const double MinLongitude = -180;
        public const double MaxLongitude = 180;

        public static async Task<Tuple<Tile, bool>> Download(this Tile tile, string downloadFolder, ImageFormat imageFormat = ImageFormat.png, HttpClient httpClient = null, bool skipIfExist = false)
        {
            LogManager.GetCurrentClassLogger().Trace(() => $"Downloading tile {tile.Url}");
            var directory = $"{downloadFolder}\\{tile.GroupName}\\{tile.TileName}\\{tile.Z}";
            var filePath = $"{directory}\\tile_{tile.X}_{tile.Y}.{imageFormat}";
            if(skipIfExist && File.Exists(filePath))
            {
                return new Tuple<Tile, bool>(tile, true);
            }
            CreateFolderIfNotExists(directory);
            var client = httpClient != null ? httpClient : new HttpClient();

            try
            {
                var result = await client.GetAsync(tile.Url);
                if (result.IsSuccessStatusCode)
                {
                    var stream = await result.Content.ReadAsStreamAsync();

                    using (var fs = File.OpenWrite(filePath))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }

                return new Tuple<Tile, bool>(tile, true);
            }
            catch(Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "Could not dowload tile");
                return new Tuple<Tile, bool>(tile, false);
            }
        }

        private static void CreateFolderIfNotExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static IEnumerable<IEnumerable<T>> Chunkify<T>(this IEnumerable<T> source, int size)
        {
            int count = 0;
            using (var iter = source.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    var chunk = new T[size];
                    count = 1;
                    chunk[0] = iter.Current;
                    for (int i = 1; i < size && iter.MoveNext(); i++)
                    {
                        chunk[i] = iter.Current;
                        count++;
                    }
                    if (count < size)
                    {
                        Array.Resize(ref chunk, count);
                    }
                    yield return chunk;
                }
            }
        }

        public static List<Point> GetTiles(this Polygon area, string name, int minZoom, int maxZoom)
        {
            var tiles = new List<Point>();
            for (int z = minZoom; z <= maxZoom; z++)
            {
                var ranges = GetTileRange(area, z);
                var x_range = ranges.Item1;
                var y_range = ranges.Item2;


                for (int y = y_range.From; y < y_range.To + 1; y++)
                {
                    for (int x = x_range.From; x < x_range.To + 1; x++)
                    {
                        if (area.DoesTileIntersects(name, x, y, z))
                            tiles.Add(new Point(x, y, z));
                    }
                }
            }
            return tiles;
        }

        //public static List<Tile> GetTilesForWorld(int minZoom, int maxZoom, string tilesUrl)
        //{
        //    var tiles = new List<Tile>();
        //    for (int z = minZoom; z <= maxZoom; z++)
        //    {
        //        var starting = LatLongToTileXY(MinLatitude, MinLongitude, z);
        //        var ending = LatLongToTileXY(MaxLatitude, MaxLongitude, z);

        //        var x_range = Tuple.Create(starting.Item1, ending.Item1);
        //        var y_range = Tuple.Create(ending.Item2, starting.Item2);


        //        for (int y = y_range.Item1; y < y_range.Item2 + 1; y++)
        //        {
        //            for (int x = x_range.Item1; x < x_range.Item2 + 1; x++)
        //            {
                        
        //                tiles.Add(new Tile(x, y, z, tilesUrl));
        //            }
        //        }
        //    }
        //    return tiles;
        //}

        public static bool DoesTileIntersects(this Polygon area, string name, int x, int y, int z)
        {
            var tilePolygon = GetTileAsPolygon(x, y, z);
            return area.Contains(tilePolygon) || area.Intersects(tilePolygon);
        }

        private static Polygon GetTileAsPolygon(int x, int y, int z)
        {
            var nw = TileXYToLatLong(x, y, z);
            var se = TileXYToLatLong(x + 1, y + 1, z);
            return new Polygon(new Coordinate[] {
                new Coordinate(nw.X, nw.Y),
                new Coordinate(se.X, nw.Y),
                new Coordinate(se.X, se.Y),
                new Coordinate(nw.X, se.Y),
            });
        }

        private static Tuple<Range, Range> GetTileRange(this Polygon area, int zoom)
        {
            //minimum bounding region (xm, ym, xmx, ymx)
            var bnds = area.Envelope;

            double xmin = bnds.BottomLeft().X;
            double xmax = bnds.BottomRight().X;
            double ymin = bnds.BottomLeft().Y;
            double ymax = bnds.TopLeft().Y;

            var starting = LatLongToTileXY(ymin, xmin, zoom);
            var ending = LatLongToTileXY(ymax, xmax, zoom);

            var x_range = new Range((int)starting.X, (int)ending.X);
            var y_range = new Range((int)ending.Y, (int)starting.Y);

            return Tuple.Create(x_range, y_range);
        }

        private static Point LatLongToTileXY(double latitude, double longitude, int z)
        {
            int tileX;
            int tileY;
            latitude = Clip(latitude, MinLatitude, MaxLatitude);
            longitude = Clip(longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180) / 360;
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

            uint mapSize = MapSize(z);
            tileX = (int)Clip(x * mapSize + 0.5, 0, mapSize - 1) / 256;
            tileY = (int)Clip(y * mapSize + 0.5, 0, mapSize - 1) / 256;

            return new Point(tileX, tileY);
        }

        /// <summary>
        /// Converts a pixel from pixel XY coordinates at a specified level of detail
        /// into latitude/longitude WGS-84 coordinates (in degrees).
        /// </summary>
        /// <param name="tileX">X coordinate of the point, in pixels.</param>
        /// <param name="tileY">Y coordinates of the point, in pixels.</param>
        /// <param name="z">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="latitude">Output parameter receiving the latitude in degrees.</param>
        /// <param name="longitude">Output parameter receiving the longitude in degrees.</param>
        private static Coordinate TileXYToLatLong(int tileX, int tileY, int z)
        {
            double latitude;
            double longitude;
            int pixelX = tileX * 256;
            int pixelY = tileY * 256;
            double mapSize = MapSize(z);
            double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
            double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

            latitude = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
            longitude = 360 * x;
            return new Coordinate(longitude, latitude);
        }

        /// <summary>
        /// Clips a number to the specified minimum and maximum values.
        /// </summary>
        /// <param name="n">The number to clip.</param>
        /// <param name="minValue">Minimum allowable value.</param>
        /// <param name="maxValue">Maximum allowable value.</param>
        /// <returns>The clipped value.</returns>
        private static double Clip(double n, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }

        /// <summary>
        /// Determines the map width and height (in pixels) at a specified level
        /// of detail.
        /// </summary>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>The map width and height in pixels.</returns>
        private static uint MapSize(int levelOfDetail)
        {
            return (uint)256 << levelOfDetail;
        }

        public static string BytesToString(this long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
