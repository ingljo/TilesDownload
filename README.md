# Download tiles from map server
This tool download tiles from map server for all zoom levels specified.
Specify a geojson file with one or more polygons to limit area for download if needed.

## Usage
Run command line application with following parameters:

| Parameter     | Description       | Default            | Example            |
| ------------- |-------------------| -------------------| -------------------|
| -h, --help    | Display help text |                    |                    |
| --tiles       | Tiles url         | http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom={z}&x={x}&y={y} | |
| --min         | Lowest zoom level | 1                  |                    |
| --max         | Highest zoom level. Max zoom level for tiles is often between 17 and 22 | 10   |   |
| --path        | Path to save tiles | Current folder    | "C:\tmp"           |
| --geojson     | Full path to geojson file with polygon areas to download |  varslingsomraader.json | |
| --property        | Feature property from geosjon to use as name | OMRAADENAV | |
| --name       | Feature property value in named property | | Hallingdal |
| --imageformat | Image format to save tiles. Use the same as tiles. | png | |
| --writereport | Write HTML report at end | true | |
| --parallell   | Number of tiles to download in parallell. Increase to download faster, but tiles server might be overloaded! | 4 |  |

### Example
regObs.TilesDownloadConsoleApp.exe --max 16 --name "Lofoten og Vesterålen" --path "C:\tmp"

## View downloaded tiles
View downloaded tiles by opening the test.html in the directory of the downloaded files. Set folder to use in the top right folder control.


## Polygons in avalanch regions
Regions in varslingsomraader.json is taken from https://github.com/ragnarekker/varsomdata/tree/master/varsomdata/input/forecastregionshapes and converted to Lat Lng coordinates using: https://ogre.adc4gis.com/
Target SRS: +proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs
(Create a zip package containing the .shp, dbf, .prj, etc files.)


## Example sizes

| Area       | Zoom levels | Tiles        | Size       |
|------------|-------------|--------------|------------|
| Hallingdal | 1 - 16      |  129 330     | 3,51 GB    |
| Lofoten og Vesterålen | 1-16 | | |