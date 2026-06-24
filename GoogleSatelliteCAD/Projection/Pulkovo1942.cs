using System;

namespace GoogleSatelliteCAD.Projection
{
    /// <summary>
    /// Ellipsoidal Transverse Mercator (ko'ndalang Merkator) proyeksiyasi.
    /// Snyder (USGS) qatorli formulalariga asoslangan; UTM va Gauss-Kruger
    /// proyeksiyalari shu yadrodan foydalanadi.
    ///
    /// SOLID: bu klass faqat TM matematikasi bilan shug'ullanadi va
    /// datum/koordinata tizimi mantig'idan ajratilgan.
    /// </summary>
    public sealed class TransverseMercator
    {
        private readonly Ellipsoid _e;
        private readonly double _lon0;   // markaziy meridian (radian)
        private readonly double _lat0;   // kenglik boshlanishi (radian)
        private readonly double _k0;     // markaziy meridiandagi masshtab
        private readonly double _fe;     // soxta sharqiy (false easting)
        private readonly double _fn;     // soxta shimoliy (false northing)
        private readonly double _m0;

        public TransverseMercator(Ellipsoid e, double lon0Deg, double lat0Deg,
                                  double k0, double falseEasting, double falseNorthing)
        {
            _e = e;
            _lon0 = lon0Deg * Math.PI / 180.0;
            _lat0 = lat0Deg * Math.PI / 180.0;
            _k0 = k0;
            _fe = falseEasting;
            _fn = falseNorthing;
            _m0 = MeridionalArc(_lat0);
        }

        /// <summary>Ellipsoiddagi lat/lon (gradus) -> proyeksiya (easting, northing).</summary>
        public void Forward(double lonDeg, double latDeg, out double easting, out double northing)
        {
            double lat = latDeg * Math.PI / 180.0;
            double lon = lonDeg * Math.PI / 180.0;

            double e2 = _e.E2;
            double ep2 = _e.Ep2;
            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double tanLat = Math.Tan(lat);

            double N = _e.A / Math.Sqrt(1.0 - e2 * sinLat * sinLat);
            double T = tanLat * tanLat;
            double C = ep2 * cosLat * cosLat;
            double A = (lon - _lon0) * cosLat;
            double M = MeridionalArc(lat);

            double A2 = A * A;
            double A3 = A2 * A;
            double A4 = A3 * A;
            double A5 = A4 * A;
            double A6 = A5 * A;

            easting = _fe + _k0 * N * (
                A
                + (1.0 - T + C) * A3 / 6.0
                + (5.0 - 18.0 * T + T * T + 72.0 * C - 58.0 * ep2) * A5 / 120.0);

            northing = _fn + _k0 * (
                (M - _m0)
                + N * tanLat * (
                    A2 / 2.0
                    + (5.0 - T + 9.0 * C + 4.0 * C * C) * A4 / 24.0
                    + (61.0 - 58.0 * T + T * T + 600.0 * C - 330.0 * ep2) * A6 / 720.0));
        }

        /// <summary>Proyeksiya (easting, northing) -> ellipsoiddagi lat/lon (gradus).</summary>
        public void Inverse(double easting, double northing, out double lonDeg, out double latDeg)
        {
            double e2 = _e.E2;
            double ep2 = _e.Ep2;
            double a = _e.A;

            double M = _m0 + (northing - _fn) / _k0;
            double mu = M / (a * (1.0 - e2 / 4.0 - 3.0 * e2 * e2 / 64.0 - 5.0 * e2 * e2 * e2 / 256.0));

            double e1 = (1.0 - Math.Sqrt(1.0 - e2)) / (1.0 + Math.Sqrt(1.0 - e2));
            double e1_2 = e1 * e1;
            double e1_3 = e1_2 * e1;
            double e1_4 = e1_3 * e1;

            double lat1 = mu
                + (3.0 * e1 / 2.0 - 27.0 * e1_3 / 32.0) * Math.Sin(2.0 * mu)
                + (21.0 * e1_2 / 16.0 - 55.0 * e1_4 / 32.0) * Math.Sin(4.0 * mu)
                + (151.0 * e1_3 / 96.0) * Math.Sin(6.0 * mu)
                + (1097.0 * e1_4 / 512.0) * Math.Sin(8.0 * mu);

            double sinLat1 = Math.Sin(lat1);
            double cosLat1 = Math.Cos(lat1);
            double tanLat1 = Math.Tan(lat1);

            double C1 = ep2 * cosLat1 * cosLat1;
            double T1 = tanLat1 * tanLat1;
            double N1 = a / Math.Sqrt(1.0 - e2 * sinLat1 * sinLat1);
            double R1 = a * (1.0 - e2) / Math.Pow(1.0 - e2 * sinLat1 * sinLat1, 1.5);
            double D = (easting - _fe) / (N1 * _k0);

            double D2 = D * D;
            double D3 = D2 * D;
            double D4 = D3 * D;
            double D5 = D4 * D;
            double D6 = D5 * D;

            double lat = lat1 - (N1 * tanLat1 / R1) * (
                D2 / 2.0
                - (5.0 + 3.0 * T1 + 10.0 * C1 - 4.0 * C1 * C1 - 9.0 * ep2) * D4 / 24.0
                + (61.0 + 90.0 * T1 + 298.0 * C1 + 45.0 * T1 * T1 - 252.0 * ep2 - 3.0 * C1 * C1) * D6 / 720.0);

            double lon = _lon0 + (
                D
                - (1.0 + 2.0 * T1 + C1) * D3 / 6.0
                + (5.0 - 2.0 * C1 + 28.0 * T1 - 3.0 * C1 * C1 + 8.0 * ep2 + 24.0 * T1 * T1) * D5 / 120.0) / cosLat1;

            latDeg = lat * 180.0 / Math.PI;
            lonDeg = lon * 180.0 / Math.PI;
        }

