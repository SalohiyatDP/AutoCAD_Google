using System.Windows.Forms;
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
    /// Litsenziya buyruqlari (WinForms variant). Nomlar LicenseConfig dan olinadi:
    ///   CmdActivate  - aktivatsiya oynasini ochadi (lenta tugmasi shuni chaqiradi)
    ///   CmdMachineId - Machine ID (Product key) ni ko'rsatadi
    ///   CmdInstall   - .lic faylni tanlab o'rnatadi
    /// </summary>
    public class LicenseCommands
    {
        [CommandMethod(LicenseConfig.CmdActivate)]
        public void Activate()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            using (var form = new ActivationForm())
            {
                AcadApp.ShowModalDialog(form);
            }
        }

        [CommandMethod(LicenseConfig.CmdMachineId)]
        public void ShowMachineId()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string id = MachineIdProvider.Get();
            ed.WriteMessage("\nMachine ID: " + id + "\n(Nusxa olindi. Litsenziya olish uchun shu ID ni yuboring.)\n");
            try { Clipboard.SetText(id); } catch { }

            LicenseResult cur = LicenseManager.CheckInstalled();
            MessageBox.Show(
                "Machine ID (nusxa olindi):\n\n" + id +
                "\n\nLitsenziya holati: " + (cur.IsValid ? "Faol" : "Faol emas") + "\n" + cur.Message,
                LicenseConfig.ProductName + " - Machine ID",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [CommandMethod(LicenseConfig.CmdInstall)]
        public void InstallLicense()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string licenseText;
            using (var dlg = new OpenFileDialog
            {
                Title = "Litsenziya faylini tanlang",
                Filter = "Litsenziya fayllari (*.lic;*.txt)|*.lic;*.txt|Barcha fayllar (*.*)|*.*"
            })
            {
                // OpenFileDialog — CommonDialog (Form emas), shuning uchun to'g'ridan-to'g'ri ShowDialog().
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    ed.WriteMessage("\nBekor qilindi.");
                    return;
                }
                try { licenseText = System.IO.File.ReadAllText(dlg.FileName); }
                catch (Exception ex) { ed.WriteMessage("\nFaylni o'qib bo'lmadi: " + ex.Message); return; }
            }

            LicenseResult result = LicenseManager.Install(licenseText);
            ed.WriteMessage("\n[Litsenziya] " + result.Message + "\n");
            MessageBox.Show(result.Message,
                result.IsValid ? "Litsenziya o'rnatildi" : "Litsenziya xatosi",
                MessageBoxButtons.OK,
                result.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}
