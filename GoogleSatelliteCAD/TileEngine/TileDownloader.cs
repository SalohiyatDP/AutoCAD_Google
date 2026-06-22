using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// HTTP orqali tile serveridan rasm yuklab oluvchi.
    /// URL shabloni {x}, {y}, {z} o'rinbosarlarini tile qiymatlari bilan almashtiradi.
    ///
    /// HttpClient bir marta yaratilib qayta ishlatiladi (soket tugashining oldini olish uchun).
    /// </summary>
    public sealed class TileDownloader : ITileDownloader
    {
        private static readonly HttpClient Http = CreateClient();

        private readonly string _urlTemplate;
        private readonly string _userAgent;

        public TileDownloader(string urlTemplate, string userAgent)
        {
            _urlTemplate = urlTemplate ?? throw new ArgumentNullException(nameof(urlTemplate));
            _userAgent = userAgent;
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
            // TLS 1.2 ni majburiy yoqamiz (eski .NET standartlari uchun).
            try
            {
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
                System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            }
            catch { /* ignore */ }
            return client;
        }

        /// <summary>URL shablonini tile qiymatlari bilan to'ldiradi.</summary>
        public string BuildUrl(TileInfo tile)
        {
            return _urlTemplate
                .Replace("{x}", tile.X.ToString())
                .Replace("{y}", tile.Y.ToString())
                .Replace("{z}", tile.Z.ToString());
        }

        public async Task<byte[]> DownloadAsync(TileInfo tile, CancellationToken token)
        {
            string url = BuildUrl(tile);
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (!string.IsNullOrEmpty(_userAgent))
                    {
                        request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
                    }

                    using (HttpResponseMessage resp = await Http
                        .SendAsync(request, HttpCompletionOption.ResponseContentRead, token)
                        .ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            Logger.Warn($"Tile yuklanmadi ({(int)resp.StatusCode}): {tile.Key}");
                            return null;
                        }

                        byte[] data = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        return (data != null && data.Length > 0) ? data : null;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return null; // bekor qilindi — normal
            }
            catch (Exception ex)
            {
                Logger.Warn($"Tile yuklashda xatolik {tile.Key}: {ex.Message}");
                return null;
            }
        }
    }
}
