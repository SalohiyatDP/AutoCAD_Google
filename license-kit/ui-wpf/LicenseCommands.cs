using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;
using Exception = System.Exception;

// Yangi buyruqlar sinfini AutoCAD ro'yxatga olishi uchun (agar loyihada boshqa
// [assembly: CommandClass] bo'lsa) shart. Zarar qilmaydi, shuning uchun doim qoldiring.
[assembly: CommandClass(typeof(PluginLicensing.LicenseCommands))]

namespace PluginLicensing
{
    /// <summary>
    /// Litsenziya buyruqlari (WPF variant). Nomlar LicenseConfig dan olinadi:
    ///   CmdActivate  - aktivatsiya oynasini ochadi (lenta tugmasi shuni chaqiradi)
    ///   CmdMachineId - Machine ID (Product key) ni ko'rsatadi
    ///   CmdInstall   - .lic faylni tanlab o'rnatadi
    /// Bu buyruqlar litsenziyasiz ham ishlaydi.
    /// </summary>
    public sealed class LicenseCommands
    {
        [CommandMethod(LicenseConfig.CmdActivate, CommandFlags.Modal)]
        public void Activate()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var win = new ActivationWindow();
            AcadApp.ShowModalWindow(win);
        }

        [CommandMethod(LicenseConfig.CmdMachineId, CommandFlags.Modal)]
        public void ShowMachineId()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string id = MachineIdProvider.Get();
            ed.WriteMessage("\nMachine ID: " + id + "\n(Nusxa olindi. Litsenziya olish uchun shu ID ni yuboring.)\n");
            try { System.Windows.Clipboard.SetText(id); } catch { }

            LicenseResult cur = LicenseManager.CheckInstalled();
            System.Windows.MessageBox.Show(
                "Machine ID (nusxa olindi):\n\n" + id +
                "\n\nLitsenziya holati: " + (cur.IsValid ? "Faol" : "Faol emas") + "\n" + cur.Message,
                LicenseConfig.ProductName + " - Machine ID",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        [CommandMethod(LicenseConfig.CmdInstall, CommandFlags.Modal)]
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
