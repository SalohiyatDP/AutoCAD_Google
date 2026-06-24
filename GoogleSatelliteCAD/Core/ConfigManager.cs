using System;
using System.IO;
using System.Xml.Serialization;
using GoogleSatelliteCAD.Projection;

namespace GoogleSatelliteCAD.Core
{
    /// <summary>
    /// Foydalanuvchi sozlamalarini saqlovchi ma'lumot modeli (POCO).
    /// Ushbu obyekt XML ko'rinishida diskka saqlanadi va o'qiladi.
    /// </summary>
    [Serializable]
    [XmlRoot("GoogleSatelliteSettings")]
    public class PluginSettings
    {
        /// <summary>
        /// Tile (xarita bo'lakchasi) serverining URL shabloni.
        /// {x}, {y}, {z} o'rinbosarlari mos ravishda tile X, Y va zoom bilan almashtiriladi.
        /// Standart: Google Satellite tile serveri.
        /// </summary>
        public string TileServerUrl { get; set; } =
            "https://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}";

        /// <summary>Disk keshining maksimal hajmi (megabaytlarda). 0 — cheksiz.</summary>
        public int MaxCacheSizeMb { get; set; } = 2048;

        /// <summary>Ruxsat etilgan eng yuqori zoom darajasi (0..22).</summary>
        public int MaxZoom { get; set; } = 21;

        /// <summary>Pan/Zoom bo'lganda xarita avtomatik yangilanadimi.</summary>
        public bool AutoRefresh { get; set; } = true;

        /// <summary>Chizma (DWG) ishlatadigan koordinata tizimi (odatiy: Pulkovo GK Zona 12N).</summary>
        public CoordinateSystemType CoordinateSystem { get; set; } =
            CoordinateSystemType.Pulkovo1942_GK_Zone12N;

        /// <summary>Parallel ravishda yuklanadigan tilelar soni (yuklab olish oqimlari).</summary>
        public int MaxParallelDownloads { get; set; } = 8;

        /// <summary>HTTP so'rovlarda foydalaniladigan User-Agent sarlavhasi.</summary>
        public string UserAgent { get; set; } =
            "GoogleSatelliteCAD/1.0 (AutoCAD Mechanical 2021 plugin)";
    }

    /// <summary>
    /// <see cref="PluginSettings"/> ni diskdan o'qish va diskka yozishni boshqaradi.
    /// Sozlamalar %APPDATA%\GoogleSatelliteCAD\settings.xml faylida saqlanadi.
    ///
    /// SOLID: bu klass faqat sozlamalarni saqlash/yuklash bilan shug'ullanadi.
    /// </summary>
    public sealed class ConfigManager
    {
        private static readonly Lazy<ConfigManager> _instance =
            new Lazy<ConfigManager>(() => new ConfigManager());

        /// <summary>Yagona (singleton) namuna.</summary>
        public static ConfigManager Instance => _instance.Value;

        private readonly string _settingsFile;
        private readonly XmlSerializer _serializer = new XmlSerializer(typeof(PluginSettings));

        /// <summary>Joriy yuklangan sozlamalar.</summary>
        public PluginSettings Settings { get; private set; }

        private ConfigManager()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GoogleSatelliteCAD");
            Directory.CreateDirectory(dir);
            _settingsFile = Path.Combine(dir, "settings.xml");

            Settings = Load();
        }

        /// <summary>
        /// Sozlamalarni diskdan o'qiydi. Fayl mavjud bo'lmasa yoki buzilgan bo'lsa,
        /// standart sozlamalar qaytariladi.
        /// </summary>
        public PluginSettings Load()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    using (var fs = File.OpenRead(_settingsFile))
                    {
                        var loaded = (PluginSettings)_serializer.Deserialize(fs);
                        // Qo'llab-quvvatlanadigan tizimlar: Web Mercator (EPSG:3857),
                        // Pulkovo GK Zona 12N (EPSG:28412) va Pulkovo GK Zona 12N (EPSG:28462).
                        // Boshqasi saqlangan bo'lsa, Pulkovo (28412) ga moslaymiz.
                        if (loaded.CoordinateSystem != CoordinateSystemType.WebMercator_3857
                            && loaded.CoordinateSystem != CoordinateSystemType.Pulkovo1942_GK_Zone12N
                            && loaded.CoordinateSystem != CoordinateSystemType.Pulkovo1942_GK_Zone12N_28462)
                        {
                            loaded.CoordinateSystem = CoordinateSystemType.Pulkovo1942_GK_Zone12N;
                        }
                        Settings = loaded;
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Sozlamalarni o'qishda xatolik, standart qiymatlar ishlatiladi: " + ex.Message);
            }

            Settings = new PluginSettings();
            return Settings;
        }

        /// <summary>Joriy sozlamalarni diskka yozadi.</summary>
        public void Save()
        {
            try
            {
                using (var fs = File.Create(_settingsFile))
                {
                    _serializer.Serialize(fs, Settings);
                }
                Logger.Info("Sozlamalar saqlandi.");
            }
            catch (Exception ex)
            {
                Logger.Error("Sozlamalarni saqlashda xatolik.", ex);
            }
        }

        /// <summary>Berilgan sozlamalarni o'rnatadi va saqlaydi.</summary>
        public void Update(PluginSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Save();
        }
    }
}
