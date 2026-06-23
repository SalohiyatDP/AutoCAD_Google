using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GoogleSatelliteCAD.Core;
using GoogleSatelliteCAD.TileEngine;

namespace GoogleSatelliteCAD.Drawing
{
    /// <summary>
    /// Tile rasmlarini chizmaga <see cref="RasterImage"/> obyektlari sifatida
    /// joylaydi va boshqaradi.
    ///
    /// Har bir tile uchun:
    ///   - GOOGLE_SATELLITE qatlamiga joylanadi (qatlam yo'q bo'lsa yaratiladi),
    ///   - Plot = False, Transparency = 0,
    ///   - chizish tartibi (draw order) eng pastga (fonga) tushiriladi,
    ///   - qatlam qulflanadi (Lock = True), shunda raster tasodifan o'zgarmaydi.
    ///
    /// Pan/Zoom paytida ortiqcha qayta chizishning oldini olish uchun
    /// allaqachon joylashtirilgan tilelar qayta yaratilmaydi (delta yangilash).
    /// </summary>
    public sealed class RasterManager
    {
        private readonly LayerManager _layerManager = new LayerManager();

        // Joriy joylashtirilgan tilelar: kalit -> (RasterImage Id, RasterImageDef Id).
        private readonly Dictionary<string, PlacedEntity> _placed =
            new Dictionary<string, PlacedEntity>();

        // Tracking qaysi ma'lumotlar bazasiga tegishli ekanini eslab qolamiz.
        private Database _trackedDb;

        /// <summary>
        /// Berilgan joylashtirishlar bilan chizmadagi rasterlarni sinxronlaydi:
        /// yangi tilelarni qo'shadi, keraksizlarini o'chiradi.
        /// AutoCAD asosiy oqimida (application context) chaqirilishi kerak.
        /// </summary>
        public void SyncTiles(IEnumerable<TilePlacement> placements)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;

            // Hujjat almashgan bo'lsa, eski tracking yaroqsiz — tozalaymiz.
            if (_trackedDb != db)
            {
                _placed.Clear();
                _trackedDb = db;
            }

            var target = placements.Where(p => !string.IsNullOrEmpty(p.FilePath))
                                   .ToDictionary(p => p.Key, p => p);

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 1. Qatlamni ta'minlaymiz va qulfni vaqtincha ochamiz (yozish uchun).
                    ObjectId layerId = _layerManager.EnsureLayer(db, tr);
                    _layerManager.SetLocked(db, tr, false);

                    // 2. Rasm lug'atini (image dictionary) ta'minlaymiz.
                    ObjectId dictId = RasterImageDef.GetImageDictionary(db);
                    if (dictId.IsNull)
                    {
                        dictId = RasterImageDef.CreateImageDictionary(db);
                    }

                    var btr = (BlockTableRecord)tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    var newlyAdded = new ObjectIdCollection();

                    // 3. Keraksiz tilelarni o'chiramiz.
                    var toRemove = _placed.Keys.Where(k => !target.ContainsKey(k)).ToList();
                    foreach (string key in toRemove)
                    {
                        ErasePlaced(tr, _placed[key]);
                        _placed.Remove(key);
                    }

                    // 4. Yangi tilelarni qo'shamiz.
                    foreach (var kv in target)
                    {
                        if (_placed.ContainsKey(kv.Key)) continue; // allaqachon joyida

                        PlacedEntity placed = PlaceOne(tr, db, btr, dictId, layerId, kv.Value);
                        if (placed != null)
                        {
                            _placed[kv.Key] = placed;
                            newlyAdded.Add(placed.RasterId);
                        }
                    }

                    // 5. Barcha rasterlarni fonga (eng pastki chizish tartibiga) tushiramiz.
                    SendToBottom(tr, btr);

