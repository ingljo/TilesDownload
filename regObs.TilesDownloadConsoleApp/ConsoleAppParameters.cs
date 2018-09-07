using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace regObs.TilesDownloadConsoleApp
{
    public class ConsoleAppParameters
    {
        [Option("tiles", Required = false, HelpText = "Tiles url template to download.", Default = "http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom={z}&x={x}&y={y}")]
        public string TilesUrlTemplate { get; set; }

        [Option("min", Required = false, HelpText = "Minimum zoom level to download.", Default = 1)]
        public int MinZoom { get; set; }

        [Option("max", Required = false, HelpText = "Max zoom level to download.", Default = 10)]
        public int MaxZoom { get; set; }

        [Option("path", Required = false, HelpText = "Path to download tiles", Default = "")]
        public string Path { get; set; }

        [Option("geojson", Required = false, HelpText = "Path to geojson file for polygon area to download", Default = "varslingsomraader.json")]
        public string GeoJsonPath { get; set; }

        [Option("name", Required = false, HelpText = "Feature property name.", Default = "OMRAADENAV")]
        public string FeaturePropertyName { get; set; }

        [Option("value", Required = false, HelpText = "Feature property value. For example Hallingdal.")]
        public string FeaturePropertyValue { get; set; }

        [Option("imageformat", Required = false, HelpText = "Image format (jpg or png)", Default = "png")]
        public string ImageFormat { get; set; }

        [Option("writereport", Required = false,  HelpText = "Write report when complete.", Default = true)]
        public bool WriteReport { get; set; }

        [Option("parallell", Required = false, HelpText = "Number of tiles to download in parallell.", Default = 4)]
        public int ParralellTasks { get; set; }

        [Option("skipexisting", Required = false, HelpText = "Skip existing tiles (Continue download from last tile in folder).", Default = false)]
        public bool SkipExisting { get; set; }
    }
}
