using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.Drawing
{
    /// <summary>
    /// GOOGLE_SATELLITE qatlamini boshqaradi: yaratish, sozlash va topish.
    /// Qatlam yo'q bo'lsa avtomatik yaratiladi.
    ///
    /// SOLID: bu klass faqat qatlam (layer) bilan ishlash bilan shug'ullanadi.
    /// </summary>
    public sealed class LayerManager
    {
        /// <summary>Plagin rasterlari joylashadigan qatlam nomi.</summary>
        public const string LayerName = "GOOGLE_SATELLITE";

        /// <summary>
        /// GOOGLE_SATELLITE qatlami mavjudligini ta'minlaydi va uning
        /// ObjectId sini qaytaradi. Mavjud bo'lmasa yaratadi.
        /// Mavjud, ochiq tranzaksiya ichida chaqirilishi kerak.
        /// </summary>
        public ObjectId EnsureLayer(Database db, Transaction tr)
        {
            var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (layerTable.Has(LayerName))
            {
                return layerTable[LayerName];
            }

            // Yangi qatlam yaratamiz.
            layerTable.UpgradeOpen();

            var ltr = new LayerTableRecord
            {
                Name = LayerName,
                // Och kulrang rang — fon raster qatlami uchun neytral.
                Color = Color.FromColorIndex(ColorMethod.ByAci, 8),
                IsPlottable = false   // Plot = False (talabga muvofiq)
            };

            ObjectId layerId = layerTable.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);

            Logger.Info($"\"{LayerName}\" qatlami yaratildi.");
            return layerId;
        }

        /// <summary>
        /// Qatlam qulfini boshqaradi. Fon xarita rasterlari tasodifan
        /// tanlanmasligi/siljimasligi uchun qatlam qulflanadi (Lock = True).
        /// </summary>
        public void SetLocked(Database db, Transaction tr, bool locked)
        {
            var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!layerTable.Has(LayerName)) return;

            var ltr = (LayerTableRecord)tr.GetObject(layerTable[LayerName], OpenMode.ForWrite);
            ltr.IsLocked = locked;
        }
    }
}