                    // 6. Qatlamni qayta qulflaymiz (Lock = True).
                    _layerManager.SetLocked(db, tr, true);

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error("Rasterlarni sinxronlashda xatolik.", ex);
                    tr.Abort();
                }
            }

            // Ekranni yangilaymiz.
            try { doc.Editor.Regen(); } catch { /* ignore */ }
        }

        /// <summary>Barcha joylashtirilgan rasterlarni va ularning ta'riflarini o'chiradi.</summary>
        public void ClearAll()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) { _placed.Clear(); return; }

            Database db = doc.Database;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    _layerManager.SetLocked(db, tr, false);

                    foreach (PlacedEntity placed in _placed.Values)
                    {
                        ErasePlaced(tr, placed);
                    }
                    _placed.Clear();

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error("Rasterlarni o'chirishda xatolik.", ex);
                    tr.Abort();
                }
            }

            try { doc.Editor.Regen(); } catch { /* ignore */ }
        }

        // ============================ Ichki yordamchilar ============================

        /// <summary>Bitta tile uchun RasterImageDef va RasterImage yaratadi.</summary>
        private PlacedEntity PlaceOne(Transaction tr, Database db, BlockTableRecord btr,
            ObjectId dictId, ObjectId layerId, TilePlacement p)
        {
            try
            {
                var dict = (DBDictionary)tr.GetObject(dictId, OpenMode.ForWrite);

                // Joylashtirish geometriyasini oxirgi marta tekshiramiz (himoya qatlami).
                // Yaroqsiz (nol/cheksiz) vektorlar AutoCAD raster mexanizmini qulatadi.
                Vector3d u = p.UVector;
                Vector3d v = p.VVector;
                if (!IsFinite(p.Origin) || !IsFinite(u) || !IsFinite(v)
                    || u.Length < 1e-6 || v.Length < 1e-6)
                {
                    Logger.Warn($"Tile geometriyasi yaroqsiz, o'tkazib yuborildi: {p.Key}");
                    return null;
                }

                // Rasm ta'rifi (RasterImageDef) — fayl manbasi.
                var rid = new RasterImageDef { SourceFileName = p.FilePath };
                rid.Load();

                // Lug'atga noyob nom bilan qo'shamiz (kalitdagi '/' belgilarni almashtiramiz).
                string defName = "GSAT_" + p.Key.Replace('/', '_');
                if (dict.Contains(defName))
                {
                    // Bir xil nomdagi eski ta'rif bo'lsa — noyob qilamiz.
                    defName += "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                }
                ObjectId defId = dict.SetAt(defName, rid);
                tr.AddNewlyCreatedDBObject(rid, true);

                // Raster obyekti.
                var ri = new RasterImage();
                ri.SetDatabaseDefaults();
                ri.ImageDefId = defId;
                ri.LayerId = layerId;
                ri.ImageTransparency = false;       // Transparency = 0 (shaffof emas)
                ri.ShowImage = true;

                // Joylashtirish geometriyasi: pastki-chap burchak, kenglik va balandlik vektorlari.
                var orientation = new CoordinateSystem3d(p.Origin, p.UVector, p.VVector);
                ri.Orientation = orientation;

                ObjectId riId = btr.AppendEntity(ri);
                tr.AddNewlyCreatedDBObject(ri, true);

                // Raster <-> ta'rif bog'lanishini o'rnatamiz (reaktorlar).
                RasterImage.EnableReactors(true);
                ri.AssociateRasterDef(rid);

                return new PlacedEntity { RasterId = riId, DefId = defId };
            }
            catch (Exception ex)
            {
                Logger.Warn($"Tile joylashtirilmadi {p.Key}: {ex.Message}");
                return null;
            }
        }

        /// <summary>Bitta joylashtirilgan rasterni va uning ta'rifini o'chiradi.</summary>
        private void ErasePlaced(Transaction tr, PlacedEntity placed)
        {
            try
            {
                if (!placed.RasterId.IsNull)
                {
                    var ri = tr.GetObject(placed.RasterId, OpenMode.ForWrite, false);
                    if (ri != null && !ri.IsErased) ri.Erase();
                }
            }
            catch { /* allaqachon o'chirilgan bo'lishi mumkin */ }

            try
            {
                if (!placed.DefId.IsNull)
                {
                    var rid = tr.GetObject(placed.DefId, OpenMode.ForWrite, false) as RasterImageDef;
                    // Har bir tile uchun alohida ta'rif (1:1) bo'lgani uchun,
                    // raster o'chirilgach uning ta'rifini ham o'chiramiz.
                    if (rid != null && !rid.IsErased)
                    {
                        rid.Erase();
                    }
                }
            }
            catch { /* boshqa obyekt foydalanayotgan bo'lsa — qoldiramiz */ }
        }

        /// <summary>Barcha joylashtirilgan rasterlarni chizish tartibida eng pastga tushiradi.</summary>
        private void SendToBottom(Transaction tr, BlockTableRecord btr)
        {
            try
            {
                if (_placed.Count == 0) return;

                var sortId = btr.DrawOrderTableId;
                if (sortId.IsNull) return;

                var sort = (DrawOrderTable)tr.GetObject(sortId, OpenMode.ForWrite);
                var ids = new ObjectIdCollection();
                foreach (PlacedEntity placed in _placed.Values)
                {
                    if (!placed.RasterId.IsNull) ids.Add(placed.RasterId);
                }
                if (ids.Count > 0) sort.MoveToBottom(ids);
            }
            catch (Exception ex)
            {
                Logger.Warn("Chizish tartibini o'zgartirishda xatolik: " + ex.Message);
            }
        }

        /// <summary>Joylashtirilgan raster va uning ta'rifi identifikatorlari.</summary>
        private sealed class PlacedEntity
        {
            public ObjectId RasterId;
            public ObjectId DefId;
        }

        // ---- Validatsiya yordamchilari (native qulashning oldini olish uchun) ----

        private static bool IsFinite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);

        private static bool IsFinite(Point3d p) => IsFinite(p.X) && IsFinite(p.Y) && IsFinite(p.Z);

        private static bool IsFinite(Vector3d v) => IsFinite(v.X) && IsFinite(v.Y) && IsFinite(v.Z);
    }
}
