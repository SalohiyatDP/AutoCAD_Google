using System;
using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;

namespace GoogleSatelliteCAD.Core
{
    /// <summary>
    /// Plagin ishga tushganda mezbon (host) Autodesk mahsulotini aniqlaydi:
    /// versiya, reliz yili, mahsulot nomi va .NET runtime oilasi.
    ///
    /// Maqsad: plagin BARCHA AutoCAD-asosidagi mahsulotlar (standart AutoCAD va
    /// vertikallar: Mechanical, Civil 3D, Map 3D, Architecture, MEP, Electrical,
    /// Plant 3D, Raster Design) uchun mo'ljallangan. Bu klass mahsulotlar API va
    /// .NET runtime bo'yicha BIR XIL emas degan tamoyilga amal qiladi — versiyani
    /// aniqlab jurnalga yozadi va mos kelmaganda ogohlantiradi.
    ///
    /// MUHIM: haqiqiy runtime yo'naltirish (2021-2024 = .NET Framework DLL,
    /// 2025-2026 = .NET 8 DLL) bundle darajasida PackageContents.xml orqali amalga
    /// oshiriladi. Bu klass — diagnostika va himoya qatlami.
    /// </summary>
    public static class HostProduct
    {
        /// <summary>Mezbon mahsulot nomi (masalan "AutoCAD" yoki vertikal nomi). Best-effort.</summary>
        public static string ProductName { get; private set; } = "Autodesk (AutoCAD-asosidagi)";

        /// <summary>Mezbon dastur versiyasi (masalan 24.0 = 2021, 25.0 = 2025).</summary>
        public static Version Version { get; private set; }

        /// <summary>Reliz yili (masalan 2021). Aniqlanmasa 0.</summary>
        public static int ReleaseYear { get; private set; }

        /// <summary>Ushbu DLL yig'ilgan .NET runtime oilasi (kompilyatsiya vaqtida aniqlanadi).</summary>
        public static string RuntimeFamily =>
#if NET8_0_OR_GREATER
            ".NET 8+ (AutoCAD 2025-2026)";
#else
            ".NET Framework 4.8 (AutoCAD 2021-2024)";
#endif

        /// <summary>Bu plagin qo'llab-quvvatlaydigan eng past reliz (AutoCAD 2021 = R24.0).</summary>
        public const int MinSupportedYear = 2021;

        /// <summary>
        /// Mezbon mahsulotni aniqlab, jurnalga yozadi. Plagin yuklanganda bir marta chaqiriladi.
        /// Hech qanday istisno tashqariga chiqmaydi (diagnostika ishga tushishni buzmasligi kerak).
        /// </summary>
        public static void DetectAndLog()
        {
            try
            {
                try { Version = Application.Version; }
                catch { /* ba'zi kontekstlarda mavjud bo'lmasligi mumkin */ }

                DetectProductName();
                ReleaseYear = MapReleaseYear(Version);

                string yil = ReleaseYear > 0 ? ReleaseYear.ToString() : "noma'lum";
                string ver = Version != null ? Version.ToString() : "noma'lum";
                Logger.Info($"Mezbon mahsulot: {ProductName}; versiya: {ver} (reliz: {yil}); runtime (DLL): {RuntimeFamily}.");

                if (Version != null && Version.Major > 0 && ReleaseYear > 0 && ReleaseYear < MinSupportedYear)
                {
                    Logger.Warn($"DIQQAT: bu plagin AutoCAD {MinSupportedYear} (R24.0) va undan yuqori relizlar uchun " +
                                $"mo'ljallangan. Joriy mezbon ({yil}) eskiroq — plagin to'g'ri ishlamasligi mumkin.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Mezbon mahsulotni aniqlashda xatolik: " + ex.Message);
            }
        }

        /// <summary>
        /// Mahsulot nomini ishga tushgan jarayonning (acad.exe) fayl versiya
        /// ma'lumotidan olishga harakat qiladi. Vertikallar odatda acad.exe ustida
        /// ishlaydi, shuning uchun bu ko'pincha "AutoCAD" qaytaradi — bu normal.
        /// </summary>
        private static void DetectProductName()
        {
            try
            {
                ProcessModule main = Process.GetCurrentProcess().MainModule;
                if (main == null) return;

                FileVersionInfo fvi = main.FileVersionInfo;
                if (fvi == null) return;

                // FileDescription odatda batafsilroq (masalan mahsulot bannerini o'z ichiga oladi).
                string desc = (fvi.FileDescription ?? "").Trim();
                string prod = (fvi.ProductName ?? "").Trim();

                if (!string.IsNullOrEmpty(prod)) ProductName = prod;
                else if (!string.IsNullOrEmpty(desc)) ProductName = desc;
            }
            catch { /* best-effort; jim qolamiz */ }
        }

        /// <summary>
        /// AutoCAD ichki versiyasini reliz yiliga o'giradi.
        ///   R24.0 = 2021, 24.1 = 2022, 24.2 = 2023, 24.3 = 2024,
        ///   R25.0 = 2025, 25.1 = 2026.
        /// </summary>
        private static int MapReleaseYear(Version v)
        {
            if (v == null) return 0;
            switch (v.Major)
            {
                case 24: return 2021 + v.Minor; // 24.0..24.3 -> 2021..2024
                case 25: return 2025 + v.Minor; // 25.0..25.1 -> 2025..2026
                default: return 0;               // qo'llab-quvvatlanmaydigan/noma'lum
            }
        }
    }
}
