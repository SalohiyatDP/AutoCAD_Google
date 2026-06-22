namespace GoogleSatelliteCAD.Projection
{
    /// <summary>
    /// Proyeksiya abstraksiyasi: chizma koordinatalari (CRS) bilan
    /// WGS84 geografik koordinatalar (lon/lat) orasidagi o'tkazishni ta'minlaydi.
    ///
    /// SOLID: har bir koordinata tizimi shu interfeysni amalga oshiradi
    /// (Open/Closed + Liskov). Yangi tizim qo'shish uchun mavjud kodni
    /// o'zgartirish shart emas — yangi <see cref="IProjection"/> qo'shiladi.
    /// </summary>
    public interface IProjection
    {
        /// <summary>Ushbu proyeksiyaning inson o'qiy oladigan nomi.</summary>
        string Name { get; }

        /// <summary>
        /// Chizma koordinatasini (X sharqqa, Y shimolga) WGS84 lon/lat ga o'tkazadi.
        /// </summary>
        GeoPoint ToGeographic(double x, double y);

        /// <summary>
        /// WGS84 lon/lat ni chizma koordinatasiga (X, Y) o'tkazadi.
        /// </summary>
        DrawingPoint FromGeographic(double lon, double lat);
    }
}
