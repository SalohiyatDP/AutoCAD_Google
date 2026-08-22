using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.Commands
{
    /// <summary>
    /// GSATON buyrug'i — Google Satellite fon xaritasini yoqadi.
    /// Tile serverga ulanib, ko'rinayotgan hudud uchun rasmlarni yuklaydi
    /// va DWG ichida fon sifatida ko'rsatadi.
    /// </summary>
    public sealed class MapOnCommand
    {
        [CommandMethod("GSATON", CommandFlags.Modal)]
        public void Execute()
        {
            if (!GoogleSatelliteCAD.Licensing.LicenseGate.Ensure()) return;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.Editor.WriteMessage("\nGoogle Satellite yoqilmoqda...");
            PluginContext.Instance.Enable();
        }

        /// <summary>
        /// GSATREFRESH buyrug'i — ko'rinib turgan hududni qayta yuklaydi (yangilaydi).
        /// Xarita o'chiq bo'lsa, avval yoqadi.
        /// </summary>
        [CommandMethod("GSATREFRESH", CommandFlags.Modal)]
        public void Refresh()
        {
            if (!GoogleSatelliteCAD.Licensing.LicenseGate.Ensure()) return;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.Editor.WriteMessage("\nXarita yangilanmoqda...");
            if (PluginContext.Instance.IsActive)
                PluginContext.Instance.RequestRefresh();
            else
                PluginContext.Instance.Enable();
        }
    }
}