        /// <summary>Meridian yoyi uzunligi M(lat) (metr).</summary>
        private double MeridionalArc(double lat)
        {
            double e2 = _e.E2;
            double e4 = e2 * e2;
            double e6 = e4 * e2;
            double a = _e.A;

            return a * (
                (1.0 - e2 / 4.0 - 3.0 * e4 / 64.0 - 5.0 * e6 / 256.0) * lat
                - (3.0 * e2 / 8.0 + 3.0 * e4 / 32.0 + 45.0 * e6 / 1024.0) * Math.Sin(2.0 * lat)
                + (15.0 * e4 / 256.0 + 45.0 * e6 / 1024.0) * Math.Sin(4.0 * lat)
                - (35.0 * e6 / 3072.0) * Math.Sin(6.0 * lat));
        }
    }

    /// <summary>
    /// Datumlararo geodezik almashtirishlar: geografik &lt;-&gt; geosentrik
    /// koordinatalar va 7 parametrli Helmert (Bursa-Wolf) o'tkazish.
    /// </summary>
    public static class GeodeticTransform
    {
        /// <summary>Geografik (lon/lat, gradus, h=0) -> geosentrik (X,Y,Z) metr.</summary>
        public static void GeographicToGeocentric(Ellipsoid e, double lonDeg, double latDeg,
                                                   out double x, out double y, out double z)
        {
            double lon = lonDeg * Math.PI / 180.0;
            double lat = latDeg * Math.PI / 180.0;
            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double N = e.A / Math.Sqrt(1.0 - e.E2 * sinLat * sinLat);
            const double h = 0.0;

            x = (N + h) * cosLat * Math.Cos(lon);
            y = (N + h) * cosLat * Math.Sin(lon);
            z = (N * (1.0 - e.E2) + h) * sinLat;
        }

        /// <summary>Geosentrik (X,Y,Z) metr -> geografik (lon/lat, gradus). Bowring iteratsiyasi.</summary>
        public static void GeocentricToGeographic(Ellipsoid e, double x, double y, double z,
                                                  out double lonDeg, out double latDeg)
        {
            double lon = Math.Atan2(y, x);
            double p = Math.Sqrt(x * x + y * y);

            // Boshlang'ich yaqinlashtirish.
            double lat = Math.Atan2(z, p * (1.0 - e.E2));
            double latPrev;
            int iter = 0;
            do
            {
                latPrev = lat;
                double sinLat = Math.Sin(lat);
                double N = e.A / Math.Sqrt(1.0 - e.E2 * sinLat * sinLat);
                double h = p / Math.Cos(lat) - N;
                lat = Math.Atan2(z, p * (1.0 - e.E2 * N / (N + h)));
            }
            while (Math.Abs(lat - latPrev) > 1e-12 && ++iter < 10);

            lonDeg = lon * 180.0 / Math.PI;
            latDeg = lat * 180.0 / Math.PI;
        }

        /// <summary>Bursa-Wolf (position vector) to'g'ri yo'nalish: manba datum -> nishon datum.</summary>
        public static void HelmertForward(DatumShift d, double x, double y, double z,
                                          out double xo, out double yo, out double zo)
        {
            xo = d.Dx + d.Scale * (x - d.Rz * y + d.Ry * z);
            yo = d.Dy + d.Scale * (d.Rz * x + y - d.Rx * z);
            zo = d.Dz + d.Scale * (-d.Ry * x + d.Rx * y + z);
        }

        /// <summary>Bursa-Wolf teskari yo'nalish: nishon datum -> manba datum.</summary>
        public static void HelmertInverse(DatumShift d, double x, double y, double z,
                                          out double xo, out double yo, out double zo)
        {
            double tx = (x - d.Dx) / d.Scale;
            double ty = (y - d.Dy) / d.Scale;
            double tz = (z - d.Dz) / d.Scale;

            // Kichik aylanishlar uchun transpozitsiya teskari aylanishga teng.
            xo = tx + d.Rz * ty - d.Ry * tz;
            yo = -d.Rz * tx + ty + d.Rx * tz;
            zo = d.Ry * tx - d.Rx * ty + tz;
        }
    }

