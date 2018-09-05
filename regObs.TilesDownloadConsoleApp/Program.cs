using CommandLine;
using regObs.TilesDownload;
using System;
using System.Data.Entity.SqlServer;

namespace regObs.TilesDownloadConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            SqlProviderServices.SqlServerTypesAssemblyName = "Microsoft.SqlServer.Types, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
            Parser.Default.ParseArguments<ConsoleAppParameters>(args).WithParsed(o =>
            {
                var progress = new Progress<DownloadTilesProgressReport>(p =>
                {
                    Console.Write("\r{0}", p.ToString());
                });

                var downloader = new TilesDownloader(o.TilesUrlTemplate, new DownloadTilesOptions(
                    folder: string.IsNullOrWhiteSpace(o.Path) ? AppDomain.CurrentDomain.BaseDirectory : o.Path,
                    progressReporter: progress,
                    minZoom: o.MinZoom,
                    maxZoom: o.MaxZoom,
                    imageFormat: o.ImageFormat == "jpg" ? ImageFormat.jpg : ImageFormat.png,
                    featureProperty: o.FeaturePropertyName,
                    featurePropertyValue: o.FeaturePropertyValue,
                    geoJsonFilename: o.GeoJsonPath,
                    writeReport: o.WriteReport,
                    parallellTasks: o.ParralellTasks));

                try
                {
                    downloader.Start().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var errorMessage = "Error while downloading tiles!";
                    NLog.LogManager.GetCurrentClassLogger().Error(ex, errorMessage);
                    Console.WriteLine($"\n{errorMessage}. Please see log file for details.");
                    Environment.Exit(-1);
                }

                Console.WriteLine("\nDone!");
                Environment.Exit(0);
            });
        }
    }
}
