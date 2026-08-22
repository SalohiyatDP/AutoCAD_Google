namespace GoogleSatelliteCAD.Projection
{
    /// <summary>
    /// Koordinata almashtirgich fasadi (facade).
    /// Chizma (CRS) koordinatalari bilan WGS84 geografik (lon/lat) orasidagi
    /// o'tkazishni soddalashtirilgan interfeys orqali taqdim etadi.
    ///
    /// SOLID: yuqori darajadagi kod (PluginContext) aniq proyeksiya
    /// klasslariga bog'liq emas — faqat <see cref="IProjection"/> abstraksiyasiga
    /// tayanadi (Dependency Inversion). Yangi koordinata tizimi qo'shilsa,
    /// faqat <see cref="Create"/> kengaytiriladi.
    /// </summary>
    public sealed class CoordinateTransform
    {
        private readonly IProjection _projection;

        /// <summary>Joriy proyeksiyaning o'qiy oladigan nomi.</summary>
        public string Name => _projection.Name;

        private CoordinateTransform(IProjection projection)
        {
            _projection = projection;
        }

        /// <summary>
        /// Tanlangan koordinata tizimi uchun mos almashtirgich yaratadi (Factory).
        /// </summary>
        public static CoordinateTransform Create(CoordinateSystemType type)
        {
            switch (type)
            {
                case CoordinateSystemType.WGS84_Geographic:
                    return new CoordinateTransform(new GeographicProjection());

                case CoordinateSystemType.WGS84_UTM:
                    return new CoordinateTransform(new UtmProjection(0)); // avto zona (ichki)

                // ---- WGS 84 / UTM zonalari (O'zbekiston 40N..43N), easting ~5xx,xxx ----
                case CoordinateSystemType.WGS84_UTM_Zone40N:
                    return new CoordinateTransform(new UtmProjection(40));

                case CoordinateSystemType.WGS84_UTM_Zone41N:
                    return new CoordinateTransform(new UtmProjection(41));

                case CoordinateSystemType.WGS84_UTM_Zone42N:
                    return new CoordinateTransform(new UtmProjection(42));

                case CoordinateSystemType.WGS84_UTM_Zone43N:
                    return new CoordinateTransform(new UtmProjection(43));

                // ---- WGS 84 / UTM PREFIKSLI easting (false easting = zona·1e6 + 500000) ----
                case CoordinateSystemType.WGS84_UTM_ZoneAuto:
                    return new CoordinateTransform(new UtmProjection(0)); // 0 = avto (prefiksli)

                case CoordinateSystemType.WGS84_UTM_Zone40N_Zoned:
                    return new CoordinateTransform(new UtmProjection(40, zonedEasting: true));

                case CoordinateSystemType.WGS84_UTM_Zone41N_Zoned:
                    return new CoordinateTransform(new UtmProjection(41, zonedEasting: true));

                case CoordinateSystemType.WGS84_UTM_Zone42N_Zoned:
                    return new CoordinateTransform(new UtmProjection(42, zonedEasting: true));

                case CoordinateSystemType.WGS84_UTM_Zone43N_Zoned:
                    return new CoordinateTransform(new UtmProjection(43, zonedEasting: true));

                case CoordinateSystemType.WebMercator_3857:
                    return new CoordinateTransform(new WebMercatorProjection());

                // ---- Prefiksli (zonalangan) easting, EPSG:284xx ----
                case CoordinateSystemType.Pulkovo1942_GK_Zone10N:
                    return new CoordinateTransform(new Pulkovo1942Projection(10));

                case CoordinateSystemType.Pulkovo1942_GK_Zone11N:
                    return new CoordinateTransform(new Pulkovo1942Projection(11));

                case CoordinateSystemType.Pulkovo1942_GK_Zone13N:
                    return new CoordinateTransform(new Pulkovo1942Projection(13));

                // ---- Prefikssiz easting (false easting 500000), EPSG:2846x ----
                case CoordinateSystemType.Pulkovo1942_GK_Zone10N_28460:
                    return new CoordinateTransform(new Pulkovo1942Projection(10, zonedEasting: false));

                case CoordinateSystemType.Pulkovo1942_GK_Zone11N_28461:
                    return new CoordinateTransform(new Pulkovo1942Projection(11, zonedEasting: false));

                case CoordinateSystemType.Pulkovo1942_GK_Zone12N_28462:
                    // Prefikssiz easting (false easting 500000, EPSG:28462).
                    return new CoordinateTransform(new Pulkovo1942Projection(12, zonedEasting: false));

                case CoordinateSystemType.Pulkovo1942_GK_Zone13N_28463:
                    return new CoordinateTransform(new Pulkovo1942Projection(13, zonedEasting: false));

                // ---- Avto-zona (prefiksli easting'dan aniqlanadi, O'zbekiston 10..13) ----
                case CoordinateSystemType.Pulkovo1942_GK_ZoneAuto:
                    return new CoordinateTransform(new Pulkovo1942Projection(0)); // 0 = avto

                case CoordinateSystemType.Pulkovo1942_GK_Zone12N:
                    return new CoordinateTransform(new Pulkovo1942Projection(12));

                default:
                    // Noma'lum tanlov — butun O'zbekiston uchun avto-zona (eng xavfsiz standart).
                    return new CoordinateTransform(new Pulkovo1942Projection(0));
            }
        }

        /// <summary>Chizma koordinatasi (X, Y) -> WGS84 lon/lat.</summary>
        public GeoPoint DrawingToGeographic(double x, double y) => _projection.ToGeographic(x, y);

        /// <summary>WGS84 lon/lat -> chizma koordinatasi (X, Y).</summary>
        public DrawingPoint GeographicToDrawing(double lon, double lat) => _projection.FromGeographic(lon, lat);
    }
}
