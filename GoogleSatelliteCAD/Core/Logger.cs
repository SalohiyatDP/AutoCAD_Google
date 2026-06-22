using System;
using System.IO;
using System.Text;

namespace GoogleSatelliteCAD.Core
{
    /// <summary>
    /// Plagin uchun oddiy, thread-safe (oqimlar uchun xavfsiz) log yozuvchi.
    /// Loglar bir vaqtning o'zida AutoCAD buyruq qatoriga (agar mavjud bo'lsa)
    /// va %APPDATA%\GoogleSatelliteCAD\plugin.log fayliga yoziladi.
    ///
    /// SOLID: ushbu klass faqat bitta vazifa — jurnal (log) yozish — bilan
    /// shug'ullanadi (Single Responsibility Principle).
    /// </summary>
    public static class Logger
    {
        // Bir vaqtda bir nechta oqim log yozishining oldini olish uchun qulf.
        private static readonly object SyncRoot = new object();

        // Log fayli yo'li bir marta hisoblanadi.
        private static readonly Lazy<string> LogFilePath = new Lazy<string>(() =>
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GoogleSatelliteCAD");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "plugin.log");
        });

        /// <summary>Faqat fayl jurnaliga yozish kerakmi yoki AutoCAD konsoliga ham.</summary>
        public static bool EchoToEditor { get; set; } = true;

        /// <summary>Ma'lumot darajasidagi xabar.</summary>
        public static void Info(string message) => Write("INFO", message);

        /// <summary>Ogohlantirish darajasidagi xabar.</summary>
        public static void Warn(string message) => Write("WARN", message);

        /// <summary>Xatolik darajasidagi xabar.</summary>
        public static void Error(string message) => Write("ERROR", message);

        /// <summary>Istisno (exception) bilan birga xatolikni yozish.</summary>
        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message + " | " + ex.GetType().Name + ": " + ex.Message);
        }

        /// <summary>
        /// Xabarni jurnal fayliga (va ixtiyoriy ravishda AutoCAD konsoliga) yozadi.
        /// Log yozish hech qachon asosiy ish jarayonini buzmasligi uchun barcha
        /// istisnolar yutib yuboriladi.
        /// </summary>
        private static void Write(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

            lock (SyncRoot)
            {
                try
                {
                    File.AppendAllText(LogFilePath.Value, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Diskka yozib bo'lmasa, jim qolamiz — log xatosi dasturni to'xtatmasligi kerak.
                }
            }

            if (EchoToEditor)
            {
                EditorEcho.Print($"\n[GoogleSatellite] {message}");
            }
        }
    }
}


namespace GoogleSatelliteCAD.Core
{
    using Autodesk.AutoCAD.ApplicationServices;

    /// <summary>
    /// AutoCAD buyruq qatoriga (Editor) xavfsiz matn chiqaruvchi yordamchi.
    /// Hujjat ochiq bo'lmagan holatlarda ham xatolik bermaydi.
    /// </summary>
    internal static class EditorEcho
    {
        public static void Print(string message)
        {
            try
            {
                Document doc = Application.DocumentManager?.MdiActiveDocument;
                doc?.Editor?.WriteMessage(message);
            }
            catch
            {
                // AutoCAD konteksti mavjud bo'lmasa (masalan, testlarda) — e'tibor bermaymiz.
            }
        }
    }
}