    /// <summary>
    /// Pulkovo 1942 (SK-42) Gauss-Kruger proyeksiyasi.
    /// Krassovskiy 1940 ellipsoidida hisoblanadi va WGS84 ga
    /// 7 parametrli datum o'tkazish orqali bog'lanadi.
    /// </summary>
    public sealed class Pulkovo1942Projection : IProjection
    {
        private readonly TransverseMercator _tm;
        private readonly DatumShift _datum;
        private readonly int _zone;

        /// <param name="zone">Gauss-Kruger zonasi (masalan, 12).</param>
        public Pulkovo1942Projection(int zone)
        {
            _zone = zone;
            double centralMeridian = 6.0 * zone - 3.0;        // zona markaziy meridiani
            double falseEasting = zone * 1_000_000.0 + 500_000.0; // zona prefiksli soxta sharqiy
            _tm = new TransverseMercator(Ellipsoid.Krassovsky1940,
                                         centralMeridian, 0.0, 1.0, falseEasting, 0.0);
            _datum = DatumShift.Pulkovo1942ToWgs84;
        }

        public string Name => $"Pulkovo 1942 / Gauss-Kruger zona {_zone}N (EPSG:{28400 + _zone})";

        public GeoPoint ToGeographic(double x, double y)
        {
            // 1. GK (Krassovskiy) -> Krassovskiy lat/lon.
            _tm.Inverse(x, y, out double lonK, out double latK);
            // 2. Krassovskiy geografik -> geosentrik.
            GeodeticTransform.GeographicToGeocentric(Ellipsoid.Krassovsky1940, lonK, latK,
                out double gx, out double gy, out double gz);
            // 3. Pulkovo -> WGS84 (Helmert to'g'ri).
            GeodeticTransform.HelmertForward(_datum, gx, gy, gz,
                out double wx, out double wy, out double wz);
            // 4. WGS84 geosentrik -> geografik.
            GeodeticTransform.GeocentricToGeographic(Ellipsoid.WGS84, wx, wy, wz,
                out double lon, out double lat);
            return new GeoPoint(lon, lat);
        }

        public DrawingPoint FromGeographic(double lon, double lat)
        {
            // 1. WGS84 geografik -> geosentrik.
            GeodeticTransform.GeographicToGeocentric(Ellipsoid.WGS84, lon, lat,
                out double wx, out double wy, out double wz);
            // 2. WGS84 -> Pulkovo (Helmert teskari).
            GeodeticTransform.HelmertInverse(_datum, wx, wy, wz,
                out double gx, out double gy, out double gz);
            // 3. Krassovskiy geosentrik -> geografik.
            GeodeticTransform.GeocentricToGeographic(Ellipsoid.Krassovsky1940, gx, gy, gz,
                out double lonK, out double latK);
            // 4. Krassovskiy lat/lon -> GK easting/northing.
            _tm.Forward(lonK, latK, out double e, out double n);
            return new DrawingPoint(e, n);
        }
    }

    /// <summary>
    /// WGS84 UTM proyeksiyasi. Zona avtomatik (longitude bo'yicha) yoki
    /// aniq berilishi mumkin. WGS84 ellipsoidida ishlaganligi uchun
    /// datum o'tkazish talab qilinmaydi.
    /// </summary>
    public sealed class UtmProjection : IProjection
    {
        private TransverseMercator _tm;
        private int _zone;
        private bool _zoneFixed;
        private bool _northern = true;

        /// <param name="zone">UTM zonasi (1..60). 0 bo'lsa, birinchi
        /// FromGeographic chaqiruvida avtomatik aniqlanadi.</param>
        public UtmProjection(int zone = 0)
        {
            if (zone >= 1 && zone <= 60)
            {
                _zone = zone;
                _zoneFixed = true;
                BuildTm();
            }
        }

        public string Name => _zoneFixed ? $"WGS84 / UTM zona {_zone}{(_northern ? "N" : "S")}" : "WGS84 / UTM (avto zona)";

        private void BuildTm()
        {
            double centralMeridian = (_zone - 1) * 6.0 - 180.0 + 3.0;
            double falseNorthing = _northern ? 0.0 : 10_000_000.0;
            _tm = new TransverseMercator(Ellipsoid.WGS84, centralMeridian, 0.0,
                                         0.9996, 500_000.0, falseNorthing);
        }

        public GeoPoint ToGeographic(double x, double y)
        {
            if (_tm == null)
            {
                // Zona hali aniqlanmagan — xavfsiz standart (zona 12).
                _zone = 12;
                BuildTm();
            }
            _tm.Inverse(x, y, out double lon, out double lat);
            return new GeoPoint(lon, lat);
        }

        public DrawingPoint FromGeographic(double lon, double lat)
        {
            if (!_zoneFixed && _tm == null)
            {
                _zone = (int)Math.Floor((lon + 180.0) / 6.0) + 1;
                _northern = lat >= 0.0;
                BuildTm();
            }
            _tm.Forward(lon, lat, out double e, out double n);
            return new DrawingPoint(e, n);
        }
    }
}
