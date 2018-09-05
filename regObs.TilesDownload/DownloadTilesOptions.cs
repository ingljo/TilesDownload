using System;

namespace regObs.TilesDownload
{
    public class DownloadTilesOptions
    {
        public string GeoJsonFilename { get; set; }

        public string FeaturePropertyValue { get; set; }

        public string FeatureProperty { get; set; }

        public int MinZoom { get; set; }

        public int MaxZoom { get; set; }

        public string Folder { get; set; }

        public ImageFormat ImageFormat { get; set; }
        
        public int ParallellTasks { get; set; }

        public IProgress<DownloadTilesProgressReport> ProgressReporter { get; set; }

        public bool WriteReport { get; set; }

        public DownloadTilesOptions(
            string folder = "", 
            IProgress<DownloadTilesProgressReport> progressReporter = null, 
            int minZoom = 1, 
            int maxZoom = 5, 
            ImageFormat imageFormat = ImageFormat.png, 
            int parallellTasks = 4, 
            string featureProperty = "OMRAADENAV", 
            string featurePropertyValue = "", 
            string geoJsonFilename = "varslingsomraader.json",
            bool writeReport = true)
        {
            MinZoom = minZoom;
            MaxZoom = maxZoom;
            ImageFormat = imageFormat;
            ParallellTasks = parallellTasks;
            FeatureProperty = featureProperty;
            FeaturePropertyValue = featurePropertyValue;
            GeoJsonFilename = geoJsonFilename;
            Folder = folder;
            ProgressReporter = progressReporter;
            WriteReport = writeReport;
        }
    }
}
