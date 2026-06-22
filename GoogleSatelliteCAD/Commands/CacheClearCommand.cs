using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.Commands
{
    /// <summary>
    /// GSATCLEAR buyrug'i — disk keshini tozalaydi.
    /// Barcha yuklangan tilelarni o'chiradi va Cache papkasini bo'shatadi.
    /// </summary>
    public sealed class CacheClearCommand
    {
        [CommandMethod("GSATCLEAR", CommandFlags.Modal)]
        public void Execute()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;

            // Tozalashdan oldin joriy kesh hajmini ko'rsatamiz.
            long bytes = PluginContext.Instance.TileManager.Cache.GetSizeBytes();
            double mb = bytes / (1024.0 * 1024.0);
            ed.WriteMessage($"\nJoriy kesh hajmi: {mb:F2} MB. Tozalanmoqda...");

            PluginContext.Instance.TileManager.Cache.Clear();

            ed.WriteMessage("\nKesh muvaffaqiyatli tozalandi.");
        }
    }
}
