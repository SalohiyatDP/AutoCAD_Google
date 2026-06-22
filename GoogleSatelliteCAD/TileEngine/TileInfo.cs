using System;
using Autodesk.AutoCAD.Geometry;

namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// Bitta tile (xarita bo'lakchasi) ni aniqlovchi (X, Y, Z) koordinatalari.
    /// Slippy-map (Google/OSM) sxemasiga mos: Z — zoom, X — gorizontal, Y — vertikal indeks.
    /// O'zgarmas (immutable) qiymat obyekti.
    /// </summary>
    public struct TileInfo : IEquatable<TileInfo>
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public TileInfo(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Keshda va lug'atlarda ishlatish uchun yagona kalit.</summary>
        public string Key => $"{Z}/{X}/{Y}";

        public bool Equals(TileInfo other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object obj) => obj is TileInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                hash = hash * 31 + Z;
                return hash;
            }
        }

        public override string ToString() => Key;
    }

    /// <summary>
    /// Tile ning geografik chegaralari (WGS84 lon/lat, gradusda).
    /// </summary>
    public struct TileBounds
    {
        public double WestLon;
        public double EastLon;
        public double SouthLat;
        public double NorthLat;
    }

    /// <summary>
    /// Yuklab olingan (yoki keshdan topilgan) tile natijasi.
    /// <see cref="FilePath"/> null bo'lsa, tile mavjud emas/yuklab bo'lmadi.
    /// </summary>
    public sealed class DownloadedTile
    {
        public TileInfo Tile { get; }
        public string FilePath { get; }

        public DownloadedTile(TileInfo tile, string filePath)
        {
            Tile = tile;
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Tile ni chizmaga raster sifatida joylashtirish uchun zarur geometriya.
    /// Affin joylashtirish: Origin (pastki-chap burchak), UVector (gorizontal,
    /// rasm kengligi), VVector (vertikal, rasm balandligi).
    /// </summary>
    public sealed class TilePlacement
    {
        public TileInfo Tile { get; set; }
        public string FilePath { get; set; }
        public Point3d Origin { get; set; }
        public Vector3d UVector { get; set; }
        public Vector3d VVector { get; set; }

        /// <summary>RasterImage entity nomi/kaliti uchun ishlatiladi.</summary>
        public string Key => Tile.Key;
    }
}
