namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// Tile disk keshi abstraksiyasi.
    /// Yuklab olingan tilelar diskda saqlanib, qayta yuklanmasligini ta'minlaydi.
    ///
    /// SOLID: kesh mexanizmi interfeys orqali ajratilgan — kelajakda
    /// xotira keshi yoki boshqa saqlash usuli oson qo'shilishi mumkin.
    /// </summary>
    public interface ITileCache
    {
        /// <summary>Tile keshda mavjudligini tekshiradi.</summary>
        bool Exists(TileInfo tile);

        /// <summary>
        /// Tile keshda bo'lsa, uning to'liq fayl yo'lini qaytaradi.
        /// Mavjud bo'lmasa false qaytaradi.
        /// </summary>
        bool TryGetPath(TileInfo tile, out string path);

        /// <summary>Tile uchun (mavjud bo'lmasa ham) kutilayotgan fayl yo'li.</summary>
        string GetPath(TileInfo tile);

        /// <summary>Tile baytlarini keshga yozadi va fayl yo'lini qaytaradi.</summary>
        string Save(TileInfo tile, byte[] data);

        /// <summary>Butun keshni o'chiradi (GSATCLEAR).</summary>
        void Clear();

        /// <summary>Kesh hajmi (bayt).</summary>
        long GetSizeBytes();

        /// <summary>Kesh hajmini berilgan chegaragacha kichraytiradi (eng eski fayllarni o'chiradi).</summary>
        void EnforceSizeLimit(long maxBytes);
    }
}
