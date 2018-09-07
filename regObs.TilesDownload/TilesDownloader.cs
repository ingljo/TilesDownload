using GeoJSON.Net.Feature;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using NLog;

namespace regObs.TilesDownload
{
    public class TilesDownloader
    {
        private string tilesUrlTemplate;
        private DownloadTilesOptions options;
        private Dictionary<string, Tile> errorTiles;

        /**
         * <param name="tilesUrlTemplate">Tiles template url</param>
         * <example>http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom={z}&x={x}&y={y}</example>
         * <param name="options">options to use</param>
         */
        public TilesDownloader(string tilesUrlTemplate, DownloadTilesOptions options = null)
        {
            this.tilesUrlTemplate = tilesUrlTemplate;
            this.options = options != null ? options : new DownloadTilesOptions();
            this.errorTiles = new Dictionary<string, Tile>();
        }

        /**
         * Start download
         */
        public async Task Start()
        {
            LogManager.GetCurrentClassLogger().Info($"Starting download tiles from {this.tilesUrlTemplate}, options: {Newtonsoft.Json.JsonConvert.SerializeObject(this.options)}");
            var tilesToDownload = GetTilesToDownload();
            var client = new HttpClient();
            var progress = new DownloadTilesProgress(tilesToDownload.SelectMany(x=> x.Value.Select(y=>$"{x.Key}_{y.Url}")).ToList());
            progress.Start();

            foreach(var polygon in tilesToDownload)
            {
                foreach (var batch in polygon.Value.Chunkify(this.options.ParallellTasks))
                {
                    SetTileGroup(polygon.Key, batch);
                    await DownloadBatch(batch, progress, client);
                }
            }

            int retryCount = 5;
            while(errorTiles.Count() > 0 && retryCount > 0)
            {
                retryCount--;
                await DownloadBatch(errorTiles.Select(x=>x.Value), progress, client);
            }
            

            if (this.options.WriteReport)
            {
                var report = $"<html><body><table><tr><td>Last run</td><td>{DateTime.Now}</td></tr><tr><td>Tiles downloaded</td><td>{progress.Report.Complete}</td></tr><tr><td>Downloaded to path</td><td>{this.options.Folder}</td></tr><tr><td>Time used</td><td>{progress.Elapsed}</td></tr></table></body></html>";
                File.WriteAllText("report.html", report);
            }
        }

        private void SetTileGroup(string name, IEnumerable<Tile> batch)
        {
            foreach (var tile in batch)
            {
                tile.GroupName = name;
            }
        }

        private async Task DownloadBatch(IEnumerable<Tile> batch, DownloadTilesProgress progress, HttpClient httpClient)
        {
            var tasks = batch.Select(x => x.Download(downloadFolder: this.options.Folder, imageFormat: this.options.ImageFormat, httpClient: httpClient, skipIfExist: this.options.SkipExisting));
            var result = await Task.WhenAll(tasks);
            progress.SetTilesDownloaded(result.Where(x => x.Item2).Select(x => $"{x.Item1.GroupName}_{x.Item1.Url}").ToList());
            foreach(var tileResult in result)
            {
                var id = $"{tileResult.Item1.GroupName}_{tileResult.Item1.Url}";
                if (tileResult.Item2)
                {
                    errorTiles.Remove(id);
                }
                else
                {
                    errorTiles[id] = tileResult.Item1;
                }
            }
            if (this.options.ProgressReporter != null)
            {
                this.options.ProgressReporter.Report(progress.Report);
            }
        }

        private Dictionary<string, List<Tile>> GetTilesToDownload()
        {
            if(!string.IsNullOrWhiteSpace(this.options.GeoJsonFilename))
            {
                var area = GetFeaturesFromGeoJsonFile();
                var tilesToDownload = new Dictionary<string, List<Tile>>();
                foreach (var polygon in area.GetTilesForPolygonsInGeoJon(featureProperty: this.options.FeatureProperty, featurePropertyValue: this.options.FeaturePropertyValue, minZoom: this.options.MinZoom, maxZoom: this.options.MaxZoom, tilesUrlTemplate: this.tilesUrlTemplate))
                {
                    tilesToDownload[polygon.Item1] = polygon.Item2;
                }
                LogManager.GetCurrentClassLogger().Trace($"Polygons to download: {string.Join(", ", tilesToDownload.Select(x=>x.Key))}");


                return tilesToDownload;
            }
            else
            {
                var tiles = ExtensionMethods.GetTilesForWorld(this.options.MinZoom, this.options.MaxZoom, tilesUrlTemplate);
                return new Dictionary<string, List<Tile>>() { { "World", tiles } };
            }
        }

        private FeatureCollection GetFeaturesFromGeoJsonFile()
        {
            var geoJson = File.ReadAllText(this.options.GeoJsonFilename);
            return new GeoJsonReader().Read<FeatureCollection>(geoJson);
        }
       
    }

}
