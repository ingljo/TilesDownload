using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace regObs.TilesDownload
{
    public class DownloadTilesProgress : Stopwatch
    {

        private Dictionary<string, bool> _tilesDownloaded;


        public DownloadTilesProgress(List<string> tilesToDownload) : base()
        {
            _tilesDownloaded = tilesToDownload.ToDictionary(key => key, value => false);
        }

        public void SetTilesDownloaded(List<string> tilesDownloaded)
        {
            tilesDownloaded.ForEach(x => _tilesDownloaded[x] = true);
        }

        public DownloadTilesProgressReport Report
        {
            get
            {
                return new DownloadTilesProgressReport
                {
                    Complete = _tilesDownloaded.Count(x => x.Value),
                    Total = _tilesDownloaded.Count(),
                    ElapsedMilliseconds = this.ElapsedMilliseconds,
                };
            }
        }
    }
}
