using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GoogleSatelliteCAD.Drawing;
using GoogleSatelliteCAD.Projection;
using GoogleSatelliteCAD.TileEngine;

namespace GoogleSatelliteCAD.Core
{
    /// <summary>
    /// Plaginning markaziy holat va jarayon koordinatori (application coordinator).
    /// Tile yuklash, koordinata almashtirish va rasterlarni chizmaga joylash
    /// jarayonlarini bir joyda boshqaradi.
    ///
    /// Yagona (singleton) namuna sifatida ishlatiladi, chunki AutoCAD seansida
    /// bitta faol fon xarita holati bo'ladi.
    /// </summary>
    public sealed class PluginContext
    {
        private static readonly Lazy<PluginContext> _instance =
            new Lazy<PluginContext>(() => new PluginContext());

        public static PluginContext Instance => _instance.Value;

        private readonly object _refreshLock = new object();
        private CancellationTokenSource _refreshCts;
        private ViewportTracker _tracker;
        private bool _refreshInProgress;

        // Fon oqimida tayyorlangan, asosiy oqimda (Idle) qo'llanishni kutayotgan
        // joylashtirishlar. Faqat Interlocked (atomik) orqali yoziladi/o'qiladi.
        private List<TilePlacement> _pendingPlacements;

        /// <summary>Tile yuklash va kesh menejeri.</summary>
        public TileManager TileManager { get; }

        /// <summary>Rasterlarni chizmaga joylash menejeri.</summary>
        public RasterManager RasterManager { get; }

        /// <summary>Joriy koordinata almashtirgich (drawing CRS &lt;-&gt; WGS84).</summary>
        public CoordinateTransform Transform { get; private set; }

        /// <summary>Fon xarita yoqilganmi.</summary>
        public bool IsActive { get; private set; }

        private PluginContext()
        {
            var settings = ConfigManager.Instance.Settings;
            TileManager = new TileManager(settings);
            RasterManager = new RasterManager();
            Transform = CoordinateTransform.Create(settings.CoordinateSystem);
        }

        /// <summary>
        /// Joriy sozlamalar asosida koordinata almashtirgichni qayta quradi.
        /// Foydalanuvchi koordinata tizimini o'zgartirganda chaqiriladi.
        /// </summary>
        public void RebuildTransform()
        {
            Transform = CoordinateTransform.Create(ConfigManager.Instance.Settings.CoordinateSystem);
            Logger.Info("Koordinata tizimi yangilandi: " + ConfigManager.Instance.Settings.CoordinateSystem);
        }

        // ============================ Yoqish / O'chirish ============================

        /// <summary>
        /// Fon xaritani yoqadi (GSATON). Pan/Zoom kuzatuvchini ishga tushiradi
        /// va ko'rinayotgan hudud uchun tilelarni yuklaydi.
        /// </summary>
        public void Enable()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Logger.Warn("Faol hujjat topilmadi. GSATON bekor qilindi.");
                return;
            }

            if (IsActive)
            {
                Logger.Info("Google Satellite allaqachon yoqilgan. Ko'rinish yangilanadi.");
                RequestRefresh();
                return;
            }

            IsActive = true;

            // Pan/Zoom hodisalarini kuzatuvchini ulaymiz.
            _tracker = new ViewportTracker(doc);
            _tracker.ViewChanged += OnViewChanged;
            _tracker.Start();

            // Yuklab olingan rasterlar faqat ASOSIY oqimda (Idle) chizmaga qo'llanadi.
            // Bu fon oqimidan AutoCAD/WPF obyektlariga tegishni butunlay yo'q qiladi.
            Application.Idle += OnApplyPendingPlacements;

