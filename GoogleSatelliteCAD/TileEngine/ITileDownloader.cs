using System.Threading;
using System.Threading.Tasks;

namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// Tile yuklab oluvchi abstraksiyasi (tile serveridan baytlarni oladi).
    ///
    /// SOLID: tarmoq yuklash mantig'i interfeys orqali ajratilgan, shu sababli
    /// uni soxta (mock) bilan almashtirib test qilish yoki boshqa server
    /// protokoliga o'tish oson.
    /// </summary>
    public interface ITileDownloader
    {
        /// <summary>
        /// Berilgan tile rasm baytlarini asinxron yuklab oladi.
        /// Xatolik yoki bekor qilinishda null qaytaradi.
        /// </summary>
        Task<byte[]> DownloadAsync(TileInfo tile, CancellationToken token);
    }
}
