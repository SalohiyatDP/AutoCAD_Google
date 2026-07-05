using System;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;

// Yangi buyruqlar sinfini AutoCAD ro'yxatga olishi uchun CommandClass shart
// (AssemblyInfo.cs da boshqa CommandClass'lar mavjud).
[assembly: CommandClass(typeof(GoogleSatelliteCAD.Licensing.LicenseCommands))]

namespace GoogleSatelliteCAD.Licensing
{
    /// <summary>
    /// Litsenziya buyruqlari:
    ///   GSATID  - shu kompyuterning Machine ID sini ko'rsatadi (vendorga yuborish uchun).
    ///   GSATLIC - litsenziya faylini (.lic) tanlab o'rnatadi.
    /// Bu buyruqlar litsenziyasiz ham ishlaydi.
    /// </summary>
    public sealed class LicenseCommands
    {
        [CommandMethod("GSATID", CommandFlags.Modal)]
        public void ShowMachineId()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string id = MachineIdProvider.Get();
            ed.WriteMessage("\nMachine ID: " + id + "\n(Nusxa olindi. Ushbu ID ni litsenziya olish uchun yuboring.)\n");

            try { System.Windows.Clipboard.SetText(id); } catch { /* clipboard band bo'lishi mumkin */ }

            LicenseResult cur = LicenseManager.CheckInstalled();
            System.Windows.MessageBox.Show(
                "Machine ID (nusxa olindi):\n\n" + id +
                "\n\nLitsenziya holati: " + (cur.IsValid ? "Faol" : "Faol emas") + "\n" + cur.Message,
                "GoogleSatelliteCAD - Machine ID",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        [CommandMethod("GSATLIC", CommandFlags.Modal)]
        public void InstallLicense()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Litsenziya faylini tanlang",
                Filter = "Litsenziya fayllari (*.lic;*.txt)|*.lic;*.txt|Barcha fayllar (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true)
            {
                ed.WriteMessage("\nBekor qilindi.");
                return;
            }

            string licenseText;
            try { licenseText = System.IO.File.ReadAllText(dlg.FileName); }
            catch (Exception ex) { ed.WriteMessage("\nFaylni o'qib bo'lmadi: " + ex.Message); return; }

            LicenseResult result = LicenseManager.Install(licenseText);
            ed.WriteMessage("\n[Litsenziya] " + result.Message + "\n");
            System.Windows.MessageBox.Show(result.Message,
                result.IsValid ? "Litsenziya o'rnatildi" : "Litsenziya xatosi",
                System.Windows.MessageBoxButton.OK,
                result.IsValid ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);
        }
    }
}
