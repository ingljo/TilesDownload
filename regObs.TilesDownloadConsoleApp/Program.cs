using CommandLine;
using regObs.TilesDownload;
using System;
using System.Linq;

namespace regObs.TilesDownloadConsoleApp
{
    public class Program
    {
        const long estimatedSizePerTile = 30000;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<DownloadTilesOptions>(args).WithParsed(o =>
            {
                var progressReporter = new Progress<DownloadTilesProgressReport>(p =>
                {
                    Console.Write("\r{0}", p.ToString());
                });

                var downloader = new TilesDownloader(o, progressReporter);

                if (o.Debug)
                {
                    var tiles = downloader.GetTilesToDownloadFromPolygon();
                    var esimatedDownload = tiles.Select((x) => new {
                        name= x.Key, 
                        numberOfTiles= x.Value.Count, 
                        estimatedSize= (x.Value.Count * estimatedSizePerTile), 
                        zoomLevels= x.Value.GroupBy(y => y.Z).Select(y => new { zoom=y.Key, numberOfTiles=y.Count(), estimatedSize=(y.Count() * estimatedSizePerTile) }) 
                    });
                    foreach (var item in esimatedDownload) {
                        Console.WriteLine($"{item.name}");
                        Console.WriteLine("--------------------");
                        foreach(var zoom in item.zoomLevels)
                        {
                            Console.WriteLine($"Zoom level {zoom.zoom}. Number of tiles: {zoom.numberOfTiles}. Estimated size: {zoom.estimatedSize.BytesToString()} ({zoom.estimatedSize} bytes)");
                        }
                        Console.WriteLine($"Total. Number of tiles: {item.numberOfTiles}. Estimated size: {item.estimatedSize.BytesToString()} ({item.estimatedSize} bytes)");
                    }
                    Environment.Exit(-1);
                }
                else
                {
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
                }
            });
        }

        
    }
}
