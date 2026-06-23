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

        // Oxirgi qo'llangan zoom darajasi (gisterezis uchun). -1 = hali aniqlanmagan.
        private int _lastZoom = -1;

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
            _lastZoom = -1; // CRS o'zgardi — zoom gisterezisini tiklaymiz.
            Logger.Info("Koordinata tizimi yangilandi: " + ConfigManager.Instance.Settings.CoordinateSystem);
        }

        /// <summary>
        /// Ko'rinishni O'zbekiston hududidagi standart nuqtaga (Toshkent) olib boradi
        /// va fon xaritani yoqadi. Bo'sh chizmada "xarita ko'rinmayapti" holatini hal qiladi.
        /// Asosiy (UI) oqimda, buyruq kontekstida chaqirilishi kerak.
        /// </summary>
        public void GoHome()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            if (!IsActive) Enable();

            // Standart nuqta (Toshkent) va ~20 km ko'rinish kengligini joriy CRS ga o'tkazamiz.
            const double groundWidthMeters = 20000.0;
            double dLon = groundWidthMeters / 2.0 / (111320.0 * Math.Cos(HomeLat * Math.PI / 180.0));

            DrawingPoint center = Transform.GeographicToDrawing(HomeLon, HomeLat);
            DrawingPoint east = Transform.GeographicToDrawing(HomeLon + dLon, HomeLat);
            DrawingPoint north = Transform.GeographicToDrawing(HomeLon, HomeLat + dLon);

            double halfW = Math.Abs(east.X - center.X);
            double halfH = Math.Abs(north.Y - center.Y);
            double width = Math.Max(halfW, halfH) * 2.0;
            if (!(width > 0) || double.IsNaN(width) || double.IsInfinity(width)) width = groundWidthMeters;

            try
            {
                Editor ed = doc.Editor;
                using (ViewTableRecord vtr = ed.GetCurrentView())
                {
                    vtr.CenterPoint = new Point2d(center.X, center.Y);
                    vtr.Width = width;
                    vtr.Height = width;
                    ed.SetCurrentView(vtr);
                }
                Logger.Info($"Uyga (Toshkent) o'tildi: markaz drawing=({center.X:F1},{center.Y:F1}).");
            }
            catch (Exception ex)
            {
                Logger.Error("Uyga o'tishda xatolik.", ex);
            }

            RequestRefresh();
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
            _lastZoom = -1;

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
                // 1. Ko'rinishning TO'RTTA burchagini drawing CRS dan WGS84 (lon/lat) ga o'tkazamiz.
                //    Pulkovo kabi tizimlarda meridian konvergensiyasi tufayli ko'rinish geografik
                //    jihatdan biroz burilgan bo'ladi — shuning uchun faqat diagonal ikki burchak
                //    yetarli emas, to'rttala burchakdan geografik chegara (bbox) quramiz.
                //    Bu chegarani BARQAROR qiladi (panda "sakrashni" kamaytiradi) va to'liq qoplaydi.
                GeoPoint c1 = Transform.DrawingToGeographic(view.MinX, view.MinY);
                GeoPoint c2 = Transform.DrawingToGeographic(view.MaxX, view.MaxY);
                GeoPoint c3 = Transform.DrawingToGeographic(view.MaxX, view.MinY);
                GeoPoint c4 = Transform.DrawingToGeographic(view.MinX, view.MaxY);

                if (!IsValidGeo(c1) || !IsValidGeo(c2) || !IsValidGeo(c3) || !IsValidGeo(c4))
                {
                    Logger.Warn(
                        "Joriy ko'rinish tanlangan koordinata tizimida (" + Transform.Name +
                        ") haqiqiy geografik hududga to'g'ri kelmadi. " +
                        "Chizma shu CRS da joylashtirilmagan bo'lishi mumkin. " +
                        "Ribbon -> Coordinate System orqali mos tizimni tanlang " +
                        "(masalan, EPSG:3857 yoki WGS84 Geographic).");
                    return;
                }

                double minLon = Math.Min(Math.Min(c1.Lon, c2.Lon), Math.Min(c3.Lon, c4.Lon));
                double maxLon = Math.Max(Math.Max(c1.Lon, c2.Lon), Math.Max(c3.Lon, c4.Lon));
                double minLat = Math.Min(Math.Min(c1.Lat, c2.Lat), Math.Min(c3.Lat, c4.Lat));
                double maxLat = Math.Max(Math.Max(c1.Lat, c2.Lat), Math.Max(c3.Lat, c4.Lat));

                // Latitude ni Web Mercator chegarasiga moslaymiz.
                minLat = Clamp(minLat, -Mercator.MaxLatitude, Mercator.MaxLatitude);
                maxLat = Clamp(maxLat, -Mercator.MaxLatitude, Mercator.MaxLatitude);

                // 2. Zoom darajasini hisoblaymiz (ekran piksellariga moslaymiz) + GISTEREZIS.
                //    Panda zoom X.5 chegarasida "tebranib" (flip-flop) butun mozaykani qayta
                //    chizmasligi uchun, oldingi zoomga yaqin bo'lsa o'sha saqlanadi.
                double rawZoom = ComputeRawZoom(minLon, maxLon, view.PixelWidth);
                int maxZoom = ConfigManager.Instance.Settings.MaxZoom;
                int zoom;
                int last = _lastZoom;
                if (last >= 0 && Math.Abs(rawZoom - last) < 0.6)
                    zoom = last;
                else
                    zoom = (int)Math.Round(rawZoom);
                zoom = Math.Max(0, Math.Min(zoom, maxZoom));
                _lastZoom = zoom;

                // 3. Faqat O'ZBEKISTON RESPUBLIKASI hududidagi tilelarni yuklaymiz.
                //    Ko'rinish chegarasini O'zbekiston chegara to'rtburchagi bilan kesib olamiz —
                //    butun (umumiy) xarita yuklanmaydi, faqat mamlakat hududi ko'rsatiladi.
                double tMinLon = Math.Max(minLon, UzWestLon);
                double tMaxLon = Math.Min(maxLon, UzEastLon);
                double tMinLat = Math.Max(minLat, UzSouthLat);
                double tMaxLat = Math.Min(maxLat, UzNorthLat);

                if (tMinLon >= tMaxLon || tMinLat >= tMaxLat)
                {
                    // Ko'rinish O'zbekiston hududidan butunlay tashqarida — eski tilelarni tozalaymiz.
                    Logger.Warn($"Ko'rinish O'zbekiston hududidan TASHQARIDA: " +
                                $"lon {minLon:F3}..{maxLon:F3}, lat {minLat:F3}..{maxLat:F3} " +
                                $"(ruxsat: lon {UzWestLon}..{UzEastLon}, lat {UzSouthLat}..{UzNorthLat}). " +
                                "Xaritani ko'rish uchun O'zbekiston hududiga o'ting.");
                    Interlocked.Exchange(ref _pendingPlacements, new List<TilePlacement>());
                    return;
                }

                List<TileInfo> tiles = TileSystem.GetTilesForBounds(tMinLon, tMinLat, tMaxLon, tMaxLat, zoom).ToList();
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

                // 5. UNIFORM LOKAL O'XSHASHLIK (similarity) TRANSFORMI bilan joylashtirish.
                //    Web Mercator tilelari mercator metrlarida ideal kvadrat to'r hosil qiladi.
                //    Ularni drawing CRS ga joylash uchun ko'rinish MARKAZIDA hisoblangan YAGONA
                //    chiziqli transform (masshtab + burilish) ishlatamiz. Natijada barcha tilelar
                //    bir xil o'lcham va yo'nalishda bo'lib, mukammal, uzluksiz to'r hosil qiladi —
                //    Pulkovo'da ham bo'shliq/qoplama va qiyshayish bo'lmaydi. Web Mercator'da
                //    transform birlik (identity) bo'lib, joylashtirish piksel-aniq mos keladi.
                double cx = (view.MinX + view.MaxX) / 2.0;
                double cy = (view.MinY + view.MaxY) / 2.0;
                GeoPoint refGeo = Transform.DrawingToGeographic(cx, cy);
                if (!IsValidGeo(refGeo)) return;

                double refMx, refMy;
                Mercator.LonLatToMeters(refGeo.Lon, refGeo.Lat, out refMx, out refMy);

                // Mercator metr -> drawing chiziqli transformini markazda sonli usulda topamiz.
                // (Mercator ham, Pulkovo ham konform — bu transform masshtab + burilishdan iborat.)
                const double delta = 1.0; // 1 mercator metr
                DrawingPoint p0 = DrawingFromMercator(refMx, refMy);
                DrawingPoint pE = DrawingFromMercator(refMx + delta, refMy);
                DrawingPoint pN = DrawingFromMercator(refMx, refMy + delta);

                double mxx = (pE.X - p0.X) / delta; // sharq -> drawing X
                double myx = (pE.Y - p0.Y) / delta; // sharq -> drawing Y
                double mxy = (pN.X - p0.X) / delta; // shimol -> drawing X
                double myy = (pN.Y - p0.Y) / delta; // shimol -> drawing Y

                if (!IsFinite(mxx) || !IsFinite(myx) || !IsFinite(mxy) || !IsFinite(myy)
                    || !IsFinite(p0.X) || !IsFinite(p0.Y))
                {
                    Logger.Warn("Lokal transformni hisoblab bo'lmadi (yaroqsiz qiymatlar).");
                    return;
                }

                var placements = new List<TilePlacement>(downloaded.Count);
                foreach (DownloadedTile dt in downloaded)
                {
                    if (dt.FilePath == null) continue;

                    // Tile'ning Web Mercator chegaralari (metr) — uniform to'r.
                    double tw, te, ts, tn;
                    TileMercatorBounds(dt.Tile, out tw, out te, out ts, out tn);

                    double ox = tw - refMx; // tile pastki-chap, markazga nisbatan (sharq)
                    double oy = ts - refMy; // (shimol)

                    var origin = new Point3d(
                        p0.X + mxx * ox + mxy * oy,
                        p0.Y + myx * ox + myy * oy,
                        0.0);

                    double dw = te - tw; // tile kengligi (mercator metr)
                    double dh = tn - ts; // tile balandligi (mercator metr)

                    var uVec = new Vector3d(mxx * dw, myx * dw, 0.0); // gorizontal (kenglik)
                    var vVec = new Vector3d(mxy * dh, myy * dh, 0.0); // vertikal (balandlik)

                    if (!IsFinite(origin.X) || !IsFinite(origin.Y)
                        || !IsValidVector(uVec) || !IsValidVector(vVec))
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
        /// Berilgan longitude chegaralari va ekran kengligi (piksel) asosida
        /// Web Mercator zoom darajasini (kasrli, yaxlitlanmagan) hisoblaydi.
        /// Faqat longitude span'ga bog'liq — bu barcha CRS'larda barqaror natija beradi.
        /// </summary>
        private double ComputeRawZoom(double minLon, double maxLon, int pixelWidth)
        {
            if (pixelWidth <= 0) pixelWidth = 1024;

            double lonSpan = Math.Abs(maxLon - minLon);
            if (lonSpan < 1e-9) return ConfigManager.Instance.Settings.MaxZoom;

            // Ko'rinish kengligini Web Mercator metrlarida hisoblaymiz.
            double viewWidthMeters = Math.Abs(Mercator.LonToMeters(maxLon) - Mercator.LonToMeters(minLon));
            if (viewWidthMeters <= 0) return ConfigManager.Instance.Settings.MaxZoom;

            double metersPerPixelView = viewWidthMeters / pixelWidth;

            // Web Mercator: zoom=0 da bir piksel ekvatorda ~156543.034 metr.
            const double initialResolution = 2.0 * Math.PI * Mercator.EarthRadius / TileSystem.TileSize;

            return Math.Log(initialResolution / metersPerPixelView, 2.0);
        }

        // ---- Haqiqiylik (validatsiya) yordamchilari ----

        /// <summary>Bir yangilashda joylashtiriladigan maksimal tile soni (xavfsizlik chegarasi).</summary>
        private const int MaxTilesPerRefresh = 800;

        // O'zbekiston Respublikasi taxminiy chegara to'rtburchagi (WGS84, gradus).
        // Faqat shu hudud ichidagi tilelar yuklanadi — butun dunyo xaritasi yuklanmaydi.
        private const double UzWestLon = 55.9;
        private const double UzEastLon = 73.25;
        private const double UzSouthLat = 37.1;
        private const double UzNorthLat = 45.65;

        // "Uyga" (GSATHOME) buyrug'i uchun standart nuqta — Toshkent markazi.
        private const double HomeLon = 69.2797;
        private const double HomeLat = 41.3111;

        /// <summary>.NET Framework 4.8 da double.IsFinite mavjud emas — o'zimiz tekshiramiz.</summary>
        private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>Geografik nuqta haqiqiy va diapazonda (lon ±180, lat ±90) ekanini tekshiradi.</summary>
        private static bool IsValidGeo(GeoPoint g)
        {
            return IsFinite(g.Lon) && IsFinite(g.Lat)
                   && g.Lon >= -180.0 && g.Lon <= 180.0
                   && g.Lat >= -90.0 && g.Lat <= 90.0;
        }

        /// <summary>
        /// Web Mercator metr koordinatasini joriy CRS chizma koordinatasiga o'tkazadi
        /// (mercator -> lon/lat -> drawing). Uniform transformni hisoblashda ishlatiladi.
        /// </summary>
        private DrawingPoint DrawingFromMercator(double mx, double my)
        {
            double lon, lat;
            Mercator.MetersToLonLat(mx, my, out lon, out lat);
            return Transform.GeographicToDrawing(lon, lat);
        }

        /// <summary>
        /// Tile'ning Web Mercator (EPSG:3857) chegaralarini metrlarda qaytaradi.
        /// Tilelar mercator metrlarida ideal kvadrat to'r tashkil etadi.
        /// </summary>
        private static void TileMercatorBounds(TileInfo t, out double west, out double east,
                                               out double south, out double north)
        {
            double half = Math.PI * Mercator.EarthRadius;   // ~20037508.34
            double world = 2.0 * half;
            double n = TileSystem.MapSizeTiles(t.Z);

            west = -half + t.X / n * world;
            east = -half + (t.X + 1) / n * world;
            north = half - t.Y / n * world;          // Y indeks pastga o'sadi
            south = half - (t.Y + 1) / n * world;
        }

        /// <summary>Joylashtirish vektori nol bo'lmagan, chekli va o'ta katta emasligini tekshiradi.</summary>
        private static bool IsValidVector(Vector3d v)
        {
            if (!IsFinite(v.X) || !IsFinite(v.Y) || !IsFinite(v.Z)) return false;
            double len = v.Length;
            return len > 1e-6 && len < 1e10;
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