            Logger.Info("Google Satellite yoqildi.");
            RequestRefresh();
        }

        /// <summary>
        /// Fon xaritani o'chiradi (GSATOFF). Barcha raster qatlamlarni o'chiradi,
        /// lekin disk keshini saqlab qoladi.
        /// </summary>
        public void Disable()
        {
            if (!IsActive)
            {
                Logger.Info("Google Satellite allaqachon o'chirilgan.");
                return;
            }

            IsActive = false;

            _refreshCts?.Cancel();
            Application.Idle -= OnApplyPendingPlacements;
            _pendingPlacements = null;

            if (_tracker != null)
            {
                _tracker.ViewChanged -= OnViewChanged;
                _tracker.Dispose();
                _tracker = null;
            }

            RasterManager.ClearAll();
            Logger.Info("Google Satellite o'chirildi (kesh saqlandi).");
        }

        private void OnViewChanged(object sender, EventArgs e)
        {
            if (ConfigManager.Instance.Settings.AutoRefresh)
            {
                RequestRefresh();
            }
        }

        // ============================ Ko'rinishni yangilash ============================

        /// <summary>
        /// Yangilashni so'raydi. Bir vaqtning o'zida faqat bitta yangilash bajariladi;
        /// jarayon davom etayotgan bo'lsa, oldingi so'rov bekor qilinib, yangisi boshlanadi.
        /// </summary>
        public void RequestRefresh()
        {
            if (!IsActive) return;

            // Avvalgi yangilashni bekor qilamiz (debounce/cancel).
            _refreshCts?.Cancel();
            _refreshCts = new CancellationTokenSource();
            CancellationToken token = _refreshCts.Token;

            // Hujjat ko'rinishini AutoCAD asosiy oqimida o'qiymiz.
            ViewWindow view;
            try
            {
                view = ReadCurrentView();
            }
            catch (Exception ex)
            {
                Logger.Error("Joriy ko'rinishni o'qishda xatolik.", ex);
                return;
            }

            if (view == null) return;

            // Tilelarni asinxron yuklab, so'ng AutoCAD oqimida joylaymiz.
            Task.Run(() => RefreshAsync(view, token), token);
        }

        /// <summary>
        /// Asinxron yangilash jarayoni:
        /// 1. Ko'rinayotgan hududni WGS84 ga o'tkazadi.
        /// 2. Mos zoom darajasini hisoblaydi.
        /// 3. Kerakli tilelarni aniqlaydi va yuklaydi.
        /// 4. AutoCAD oqimida rasterlarni joylaydi.
        /// </summary>
        private async Task RefreshAsync(ViewWindow view, CancellationToken token)
        {
            lock (_refreshLock)
            {
                if (_refreshInProgress) { /* yangi token oldingisini bekor qildi */ }
                _refreshInProgress = true;
            }

            try
            {
                // 1. Ko'rinishning to'rt burchagini drawing CRS dan WGS84 (lon/lat) ga o'tkazamiz.
                GeoPoint ll = Transform.DrawingToGeographic(view.MinX, view.MinY);
                GeoPoint ur = Transform.DrawingToGeographic(view.MaxX, view.MaxY);

                // MUHIM: chizma tanlangan koordinata tizimida geografik joylashtirilmagan
                // bo'lsa, natija haqiqiy emas (NaN yoki diapazondan tashqari) bo'lishi mumkin.
                // Bunday qiymatlarni AutoCAD raster mexanizmiga uzatish dasturni
                // qulatadi (Access Violation). Shu sababli oldindan tekshiramiz.
                if (!IsValidGeo(ll) || !IsValidGeo(ur))
                {
                    Logger.Warn(
                        "Joriy ko'rinish tanlangan koordinata tizimida (" + Transform.Name +
                        ") haqiqiy geografik hududga to'g'ri kelmadi. " +
                        "Chizma shu CRS da joylashtirilmagan bo'lishi mumkin. " +
                        "Ribbon -> Coordinate System orqali mos tizimni tanlang " +
                        "(masalan, EPSG:3857 yoki WGS84 Geographic).");
                    return;
                }

                double minLon = Math.Min(ll.Lon, ur.Lon);
                double maxLon = Math.Max(ll.Lon, ur.Lon);
                double minLat = Math.Min(ll.Lat, ur.Lat);
                double maxLat = Math.Max(ll.Lat, ur.Lat);

                // Latitude ni Web Mercator chegarasiga moslaymiz.
                minLat = Clamp(minLat, -Mercator.MaxLatitude, Mercator.MaxLatitude);
                maxLat = Clamp(maxLat, -Mercator.MaxLatitude, Mercator.MaxLatitude);

                // 2. Zoom darajasini hisoblaymiz (ekran piksellariga moslaymiz).
                int zoom = ComputeZoom(minLon, maxLon, minLat, maxLat, view.PixelWidth);
                zoom = Math.Max(0, Math.Min(zoom, ConfigManager.Instance.Settings.MaxZoom));

                // 3. Kerakli tilelar ro'yxatini tuzamiz.
                List<TileInfo> tiles = TileSystem.GetTilesForBounds(minLon, minLat, maxLon, maxLat, zoom).ToList();
                if (tiles.Count == 0) return;

                // Xavfsizlik chegarasi: juda ko'p tile bo'lsa (koordinata mosligi buzilgan
                // yoki haddan tashqari kichik zoom), yangilashni o'tkazib yuboramiz.
                if (tiles.Count > MaxTilesPerRefresh)
                {
                    Logger.Warn($"Juda ko'p tile talab qilindi ({tiles.Count} > {MaxTilesPerRefresh}). " +
                                "Yangilash o'tkazib yuborildi. Ko'rinishni kattalashtiring (zoom in).");
                    return;
                }

                double centerLatLog = (minLat + maxLat) / 2.0;
                double centerLonLog = (minLon + maxLon) / 2.0;
                Logger.Info($"Yangilash: CRS={Transform.Name}; markaz lon={centerLonLog:F5}, lat={centerLatLog:F5}; " +
                            $"zoom={zoom}; tile soni={tiles.Count}.");

                // 4. Tilelarni yuklaymiz (asinxron, parallel) — bekor qilinishi mumkin.
                IReadOnlyList<DownloadedTile> downloaded =
                    await TileManager.EnsureTilesAsync(tiles, token).ConfigureAwait(false);

                int okCount = 0;
                foreach (var d in downloaded) if (d.FilePath != null) okCount++;
                Logger.Info($"Tilelar yuklandi/keshdan olindi: {okCount}/{tiles.Count}.");

                if (token.IsCancellationRequested) return;

                // 5. Har bir tile uchun joylashtirish geometriyasini hisoblaymiz.
                var placements = new List<TilePlacement>(downloaded.Count);
                foreach (DownloadedTile dt in downloaded)
                {
                    if (dt.FilePath == null) continue;

                    TileBounds b = TileSystem.GetTileGeoBounds(dt.Tile);

                    // Tile burchaklarini drawing CRS ga qaytaramiz (affin joylashtirish uchun).
                    DrawingPoint dOrigin = Transform.GeographicToDrawing(b.WestLon, b.SouthLat);     // pastki-chap
                    DrawingPoint dLowerRight = Transform.GeographicToDrawing(b.EastLon, b.SouthLat);
                    DrawingPoint dUpperLeft = Transform.GeographicToDrawing(b.WestLon, b.NorthLat);

                    // Hisoblangan koordinatalar haqiqiy (chekli) ekanini tekshiramiz.
                    if (!IsValidPoint(dOrigin) || !IsValidPoint(dLowerRight) || !IsValidPoint(dUpperLeft))
                        continue;

                    Point3d origin = ToPoint(dOrigin);
                    Vector3d uVec = ToPoint(dLowerRight) - origin; // gorizontal (rasm kengligi)
                    Vector3d vVec = ToPoint(dUpperLeft) - origin;  // vertikal (rasm balandligi)

                    // Vektorlar nol bo'lmasligi va aql bovar qiladigan kattalikda bo'lishi kerak.
                    if (!IsValidVector(uVec) || !IsValidVector(vVec))
                        continue;

                    // Buzilish (shear/aspect) filtri: RasterImage faqat affin (parallelogramm)
                    // joylashtirishni qo'llab-quvvatlaydi. Gauss-Kruger (Pulkovo) o'z zonasidan
                    // uzoqda tilelarni qattiq qiyshaytiradi — bunday tilelar chizilsa "chalkashlik"
                    // hosil bo'ladi. Shu sababli juda cho'zilgan yoki qiyshaygan tilelarni
                    // o'tkazib yuboramiz. (Web Mercator'da barcha tilelar ideal kvadrat — o'tadi.)
                    if (!IsAffineFriendly(uVec, vVec))
                        continue;

                    placements.Add(new TilePlacement
                    {
                        Tile = dt.Tile,
                        FilePath = dt.FilePath,
                        Origin = origin,
                        UVector = uVec,
                        VVector = vVec
                    });
                }

                if (placements.Count == 0)
                {
                    Logger.Warn("Joylashtirish uchun haqiqiy tile topilmadi (geometriya yaroqsiz).");
                    return;
                }

                Logger.Info($"Joylashtirishga tayyor tile: {placements.Count}. Asosiy oqimda (Idle) qo'llanadi.");

                if (token.IsCancellationRequested) return;

                // 6. Rasterlarni ASOSIY oqimga uzatamiz. Haqiqiy chizma o'zgarishi
                //    keyingi Application.Idle hodisasida (OnApplyPendingPlacements) bajariladi.
                //    Bu fon oqimidan AutoCAD'ga tegmaslikni kafolatlaydi.
                Interlocked.Exchange(ref _pendingPlacements, placements);
            }
            catch (OperationCanceledException)
            {
                // Ko'rinish yana o'zgardi — bu normal holat.
            }
            catch (Exception ex)
            {
                Logger.Error("Ko'rinishni yangilashda kutilmagan xatolik.", ex);
            }
            finally
            {
                lock (_refreshLock) { _refreshInProgress = false; }
            }
        }

        // ============================ Yordamchi metodlar ============================

        /// <summary>
        /// Berilgan geografik chegaralar va ekran kengligi (piksel) asosida
        /// Web Mercator zoom darajasini hisoblaydi.
        /// </summary>
        private int ComputeZoom(double minLon, double maxLon, double minLat, double maxLat, int pixelWidth)
        {
            if (pixelWidth <= 0) pixelWidth = 1024;

            // Ko'rinishning markaziy kenglik (latitude) bo'yicha Web Mercator metr/piksel.
            double centerLat = (minLat + maxLat) / 2.0;

            // Ko'rinish kengligini Web Mercator metrlarida hisoblaymiz.
            double west = Mercator.LonToMeters(minLon);
            double east = Mercator.LonToMeters(maxLon);
            double viewWidthMeters = Math.Abs(east - west);
            if (viewWidthMeters <= 0) return ConfigManager.Instance.Settings.MaxZoom;

            double metersPerPixelView = viewWidthMeters / pixelWidth;

            // Web Mercator: zoom=0 da bir piksel ekvatorda ~156543.034 metr.
            const double initialResolution = 2.0 * Math.PI * Mercator.EarthRadius / TileSystem.TileSize;

            double zoom = Math.Log(initialResolution / metersPerPixelView, 2.0);
            return (int)Math.Round(zoom);
        }

        /// <summary>WGS84 -> drawing natijasini AutoCAD Point3d ga o'giradi.</summary>
        private static Point3d ToPoint(DrawingPoint p) => new Point3d(p.X, p.Y, 0.0);

        // ---- Haqiqiylik (validatsiya) yordamchilari ----

        /// <summary>Bir yangilashda joylashtiriladigan maksimal tile soni (xavfsizlik chegarasi).</summary>
        private const int MaxTilesPerRefresh = 800;

        /// <summary>.NET Framework 4.8 da double.IsFinite mavjud emas — o'zimiz tekshiramiz.</summary>
        private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>Geografik nuqta haqiqiy va diapazonda (lon ±180, lat ±90) ekanini tekshiradi.</summary>
        private static bool IsValidGeo(GeoPoint g)
        {
            return IsFinite(g.Lon) && IsFinite(g.Lat)
                   && g.Lon >= -180.0 && g.Lon <= 180.0
                   && g.Lat >= -90.0 && g.Lat <= 90.0;
        }

        /// <summary>Chizma nuqtasi chekli va aql bovar qiladigan kattalikda ekanini tekshiradi.</summary>
        private static bool IsValidPoint(DrawingPoint p)
        {
            const double limit = 1e12; // o'ta katta koordinatalar raster mexanizmini qulatadi
            return IsFinite(p.X) && IsFinite(p.Y)
                   && Math.Abs(p.X) < limit && Math.Abs(p.Y) < limit;
        }

        /// <summary>Joylashtirish vektori nol bo'lmagan, chekli va o'ta katta emasligini tekshiradi.</summary>
        private static bool IsValidVector(Vector3d v)
        {
            if (!IsFinite(v.X) || !IsFinite(v.Y) || !IsFinite(v.Z)) return false;
            double len = v.Length;
            return len > 1e-6 && len < 1e10;
        }

        /// <summary>
        /// Tile affin (parallelogramm) joylashtirishga yaroqlimi — ya'ni kam qiyshaygan
        /// va cho'zilmaganligini tekshiradi. Bu Gauss-Kruger zonasidan uzoqdagi qattiq
        /// buzilgan tilelarni filtrlaydi (Web Mercator'da har doim true).
        /// </summary>
        private static bool IsAffineFriendly(Vector3d u, Vector3d v)
        {
            double uLen = u.Length;
            double vLen = v.Length;
            if (uLen < 1e-9 || vLen < 1e-9) return false;

            // 1) Tomonlar nisbati ~1 ga yaqin bo'lsin (cho'zilmagan).
            double ratio = uLen / vLen;
            if (ratio < 0.5 || ratio > 2.0) return false;

            // 2) U va V vektorlari deyarli perpendikulyar bo'lsin (kam qiyshaygan).
            //    cos(burchak) = (u·v)/(|u||v|); perpendikulyar uchun ~0.
            double cos = Math.Abs(u.DotProduct(v) / (uLen * vLen));
            if (cos > 0.34) return false; // 90° ± ~20° doirasida

            return true;
        }

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        /// <summary>
        /// Joriy model-space ko'rinishini o'qib, drawing koordinatalaridagi
        /// ko'rinish to'rtburchagi va ekran piksel o'lchamini qaytaradi.
        /// </summary>
        private ViewWindow ReadCurrentView()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;

            Editor ed = doc.Editor;
            using (ViewTableRecord vtr = ed.GetCurrentView())
            {
                // CenterPoint DCS (Display Coordinate System) da. Yuqoridan ko'rinish (plan)
                // uchun bu WCS X/Y bilan mos keladi.
                double cx = vtr.CenterPoint.X;
                double cy = vtr.CenterPoint.Y;
                double halfW = vtr.Width / 2.0;
                double halfH = vtr.Height / 2.0;

                // Ekran piksel o'lchami (SCREENSIZE tizim o'zgaruvchisi).
                int pxWidth = 1024;
                try
                {
                    object ss = Application.GetSystemVariable("SCREENSIZE");
                    if (ss is Point2d p2d) pxWidth = (int)p2d.X;
                }
                catch { /* standart qiymat ishlatiladi */ }

                return new ViewWindow
                {
                    MinX = cx - halfW,
                    MinY = cy - halfH,
                    MaxX = cx + halfW,
                    MaxY = cy + halfH,
                    PixelWidth = pxWidth
                };
            }
        }

        /// <summary>
        /// Application.Idle (ASOSIY oqim) da chaqiriladi. Fon oqimida tayyorlangan
        /// joylashtirishlar bo'lsa, ularni chizmaga qo'llaydi. Bu yagona joy bo'lib,
        /// rasterlar shu yerda — har doim asosiy oqimda — yaratiladi.
        /// </summary>
        private void OnApplyPendingPlacements(object sender, EventArgs e)
        {
            if (!IsActive) return;

            // Atomik ravishda kutayotgan ro'yxatni olamiz va bo'shatamiz.
            List<TilePlacement> placements = Interlocked.Exchange(ref _pendingPlacements, null);
            if (placements == null) return;

            try
            {
                RasterManager.SyncTiles(placements);
                Logger.Info($"Rasterlar chizmaga qo'llandi: {placements.Count} ta.");
            }
            catch (Exception ex)
            {
                Logger.Error("Rasterlarni chizmaga qo'llashda xatolik.", ex);
            }
        }

        /// <summary>Drawing koordinatalaridagi ko'rinish oynasi modeli.</summary>
        private sealed class ViewWindow
        {
            public double MinX, MinY, MaxX, MaxY;
            public int PixelWidth;
        }
    }
}
