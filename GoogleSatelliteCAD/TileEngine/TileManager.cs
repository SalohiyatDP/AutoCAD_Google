using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// Tile yuklash jarayonining yuqori darajadagi koordinatori.
    /// Keshni ({ <see cref="ITileCache"/> }) va yuklab oluvchini
    /// ({ <see cref="ITileDownloader"/> }) birlashtiradi:
    ///   1. Avval keshdan qidiradi.
    ///   2. Yetishmayotgan tilelarni parallel (asinxron) yuklaydi.
    ///   3. Yuklanganlarni keshga yozadi.
    ///
    /// Performance: SemaphoreSlim orqali bir vaqtda yuklanadigan tilelar soni
    /// cheklanadi (parallel download), bu 500+ tile holatida ham barqaror ishlaydi.
    /// </summary>
    public sealed class TileManager
    {
        private readonly ITileCache _cache;
        private readonly ITileDownloader _downloader;
        private readonly int _maxParallel;
        private readonly long _maxCacheBytes;

        public TileManager(PluginSettings settings)
            : this(settings, new TileCache(),
                   new TileDownloader(settings.TileServerUrl, settings.UserAgent))
        {
        }

        /// <summary>Bog'liqliklarni tashqaridan berish uchun (Dependency Injection, test).</summary>
        public TileManager(PluginSettings settings, ITileCache cache, ITileDownloader downloader)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _maxParallel = Math.Max(1, settings.MaxParallelDownloads);
            _maxCacheBytes = (long)Math.Max(0, settings.MaxCacheSizeMb) * 1024L * 1024L;
        }

        /// <summary>Keshga to'g'ridan-to'g'ri kirish (GSATCLEAR uchun).</summary>
        public ITileCache Cache => _cache;

        /// <summary>
        /// Berilgan tilelarning hammasini ta'minlaydi: keshdagilarni qoldiradi,
        /// yetishmayotganlarni yuklaydi. Natijada har bir tile uchun fayl yo'li
        /// (yoki null) qaytariladi.
        /// </summary>
        public async Task<IReadOnlyList<DownloadedTile>> EnsureTilesAsync(
            IEnumerable<TileInfo> tiles, CancellationToken token)
        {
            var distinct = tiles.Distinct().ToList();
            var results = new ConcurrentBag<DownloadedTile>();

            using (var gate = new SemaphoreSlim(_maxParallel))
            {
                var tasks = new List<Task>(distinct.Count);

                foreach (TileInfo tile in distinct)
                {
                    // 1. Kesh urilishi (cache hit) — yuklab o'tirmaymiz.
                    if (_cache.TryGetPath(tile, out string cachedPath))
                    {
                        results.Add(new DownloadedTile(tile, cachedPath));
                        continue;
                    }

                    // 2. Kesh promashi (cache miss) — parallel yuklaymiz.
                    tasks.Add(DownloadOneAsync(tile, gate, results, token));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // Kesh hajmini cheklov ostida saqlaymiz (fon ishi).
            if (_maxCacheBytes > 0)
            {
                _ = Task.Run(() => _cache.EnforceSizeLimit(_maxCacheBytes));
            }

            return results.ToList();
        }

        private async Task DownloadOneAsync(TileInfo tile, SemaphoreSlim gate,
            ConcurrentBag<DownloadedTile> results, CancellationToken token)
        {
            await gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (token.IsCancellationRequested) return;

                byte[] data = await _downloader.DownloadAsync(tile, token).ConfigureAwait(false);
                if (data != null)
                {
                    string path = _cache.Save(tile, data);
                    if (path != null)
                    {
                        results.Add(new DownloadedTile(tile, path));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal
            }
            catch (Exception ex)
            {
                Logger.Warn($"Tile ta'minlashda xatolik {tile.Key}: {ex.Message}");
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
