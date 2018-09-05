using GeoJSON.Net;
using GeoJSON.Net.Contrib.EntityFramework;
using GeoJSON.Net.Feature;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace regObs.TilesDownload
{
    public static class ExtensionMethods
    {
        public const double MinLatitude = -85.05112878;
        public const double MaxLatitude = 85.05112878;
        public const double MinLongitude = -180;
        public const double MaxLongitude = 180;

        public static async Task Download(this Tile tile, string downloadFolder, string name, ImageFormat imageFormat = ImageFormat.png, HttpClient httpClient = null)
        {
            LogManager.GetCurrentClassLogger().Trace(() => $"Downloading tile {tile.Url}");
            var client = httpClient != null ? httpClient : new HttpClient();
            var result = await client.GetAsync(tile.Url);
            if (result.IsSuccessStatusCode)
            {
                var stream = await result.Content.ReadAsStreamAsync();
                var directory = $"{downloadFolder}\\{name}\\{tile.Z}";
                CreateFolderIfNotExists(directory);
                var filePath = $"{directory}\\tile_{tile.X}_{tile.Y}.{imageFormat}";
                using (var fs = File.OpenWrite(filePath))
                {
                    await stream.CopyToAsync(fs);
                }
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

        public static List<Tile> GetTiles(this DbGeography area, int minZoom, int maxZoom, string tilesUrl)
        {
            var tiles = new List<Tile>();
            for (int z = minZoom; z <= maxZoom; z++)
            {
                var ranges = GetTileRange(area, z);
                var x_range = ranges.Item1;
                var y_range = ranges.Item2;


                for (int y = y_range.Item1; y < y_range.Item2 + 1; y++)
                {
                    for (int x = x_range.Item1; x < x_range.Item2 + 1; x++)
                    {
                        if (area.DoesTileIntersects(x, y, z))
                            tiles.Add(new Tile(x, y, z, tilesUrl));
                    }
                }
            }
            return tiles;
        }

        public static List<Tile> GetTilesForWorld(int minZoom, int maxZoom, string tilesUrl)
        {
            var tiles = new List<Tile>();
            for (int z = minZoom; z <= maxZoom; z++)
            {
                var starting = LatLongToTileXY(MinLatitude, MinLongitude, z);
                var ending = LatLongToTileXY(MaxLatitude, MaxLongitude, z);

                var x_range = Tuple.Create(starting.Item1, ending.Item1);
                var y_range = Tuple.Create(ending.Item2, starting.Item2);


                for (int y = y_range.Item1; y < y_range.Item2 + 1; y++)
                {
                    for (int x = x_range.Item1; x < x_range.Item2 + 1; x++)
                    {
                        
                        tiles.Add(new Tile(x, y, z, tilesUrl));
                    }
                }
            }
            return tiles;
        }

        public static bool DoesTileIntersects(this DbGeography area, int x, int y, int z)
        {
            var tile = GetTileASpolygon(x, y, z);
            bool intersects = area.Intersects(tile);
            return intersects;
        }

        public static IEnumerable<Tuple<string, List<Tile>>> GetTilesForPolygonsInGeoJon(this FeatureCollection featureCollection, string featureProperty, string featurePropertyValue, int minZoom, int maxZoom, string tilesUrlTemplate)
        {
            // loop through all the parsed featurd   
            for (int featureIndex = 0;
                 featureIndex < featureCollection.Features.Count;
                 featureIndex++)
            {
                // get json feature
                var jsonFeature = featureCollection.Features[featureIndex];

                if (!string.IsNullOrWhiteSpace(featurePropertyValue) && (string)jsonFeature.Properties[featureProperty] != featurePropertyValue)
                    continue;

                // get geometry type to create appropriate geometry
                switch (jsonFeature.Geometry.Type)
                {
                    case GeoJSONObjectType.Point:
                        break;
                    case GeoJSONObjectType.MultiPoint:
                        break;
                    case GeoJSONObjectType.LineString:
                        break;
                    case GeoJSONObjectType.MultiLineString:
                        break;
                    case GeoJSONObjectType.Polygon:
                        {
                            var polygon = jsonFeature.Geometry as GeoJSON.Net.Geometry.Polygon;
                            var name = (string)jsonFeature.Properties[featureProperty];
                            yield return new Tuple<string, List<Tile>>(name, polygon.ToDbGeography().GetTiles(minZoom, maxZoom, tilesUrlTemplate));
                            break;
                        }
                    case GeoJSONObjectType.MultiPolygon:
                        break;
                    case GeoJSONObjectType.GeometryCollection:
                        break;
                    case GeoJSONObjectType.Feature:
                        break;
                    case GeoJSONObjectType.FeatureCollection:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            yield break;
        }

        private static DbGeography GetTileASpolygon(int x, int y, int z)
        {
            var nw = TileXYToLatLong(x, y, z);
            var se = TileXYToLatLong(x + 1, y + 1, z);
            var northLat = nw.Item1.ToString(CultureInfo.InvariantCulture);
            var westLng = nw.Item2.ToString(CultureInfo.InvariantCulture);
            var southLat = se.Item1.ToString(CultureInfo.InvariantCulture);
            var eastLng = se.Item2.ToString(CultureInfo.InvariantCulture);
            var northWest = $"{westLng} {northLat}";
            var northEast = $"{eastLng} {northLat}";
            var southEast = $"{eastLng} {southLat}";
            var southWest = $"{westLng} {southLat}";

            var text = $"POLYGON(({northWest}, {northEast}, {southEast}, {southWest}, {northWest}))";
            return DbGeography.FromText(text, 4326);
        }

        private static Tuple<Tuple<int, int>, Tuple<int, int>> GetTileRange(this DbGeography area, int zoom)
        {
            //minimum bounding region (xm, ym, xmx, ymx)
            string dbGeography = area.AsText();
            var dbGeometry = DbGeometry.FromText(dbGeography);
            var bnds = dbGeometry.Envelope;

            double xmin = bnds.PointAt(1).XCoordinate.Value;
            double xmax = bnds.PointAt(3).XCoordinate.Value;
            double ymin = bnds.PointAt(1).YCoordinate.Value;
            double ymax = bnds.PointAt(3).YCoordinate.Value;

            var starting = LatLongToTileXY(ymin, xmin, zoom);
            var ending = LatLongToTileXY(ymax, xmax, zoom);

            var x_range = Tuple.Create(starting.Item1, ending.Item1);
            var y_range = Tuple.Create(ending.Item2, starting.Item2);

            return Tuple.Create(x_range, y_range);
        }

        private static Tuple<int, int> LatLongToTileXY(double latitude, double longitude, int z)
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

            return Tuple.Create(tileX, tileY);
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
        private static Tuple<double, double> TileXYToLatLong(int tileX, int tileY, int z)
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
            return Tuple.Create(latitude, longitude);
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
    }
}
