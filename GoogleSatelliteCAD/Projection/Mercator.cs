using System;

namespace GoogleSatelliteCAD.Projection
{
    /// <summary>
    /// Web Mercator (EPSG:3857) proyeksiyasi uchun matematik yordamchi.
    /// Sferik Mercator formulalari (Google/OSM tile tizimi shularga asoslangan).
    ///
    /// Eslatma: Web Mercator Yer'ni sfera deb hisoblaydi (radius = WGS84 a).
    /// Tile koordinatalari aynan shu modeldan kelib chiqadi.
    /// </summary>
    public static class Mercator
    {
        /// <summary>Web Mercator'da ishlatiladigan Yer radiusi (metr).</summary>
        public const double EarthRadius = 6378137.0;

        /// <summary>Web Mercator chegarasidagi maksimal kenglik (~85.0511°).</summary>
        public const double MaxLatitude = 85.05112877980659;

        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        /// <summary>Longitude (gradus) -> Web Mercator X (metr).</summary>
        public static double LonToMeters(double lon)
        {
            return lon * DegToRad * EarthRadius;
        }

        /// <summary>Latitude (gradus) -> Web Mercator Y (metr).</summary>
        public static double LatToMeters(double lat)
        {
            lat = Clamp(lat, -MaxLatitude, MaxLatitude);
            double rad = lat * DegToRad;
            return EarthRadius * Math.Log(Math.Tan(Math.PI / 4.0 + rad / 2.0));
        }

        /// <summary>Web Mercator X (metr) -> longitude (gradus).</summary>
        public static double MetersToLon(double x)
        {
            return (x / EarthRadius) * RadToDeg;
        }

        /// <summary>Web Mercator Y (metr) -> latitude (gradus).</summary>
        public static double MetersToLat(double y)
        {
            return (2.0 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI / 2.0) * RadToDeg;
        }

        /// <summary>lon/lat (gradus) -> Web Mercator metr (x, y).</summary>
        public static void LonLatToMeters(double lon, double lat, out double x, out double y)
        {
            x = LonToMeters(lon);
            y = LatToMeters(lat);
        }

        /// <summary>Web Mercator metr (x, y) -> lon/lat (gradus).</summary>
        public static void MetersToLonLat(double x, double y, out double lon, out double lat)
        {
            lon = MetersToLon(x);
            lat = MetersToLat(y);
        }

        private static double Clamp(double v, double min, double max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }

    /// <summary>
    /// EPSG:3857 chizma uchun proyeksiya: chizma koordinatalari to'g'ridan-to'g'ri
    /// Web Mercator metrlari hisoblanadi.
    /// </summary>
    public sealed class WebMercatorProjection : IProjection
    {
        public string Name => "EPSG:3857 Web Mercator";

        public GeoPoint ToGeographic(double x, double y)
        {
            Mercator.MetersToLonLat(x, y, out double lon, out double lat);
            return new GeoPoint(lon, lat);
        }

        public DrawingPoint FromGeographic(double lon, double lat)
        {
            Mercator.LonLatToMeters(lon, lat, out double x, out double y);
            return new DrawingPoint(x, y);
        }
    }

    /// <summary>
    /// WGS84 geografik chizma uchun proyeksiya: chizma koordinatalari
    /// to'g'ridan-to'g'ri gradusdagi longitude (X) va latitude (Y).
    /// </summary>
    public sealed class GeographicProjection : IProjection
    {
        public string Name => "WGS84 Geographic (lon/lat)";

        public GeoPoint ToGeographic(double x, double y) => new GeoPoint(x, y);

        public DrawingPoint FromGeographic(double lon, double lat) => new DrawingPoint(lon, lat);
    }
}
