using System;
using System.IO;
using System.Linq;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.TileEngine
{
    /// <summary>
    /// Tile larni diskda saqlovchi kesh.
    /// Struktura (texnik talabga muvofiq):
    ///   %APPDATA%\GoogleSatelliteCAD\Cache\{z}\{x}\{y}.jpg
    ///
    /// Bir xil tile ikki marta yuklanmaydi — avval keshdan qidiriladi.
    /// </summary>
    public sealed class TileCache : ITileCache
    {
        private readonly string _root;
        private readonly object _ioLock = new object();

        public TileCache()
        {
            _root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GoogleSatelliteCAD", "Cache");
            Directory.CreateDirectory(_root);
        }

        /// <summary>Keshning ildiz papkasi.</summary>
        public string Root => _root;

        public string GetPath(TileInfo tile)
        {
            return Path.Combine(_root,
                tile.Z.ToString(),
                tile.X.ToString(),
                tile.Y.ToString() + ".jpg");
        }

        public bool Exists(TileInfo tile)
        {
            string path = GetPath(tile);
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }

        public bool TryGetPath(TileInfo tile, out string path)
        {
            path = GetPath(tile);
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }

        public string Save(TileInfo tile, byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            string path = GetPath(tile);
            try
            {
                lock (_ioLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    // Atomik yozish: avval vaqtinchalik faylga yozib, so'ng o'rnini almashtiramiz.
                    string tmp = path + ".tmp";
                    File.WriteAllBytes(tmp, data);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
                return path;
            }
            catch (Exception ex)
            {
                Logger.Error($"Tile keshga yozilmadi: {tile.Key}", ex);
                return null;
            }
        }

        public void Clear()
        {
            lock (_ioLock)
            {
                try
                {
                    if (Directory.Exists(_root))
                    {
                        Directory.Delete(_root, recursive: true);
                    }
                    Directory.CreateDirectory(_root);
                    Logger.Info("Tile keshi tozalandi.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Keshni tozalashda xatolik.", ex);
                }
            }
        }

        public long GetSizeBytes()
        {
            try
            {
                if (!Directory.Exists(_root)) return 0;
                return Directory.EnumerateFiles(_root, "*.jpg", SearchOption.AllDirectories)
                                .Sum(f => new FileInfo(f).Length);
            }
            catch
            {
                return 0;
            }
        }

        public void EnforceSizeLimit(long maxBytes)
        {
            if (maxBytes <= 0) return; // 0 — cheksiz

            try
            {
                long total = GetSizeBytes();
                if (total <= maxBytes) return;

                lock (_ioLock)
                {
                    // Eng eski (oxirgi marta kirilgan) fayllarni birinchi bo'lib o'chiramiz.
                    var files = Directory.EnumerateFiles(_root, "*.jpg", SearchOption.AllDirectories)
                                         .Select(f => new FileInfo(f))
                                         .OrderBy(fi => fi.LastAccessTimeUtc)
                                         .ToList();

                    foreach (var fi in files)
                    {
                        if (total <= maxBytes) break;
                        try
                        {
                            long len = fi.Length;
                            fi.Delete();
                            total -= len;
                        }
                        catch { /* fayl band bo'lishi mumkin — o'tkazib yuboramiz */ }
                    }
                    Logger.Info("Kesh hajmi cheklov ostida saqlandi.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Kesh hajmini cheklashda xatolik.", ex);
            }
        }
    }
}
