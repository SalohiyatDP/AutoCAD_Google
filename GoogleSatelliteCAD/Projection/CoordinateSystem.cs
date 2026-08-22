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
        Pulkovo1942_GK_Zone12N_28462 = 4,

        /// <summary>
        /// Pulkovo 1942 / Gauss-Kruger — AVTO-ZONA (O'zbekiston uchun 10N..13N).
        /// Zona chizma koordinatasidagi prefiksli easting qiymatidan avtomatik aniqlanadi
        /// (masalan easting ~11,5xx,xxx -> zona 11, ~12,5xx,xxx -> zona 12).
        /// Butun O'zbekiston hududi uchun eng qulay standart tanlov.
        /// DIQQAT: faqat prefiksli (zonali) easting uchun ishlaydi; prefikssiz (500000)
        /// easting'da zonani aniqlab bo'lmaydi — u holda aniq zona variantini tanlang.
        /// </summary>
        Pulkovo1942_GK_ZoneAuto = 5,

        /// <summary>Pulkovo 1942 / GK zona 10N, prefiksli easting (EPSG:28410, easting ~10,5xx,xxx).</summary>
        Pulkovo1942_GK_Zone10N = 6,

        /// <summary>Pulkovo 1942 / GK zona 11N, prefiksli easting (EPSG:28411, easting ~11,5xx,xxx).</summary>
        Pulkovo1942_GK_Zone11N = 7,

        /// <summary>Pulkovo 1942 / GK zona 13N, prefiksli easting (EPSG:28413, easting ~13,5xx,xxx).</summary>
        Pulkovo1942_GK_Zone13N = 8,

        /// <summary>Pulkovo 1942 / GK zona 10N, prefikssiz easting (EPSG:28460, easting ~5xx,xxx).</summary>
        Pulkovo1942_GK_Zone10N_28460 = 9,

        /// <summary>Pulkovo 1942 / GK zona 11N, prefikssiz easting (EPSG:28461, easting ~5xx,xxx).</summary>
        Pulkovo1942_GK_Zone11N_28461 = 10,

        /// <summary>Pulkovo 1942 / GK zona 13N, prefikssiz easting (EPSG:28463, easting ~5xx,xxx).</summary>
        Pulkovo1942_GK_Zone13N_28463 = 11,

        /// <summary>WGS 84 / UTM zona 40N (EPSG:32640, markaziy meridian 57°E, easting ~5xx,xxx).</summary>
        WGS84_UTM_Zone40N = 12,

        /// <summary>WGS 84 / UTM zona 41N (EPSG:32641, markaziy meridian 63°E, easting ~5xx,xxx).</summary>
        WGS84_UTM_Zone41N = 13,

        /// <summary>WGS 84 / UTM zona 42N (EPSG:32642, markaziy meridian 69°E, easting ~5xx,xxx).</summary>
        WGS84_UTM_Zone42N = 14,

        /// <summary>WGS 84 / UTM zona 43N (EPSG:32643, markaziy meridian 75°E, easting ~5xx,xxx).</summary>
        WGS84_UTM_Zone43N = 15
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
