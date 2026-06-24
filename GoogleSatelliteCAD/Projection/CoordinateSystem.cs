namespace GoogleSatelliteCAD.Projection
{
    /// <summary>
    /// Plagin qo'llab-quvvatlaydigan koordinata tizimlari.
    /// Chizma (DWG) shu tizimlardan birida bo'lishi mumkin; plagin esa
    /// uni WGS84 geografik (lon/lat) ga o'tkazib, tilelarni hisoblaydi.
    /// </summary>
    public enum CoordinateSystemType
    {
        /// <summary>WGS84 geografik koordinatalar (gradusda: longitude, latitude).</summary>
        WGS84_Geographic = 0,

        /// <summary>WGS84 UTM (avtomatik zona, longitude bo'yicha aniqlanadi).</summary>
        WGS84_UTM = 1,

        /// <summary>Pulkovo 1942 (SK-42) Gauss-Kruger zona 12N — standart holat.</summary>
        Pulkovo1942_GK_Zone12N = 2,

        /// <summary>EPSG:3857 Web Mercator (Google/OSM proyeksiyasi).</summary>
        WebMercator_3857 = 3,

        /// <summary>
        /// Pulkovo 1942 / Gauss-Kruger zona 12N, prefikssiz soxta sharqiy (false easting 500000;
        /// EPSG:28462). Easting ~5xx,xxx ko'rinishida — ko'pincha GPS/geodezik qurilma eksporti.
        /// </summary>
        Pulkovo1942_GK_Zone12N_28462 = 4
    }

    /// <summary>
    /// Geografik nuqta: longitude (uzunlik) va latitude (kenglik), gradusda.
    /// </summary>
    public struct GeoPoint
    {
        public double Lon;
        public double Lat;

        public GeoPoint(double lon, double lat)
        {
            Lon = lon;
            Lat = lat;
        }

        public override string ToString() => $"Lon={Lon:F8}, Lat={Lat:F8}";
    }

    /// <summary>
    /// Chizma (drawing/CRS) koordinatalaridagi nuqta: X (sharqqa), Y (shimolga).
    /// AutoCAD'ning Point3d ga bog'liq bo'lmaslik uchun alohida sodda struktura.
    /// </summary>
    public struct DrawingPoint
    {
        public double X;
        public double Y;

        public DrawingPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"X={X:F4}, Y={Y:F4}";
    }
}
