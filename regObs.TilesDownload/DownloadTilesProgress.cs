using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace regObs.TilesDownload
{
    public class DownloadTilesProgress : Stopwatch
    {

        private Dictionary<Tile, bool> _tilesDownloaded;
        private List<Tile> _errorTiles;

        public bool HasError
        {
            get
            {
                return _errorTiles.Count() > 0;
            }
        }


        public DownloadTilesProgress(List<Tile> tilesToDownload) : base()
        {
            _tilesDownloaded = tilesToDownload.ToDictionary(key => key, value => false);
            _errorTiles = new List<Tile>();
        }

        public void SetTilesDownloaded(List<Tile> tilesDownloaded)
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

        internal void SetError(List<Tile> errorTiles)
        {
            _errorTiles.AddRange(errorTiles);
            _errorTiles = _errorTiles.Distinct().ToList();
        }
    }
}
