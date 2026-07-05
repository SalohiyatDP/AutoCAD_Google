using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PluginLicensing
{
    /// <summary>
    /// Buyruq boshida chaqiriladigan "darvoza".
    /// Ishlatilishi (himoyalanadigan har bir buyruq boshida):
    ///     if (!PluginLicensing.LicenseGate.Ensure()) return;
    /// Litsenziya yaroqli bo'lsa true; aks holda buyruq satriga xabar yozib false qaytaradi.
    /// </summary>
    public static class LicenseGate
    {
        public static bool Ensure()
        {
            LicenseResult result = LicenseManager.CheckInstalled();
            if (!result.IsValid)
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage(
                        "\n[" + LicenseConfig.ProductName + " litsenziya] " + result.Message +
                        "\n  Faollashtirish:  " + LicenseConfig.CmdActivate +
                        "\n  Machine ID:      " + LicenseConfig.CmdMachineId + "\n");
                }
                return false;
            }
            return true;
        }
    }
}
