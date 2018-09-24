using CommandLine;
using System.Collections.Generic;

namespace regObs.TilesDownload
{
    public class DownloadTilesOptions
    {
        [Option("tiles", Required = false, HelpText = "Tiles url template to download. (Comma separated)", Separator = ',', Default = new string[] {
            "http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom={z}&x={x}&y={y}",
            "http://gis2.nve.no/arcgis/rest/services/wmts/Kvikkleire_Jordskred/MapServer/tile/{z}/{y}/{x}",
            "http://gis3.nve.no/arcgis/rest/services/wmts/Flomsoner1/MapServer/tile/{z}/{y}/{x}",
            "http://gis3.nve.no/arcgis/rest/services/wmts/Bratthet/MapServer/tile/{z}/{y}/{x}",
            "http://gis3.nve.no/arcgis/rest/services/wmts/SvekketIs/MapServer/tile/{z}/{y}/{x}",
        })]
        public IEnumerable<string> TileSourceUrls { get; set; }

        [Option("tilenames", Required = false, HelpText = "Tile source names. (Comma separated)", Separator=',', Default = new string[] { "topo", "clayzones", "floodzoones", "steepness", "weakenedice" })]
        public IEnumerable<string> TilesNames { get; set; }

        [Option("min", Required = false, HelpText = "Minimum zoom level to download.", Default = 1)]
        public int MinZoom { get; set; }

        [Option("max", Required = false, HelpText = "Max zoom level to download.", Default = 10)]
        public int MaxZoom { get; set; }

        [Option("path", Required = false, HelpText = "Path to download tiles", Default = "")]
        public string Path { get; set; }

        [Option("shapefile", Required = false, HelpText = "Path to shape file for polygon area to download")]
        public string ShapeFile { get; set; }

        [Option("name", Required = false, HelpText = "Feature property name. (For shape files with multiple features)")]
        public string FeaturePropertyName { get; set; }

        [Option("value", Required = false, HelpText = "Feature property value. (For shape files with multiple features. For example Hallingdal.)")]
        public string FeaturePropertyValue { get; set; }

        [Option("imageformat", Required = false, HelpText = "Image format (jpg or png)", Default = ImageFormat.png)]
        public ImageFormat ImageFormat { get; set; }

        [Option("writereport", Required = false,  HelpText = "Write report when complete.", Default = true)]
        public bool WriteReport { get; set; }

        [Option("writemetadata", Required = false, HelpText = "Write metadata", Default = true)]
        public bool WriteMetadata { get; set; }

        [Option("parallell", Required = false, HelpText = "Number of tiles to download in parallell.", Default = 4)]
        public int ParallellTasks { get; set; }

        [Option("skipexisting", Required = false, HelpText = "Skip existing tiles (Continue download from last tile in folder).", Default = false)]
        public bool SkipExisting { get; set; }

        [Option("debug", Required = false, HelpText = "Debug number of tiles and estimated size.", Default = false)]
        public bool Debug { get; set; }

        [Option("zip", Required = false, HelpText = "Zip folder when complete", Default = false)]
        public bool ZipOnComplete { get; set; }

        [Option("retry", Required = false, HelpText = "How many times to retry download of failed tiles", Default = 5)]
        public int RetryCount { get; set; }
    }
}
