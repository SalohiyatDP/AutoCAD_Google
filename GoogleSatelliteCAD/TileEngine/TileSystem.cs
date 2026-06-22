using System;
using System.Collections.Generic;

namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// Slippy-map (Google/OSM/Web Mercator) tile matematikasi.
    /// Geografik koordinatalardan tile indekslarini va aksincha hisoblaydi.
    ///
    /// Tile formulalari (texnik talabga muvofiq):
    ///   tileX = floor((lon + 180) / 360 * 2^zoom)
    ///   tileY = floor((1 - ln(tan(lat) + sec(lat)) / PI) / 2 * 2^zoom)
    /// </summary>
    public static class TileSystem
    {
        /// <summary>Tile o'lchami piksellarda (256 x 256).</summary>
        public const int TileSize = 256;

        /// <summary>Eng kichik zoom darajasi.</summary>
        public const int MinZoom = 0;

        /// <summary>Eng katta zoom darajasi.</summary>
        public const int MaxZoom = 22;

        private const double DegToRad = Math.PI / 180.0;

        /// <summary>Berilgan zoomda bir o'qdagi tilelar soni (2^zoom).</summary>
        public static int MapSizeTiles(int zoom) => 1 << zoom;

        /// <summary>lon/lat (gradus) -> tile X indeksi.</summary>
        public static int LonToTileX(double lon, int zoom)
        {
            int n = MapSizeTiles(zoom);
            int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);
            return Clamp(x, 0, n - 1);
        }

        /// <summary>lon/lat (gradus) -> tile Y indeksi.</summary>
        public static int LatToTileY(double lat, int zoom)
        {
            int n = MapSizeTiles(zoom);
            double latRad = lat * DegToRad;
            // ln(tan(lat) + sec(lat)) = ln(tan(lat) + 1/cos(lat)) = atanh(sin(lat)) ko'rinishi.
            double y = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n;
            return Clamp((int)Math.Floor(y), 0, n - 1);
        }

        /// <summary>Tile X chap chetining longitude qiymati (gradus).</summary>
        public static double TileXToLon(int x, int zoom)
        {
            int n = MapSizeTiles(zoom);
            return (double)x / n * 360.0 - 180.0;
        }

        /// <summary>Tile Y yuqori chetining latitude qiymati (gradus).</summary>
        public static double TileYToLat(int y, int zoom)
        {
            int n = MapSizeTiles(zoom);
            double m = Math.PI * (1.0 - 2.0 * y / n);
            return 180.0 / Math.PI * Math.Atan(Math.Sinh(m));
        }

        /// <summary>Tile ning geografik chegaralarini (lon/lat) qaytaradi.</summary>
        public static TileBounds GetTileGeoBounds(TileInfo tile)
        {
            return new TileBounds
            {
                WestLon = TileXToLon(tile.X, tile.Z),
                EastLon = TileXToLon(tile.X + 1, tile.Z),
                // Y indeksi pastga qarab o'sadi: y -> shimoliy chet, y+1 -> janubiy chet.
                NorthLat = TileYToLat(tile.Y, tile.Z),
                SouthLat = TileYToLat(tile.Y + 1, tile.Z)
            };
        }

        /// <summary>
        /// Berilgan geografik to'rtburchak ichida (yoki unga tegib turgan)
        /// barcha tilelar ro'yxatini qaytaradi.
        /// </summary>
        public static IEnumerable<TileInfo> GetTilesForBounds(
            double minLon, double minLat, double maxLon, double maxLat, int zoom)
        {
            zoom = Clamp(zoom, MinZoom, MaxZoom);

            int xMin = LonToTileX(minLon, zoom);
            int xMax = LonToTileX(maxLon, zoom);
            // Latitude oshgani sari tile Y kamayadi — shu sababli min/max almashadi.
            int yMin = LatToTileY(maxLat, zoom);
            int yMax = LatToTileY(minLat, zoom);

            if (xMin > xMax) Swap(ref xMin, ref xMax);
            if (yMin > yMax) Swap(ref yMin, ref yMax);

            var result = new List<TileInfo>();
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    result.Add(new TileInfo(x, y, zoom));
                }
            }
            return result;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static void Swap(ref int a, ref int b)
        {
            int t = a; a = b; b = t;
        }
    }
}
