using System;
using System.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.Drawing
{
    /// <summary>
    /// Model-space ko'rinishidagi o'zgarishlarni (Pan / Zoom) kuzatadi va
    /// <see cref="ViewChanged"/> hodisasini chiqaradi.
    ///
    /// AutoCAD'da to'g'ridan-to'g'ri "view changed" hodisasi yo'q. Pan/Zoom
    /// amalga oshganda VIEWCTR (ko'rinish markazi) va VIEWSIZE (ko'rinish balandligi)
    /// tizim o'zgaruvchilari o'zgaradi. Shu sababli
    /// <c>Application.SystemVariableChanged</c> hodisasiga ulanamiz.
    ///
    /// Ko'p marta ketma-ket chaqirilishning oldini olish uchun kechiktirish
    /// (debounce) qo'llaniladi — bu performansni saqlaydi.
    /// </summary>
    public sealed class ViewportTracker : IDisposable
    {
        // Ko'rinish o'zgarganda o'zgaradigan tizim o'zgaruvchilari.
        private static readonly string[] ViewVars = { "VIEWCTR", "VIEWSIZE", "VIEWDIR", "TARGET", "CVPORT" };

        private const int DebounceMs = 250;

        private readonly Document _doc;
        private readonly Timer _debounceTimer;
        private bool _started;
        private bool _disposed;

        /// <summary>Ko'rinish (Pan/Zoom) o'zgarganda chiqariladigan hodisa.</summary>
        public event EventHandler ViewChanged;

        public ViewportTracker(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>Kuzatishni boshlaydi.</summary>
        public void Start()
        {
            if (_started) return;
            Application.SystemVariableChanged += OnSystemVariableChanged;
            _started = true;
            Logger.Info("Ko'rinish kuzatuvchi (ViewportTracker) ishga tushdi.");
        }

        /// <summary>Kuzatishni to'xtatadi.</summary>
        public void Stop()
        {
            if (!_started) return;
            Application.SystemVariableChanged -= OnSystemVariableChanged;
            _started = false;
        }

        private void OnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            // Faqat ko'rinishga aloqador o'zgaruvchilarga e'tibor beramiz.
            for (int i = 0; i < ViewVars.Length; i++)
            {
                if (string.Equals(e.Name, ViewVars[i], StringComparison.OrdinalIgnoreCase))
                {
                    // Debounce: taymerni qayta ishga tushiramiz.
                    _debounceTimer.Change(DebounceMs, Timeout.Infinite);
                    return;
                }
            }
        }

        private void OnDebounceElapsed(object state)
        {
            // AutoCAD obyektlariga faqat asosiy oqimda murojaat qilish xavfsiz.
            try
            {
                Application.DocumentManager.ExecuteInApplicationContext(
                    _ => ViewChanged?.Invoke(this, EventArgs.Empty), null);
            }
            catch
            {
                // Hujjat yopilgan bo'lishi mumkin — e'tibor bermaymiz.
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _debounceTimer?.Dispose();
        }
    }
}
