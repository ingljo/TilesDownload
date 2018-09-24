using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using NLog;
using DotSpatial.Data;
using DotSpatial.Projections;
using DotSpatial.Topology;

namespace regObs.TilesDownload
{
    public class TilesDownloader
    {
        private DownloadTilesOptions options;
        private List<Tile> errorTiles;
        private IProgress<DownloadTilesProgressReport> progressReporter;


        public TilesDownloader(DownloadTilesOptions options, IProgress<DownloadTilesProgressReport> progressReporter = null)
        {
            this.options =  options;
            this.errorTiles = new List<Tile>();
            this.progressReporter = progressReporter;
        }

        /**
         * Start download
         */
        public async Task Start()
        {
            LogManager.GetCurrentClassLogger().Info($"Starting download tiles from {this.options.TileSourceUrls}, options: {Newtonsoft.Json.JsonConvert.SerializeObject(this.options)}");
            var tilesInPolygon = GetTilesToDownloadFromPolygon();
            var tilesToDownload = GetTilesToDownloadForAllSources(tilesInPolygon);
            var client = new HttpClient();
            var progress = new DownloadTilesProgress(tilesToDownload);
            progress.Start();


            foreach (var batch in tilesToDownload.Chunkify(this.options.ParallellTasks))
            {
                await DownloadBatch(batch, progress, client);
            }

            await RetryDownloadOfFailedTiles(client, progress);

            if (errorTiles.Count > 0)
            {
                LogManager.GetCurrentClassLogger().Error($"Tiles left to download: ${string.Join(", ", errorTiles)}");
                throw new ApplicationException("Could not download all tiles!");
            }

            if (this.options.WriteMetadata)
            {
                var folders = tilesToDownload.GroupBy(x => new { x.TileName, x.GroupName }).Select((g) => new { key = $"{g.Key.GroupName}//{g.Key.TileName}", metadata = Newtonsoft.Json.JsonConvert.SerializeObject(g.Select(t => $"{t.TileName}/{t.Z}/tile_{t.X}_{t.Y}")) });
                foreach (var folder in folders)
                {
                    var path = this.options.Path + "//" + folder.key +"//" +"tiles.json";
                    File.WriteAllText(path, folder.metadata);
                }
            }


            if (this.options.WriteReport)
            {
                var report = $"<html><body><table><tr><td>Last run</td><td>{DateTime.Now}</td></tr><tr><td>Tiles downloaded</td><td>{progress.Report.Complete}</td></tr><tr><td>Downloaded to path</td><td>{this.options.Path}</td></tr><tr><td>Time used</td><td>{progress.Elapsed}</td></tr></table></body></html>";
                File.WriteAllText("report.html", report);
            }
        }

        private List<Tile> GetTilesToDownloadForAllSources(Dictionary<string, List<Point>> tilesInPolygon)
        {
            var allTiles = new List<Tile>();
            for (int i = 0; i < this.options.TileSourceUrls.Count(); i++)
            {
                var name = this.options.TilesNames.Count() > i ? this.options.TilesNames.ToList()[i] : $"tile_{i}";
                var urlTemplate = this.options.TileSourceUrls.ToList()[i];
                var tileUrlForCurrentTile = tilesInPolygon.SelectMany(x => x.Value.Select(y => new Tile(x: (int)y.X, y: (int)y.Y, z: (int)y.Z, groupName: x.Key, tileName: name, urlTemplate: urlTemplate))).ToList();
                allTiles.AddRange(tileUrlForCurrentTile);
            }

            return allTiles;
        }

        private async Task RetryDownloadOfFailedTiles(HttpClient client, DownloadTilesProgress progress)
        {
            int retryCount = this.options.RetryCount;
            while (errorTiles.Count() > 0 && retryCount > 0)
            {
                retryCount--;
                foreach (var batch in errorTiles.Chunkify(this.options.ParallellTasks))
                {
                    await DownloadBatch(batch, progress, client);
                }
            }
        }

        private async Task DownloadBatch(IEnumerable<Tile> batch, DownloadTilesProgress progress, HttpClient httpClient)
        {
            var tasks = batch.Select(x => x.Download(downloadFolder: this.options.Path, imageFormat: this.options.ImageFormat, httpClient: httpClient, skipIfExist: this.options.SkipExisting));
            var result = await Task.WhenAll(tasks);
            progress.SetTilesDownloaded(result.Where(x => x.Item2).Select(x => x.Item1).ToList());
            foreach(var tileResult in result)
            {
                if (tileResult.Item2)
                {
                    errorTiles.Remove(tileResult.Item1);
                }
                else
                {
                    if (!errorTiles.Contains(tileResult.Item1))
                    {
                        errorTiles.Add(tileResult.Item1);
                    }
                }
            }
            if (this.progressReporter != null)
            {
                this.progressReporter.Report(progress.Report);
            }
        }

        public Dictionary<string, List<Point>> GetTilesToDownloadFromPolygon()
        {
            var tilesToDownload = new Dictionary<string, List<Point>>();
            var indexMapFile = Shapefile.OpenFile(this.options.ShapeFile);
            indexMapFile.Reproject(ProjectionInfo.FromEpsgCode(4326));

            // Get the map index from the Feature data
            for (int i = 0; i < indexMapFile.DataTable.Rows.Count; i++)
            {

                // Get the feature
                IFeature feature = indexMapFile.Features.ElementAt(i);

                var polygon = feature.BasicGeometry as Polygon;
                var name = (string)feature.DataRow[1];           

                if(!string.IsNullOrWhiteSpace(this.options.FeaturePropertyValue))
                {
                    if (this.options.FeaturePropertyValue != name)
                    {
                        continue;
                    }
                }
                tilesToDownload[name] = polygon.GetTiles(name, this.options.MinZoom, this.options.MaxZoom);
                // Now it's very quick to iterate through and work with the feature.
            }
            return tilesToDownload;
        }
       
    }

}
