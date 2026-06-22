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
                    return new CoordinateTransform(new UtmProjection(0)); // avto zona

                case CoordinateSystemType.WebMercator_3857:
                    return new CoordinateTransform(new WebMercatorProjection());

                case CoordinateSystemType.Pulkovo1942_GK_Zone12N:
                default:
                    return new CoordinateTransform(new Pulkovo1942Projection(12));
            }
        }

        /// <summary>Chizma koordinatasi (X, Y) -> WGS84 lon/lat.</summary>
        public GeoPoint DrawingToGeographic(double x, double y) => _projection.ToGeographic(x, y);

        /// <summary>WGS84 lon/lat -> chizma koordinatasi (X, Y).</summary>
        public DrawingPoint GeographicToDrawing(double lon, double lat) => _projection.FromGeographic(lon, lat);
    }
}
