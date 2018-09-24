using DotSpatial.Data;
using DotSpatial.Projections;
using DotSpatial.Topology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace regObs.TilesDownload.Tests
{
    [TestClass]
    public class UnitTest1
    {

        private Polygon GetPolygonOfAkerBryggeBislettAndGronland()
        {
            var polygon = new Polygon(new Coordinate[] {
                new Coordinate(10.7244832, 59.9082588),
                new Coordinate(10.7581288, 59.9121747),
                new Coordinate(10.7334954, 59.9247368),
                new Coordinate(10.7244832, 59.9082588),
            });
            return polygon;
        }

        private Polygon GetLuster()
        {
            Shapefile indexMapFile = Shapefile.OpenFile("kommune2018.shp");
            indexMapFile.Reproject(ProjectionInfo.FromEpsgCode(4326));
            Polygon polygon = null;

            // Get the map index from the Feature data
            for (int i = 0; i < indexMapFile.DataTable.Rows.Count; i++)
            {

                // Get the feature
                IFeature feature = indexMapFile.Features.ElementAt(i);
                if ((string)feature.DataRow[1] == "Luster")
                {

                    polygon = feature.BasicGeometry as Polygon;
                }
            }
            return polygon;
        }

        [TestMethod]
        public void Test_Tile_Of_Stortinget_Is_Inside_Polygon_Of_Akerbrygge_Bislett_And_Gronland()
        {
            var area = GetPolygonOfAkerBryggeBislettAndGronland();

            // Tile of stortinget: http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom=17&x=69446&y=38126
            var tileShouldIntersect = area.DoesTileIntersects("Test", 69446, 38126, 17);

            Assert.IsTrue(tileShouldIntersect);
        }

        [TestMethod]
        public void Test_Tile_Of_Damstredet_Is_Not_Inside_Polygon_Of_Akerbrygge_Bislett_And_Gronland()
        {
            var area = GetPolygonOfAkerBryggeBislettAndGronland();

            // Tile of stortinget: http://opencache.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=norgeskart_bakgrunn&zoom=17&x=69448&y=38121
            var tileShouldNotIntersect = area.DoesTileIntersects("Test", 69448, 38121, 17);

            Assert.IsFalse(tileShouldNotIntersect);
        }


        [TestMethod]
        public void Test_Tile_Outside_Polygon_Of_Luster()
        {
            var luster = GetLuster();
            var tileShouldNotIntersect = luster.DoesTileIntersects("Test", 265, 145, 9);
            Assert.IsFalse(tileShouldNotIntersect);
        }

        [TestMethod]
        public void Test_Tile_Inside_Polygon_Of_Luster()
        {
            var luster = GetLuster();
            var tileShouldIntersect = luster.DoesTileIntersects("Test", 532, 288, 10);
            Assert.IsTrue(tileShouldIntersect);
        }

        [TestMethod]
        public void Test_Tile_OnSide_Polygon_Of_Luster()
        {
            var luster = GetLuster();
            var tileShouldIntersect = luster.DoesTileIntersects("Test", 266, 145, 9);
            Assert.IsTrue(tileShouldIntersect);
        }
    }
}
