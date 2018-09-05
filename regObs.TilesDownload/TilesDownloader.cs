using GeoJSON.Net.Feature;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using NLog;
using System.Data.Entity.Spatial;
using System.Globalization;
using Microsoft.SqlServer.Types;

namespace regObs.TilesDownload
{
    public class TilesDownloader
    {
        private string tilesUrlTemplate;
        private DownloadTilesOptions options;

        /**
         * <param name="tilesUrlTemplate">Tiles template url</param>
         * <example>http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom={z}&x={x}&y={y}</example>
         * <param name="options">options to use</param>
         */
        public TilesDownloader(string tilesUrlTemplate, DownloadTilesOptions options = null)
        {
            this.tilesUrlTemplate = tilesUrlTemplate;
            this.options = options != null ? options : new DownloadTilesOptions();
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
                    var tasks = batch.Select(x => x.Download(downloadFolder: this.options.Folder, name: polygon.Key, imageFormat: this.options.ImageFormat, httpClient: client));
                    await Task.WhenAll(tasks);
                    progress.SetTilesDownloaded(batch.Select(x => $"{polygon.Key}_{x.Url}").ToList());
                    if (this.options.ProgressReporter != null)
                    {
                        this.options.ProgressReporter.Report(progress.Report);
                    }
                }
            }
            

            if (this.options.WriteReport)
            {
                var report = $"<html><body><table><tr><td>Last run</td><td>{DateTime.Now}</td></tr><tr><td>Tiles downloaded</td><td>{progress.Report.Complete}</td></tr><tr><td>Downloaded to path</td><td>{this.options.Folder}</td></tr><tr><td>Time used</td><td>{progress.Elapsed}</td></tr></table></body></html>";
                File.WriteAllText("report.html", report);
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
