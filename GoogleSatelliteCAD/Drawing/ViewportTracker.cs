using System;
using Autodesk.AutoCAD.ApplicationServices;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.Drawing
{
    /// <summary>
    /// Model-space ko'rinishidagi o'zgarishlarni (Pan / Zoom) kuzatadi va
    /// <see cref="ViewChanged"/> hodisasini chiqaradi.
    ///
    /// MUHIM (thread-safety): AutoCAD va uning WPF Ribbon obyektlari faqat
    /// asosiy (UI) oqimga tegishli. Shuning uchun bu kuzatuvchi HECH QANDAY
    /// fon oqimi yoki taymer ishlatmaydi — barcha ish AutoCAD'ning
    /// <c>Application.Idle</c> hodisasi orqali asosiy oqimda bajariladi.
    ///
    /// Ishlash printsipi:
    ///   1. Pan/Zoom bo'lganda VIEWCTR/VIEWSIZE tizim o'zgaruvchilari o'zgaradi
    ///      (SystemVariableChanged — asosiy oqimda chaqiriladi).
    ///   2. "Yangilash kerak" bayrog'i o'rnatiladi va vaqt belgilanadi.
    ///   3. Idle hodisasida (asosiy oqim), oxirgi o'zgarishdan beri kechikish
    ///      (debounce) o'tgan bo'lsa, ViewChanged chiqariladi.
    /// </summary>
    public sealed class ViewportTracker : IDisposable
    {
        // Ko'rinish o'zgarganda o'zgaradigan tizim o'zgaruvchilari.
        private static readonly string[] ViewVars = { "VIEWCTR", "VIEWSIZE", "VIEWDIR", "TARGET", "CVPORT" };

        // Ketma-ket o'zgarishlarni birlashtirish uchun kechikish.
        private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

        private readonly Document _doc;
        private bool _started;
        private bool _disposed;
        private volatile bool _pending;
        private DateTime _pendingSinceUtc;

        /// <summary>Ko'rinish (Pan/Zoom) o'zgarganda chiqariladigan hodisa (asosiy oqimda).</summary>
        public event EventHandler ViewChanged;

        public ViewportTracker(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        /// <summary>Kuzatishni boshlaydi.</summary>
        public void Start()
        {
            if (_started) return;
            Application.SystemVariableChanged += OnSystemVariableChanged;
            Application.Idle += OnIdle;
            _started = true;
            Logger.Info("Ko'rinish kuzatuvchi (ViewportTracker) ishga tushdi.");
        }

        /// <summary>Kuzatishni to'xtatadi.</summary>
        public void Stop()
        {
            if (!_started) return;
            Application.SystemVariableChanged -= OnSystemVariableChanged;
            Application.Idle -= OnIdle;
            _started = false;
        }

        private void OnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            // Bu hodisa asosiy oqimda chiqariladi — faqat bayroq o'rnatamiz.
            for (int i = 0; i < ViewVars.Length; i++)
            {
                if (string.Equals(e.Name, ViewVars[i], StringComparison.OrdinalIgnoreCase))
                {
                    _pending = true;
                    _pendingSinceUtc = DateTime.UtcNow;
                    return;
                }
            }
        }

        private void OnIdle(object sender, EventArgs e)
        {
            // Idle asosiy oqimda, tez-tez chaqiriladi — shuning uchun arzon tekshiruv.
            if (!_pending) return;
            if (DateTime.UtcNow - _pendingSinceUtc < Debounce) return;

            _pending = false;
            try
            {
                ViewChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("ViewChanged ishlovchisida xatolik.", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
