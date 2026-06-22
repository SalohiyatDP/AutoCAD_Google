using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.Commands
{
    /// <summary>
    /// GSATOFF buyrug'i — Google Satellite fon xaritasini o'chiradi.
    /// Barcha raster qatlamlarni chizmadan o'chiradi, ammo disk keshini
    /// saqlab qoladi (keyingi yoqishda tezroq yuklanadi).
    /// </summary>
    public sealed class MapOffCommand
    {
        [CommandMethod("GSATOFF", CommandFlags.Modal)]
        public void Execute()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.Editor.WriteMessage("\nGoogle Satellite o'chirilmoqda...");
            PluginContext.Instance.Disable();
        }
    }
}
