using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using GoogleSatelliteCAD.UI;

// "Exception" nomi System va Autodesk.AutoCAD.Runtime ikkalasida ham mavjud.
// Bu yerda har doim .NET ning System.Exception ini nazarda tutamiz (CS0104 ni oldini oladi).
using Exception = System.Exception;

namespace GoogleSatelliteCAD.Core
{
    /// <summary>
    /// Plaginning kirish nuqtasi. AutoCAD DLL'ni NETLOAD orqali yuklaganda
    /// <see cref="IExtensionApplication.Initialize"/> avtomatik chaqiriladi.
    ///
    /// Bu yerda Ribbon menyusi quriladi, hodisalar ulanadi va plagin
    /// boshlang'ich holatga keltiriladi.
    /// </summary>
    public sealed class PluginEntry : IExtensionApplication
    {
        /// <summary>
        /// Plagin yuklanganda bir marta ishga tushadi.
        /// </summary>
        public void Initialize()
        {
            try
            {
                Logger.Info("====================================================");
                Logger.Info("GoogleSatelliteCAD plagini yuklanmoqda...");

                // Mezbon Autodesk mahsulotini (versiya, reliz yili, runtime) aniqlab jurnalga yozamiz.
                // Plagin AutoCAD 2021-2026 va barcha vertikallarni (Mechanical, Civil 3D, Map 3D,
                // Architecture, MEP, Electrical, Plant 3D, Raster Design) qo'llab-quvvatlaydi.
                HostProduct.DetectAndLog();

                // Sozlamalarni yuklaymiz (singleton tashabbuskori).
                var _ = ConfigManager.Instance.Settings;

                // Ribbon mavjud bo'lsa darhol quramiz, aks holda Ribbon paydo bo'lishini kutamiz.
                RibbonUI.Instance.TryBuild();

                // Litsenziya holatini jurnalga yozamiz (yuklashni bloklamaydi; buyruqlar
                // ishlaganda GoogleSatelliteCAD.Licensing.LicenseGate orqali tekshiriladi).
                var lic = GoogleSatelliteCAD.Licensing.LicenseManager.CheckInstalled();
                Logger.Info(lic.IsValid
                    ? "Litsenziya faol. " + lic.Message
                    : "Litsenziya faol emas: " + lic.Message + "  (Machine ID: GSATID, o'rnatish: GSATLIC)");

                // Yangi hujjat faollashganda Ribbon hali tayyor bo'lmagan bo'lsa quramiz.
                Application.DocumentManager.DocumentActivated += OnDocumentActivated;

                // AutoCAD chiqishidan oldin tozalash.
                Application.QuitWillStart += OnQuitWillStart;

                Logger.Info("GoogleSatelliteCAD muvaffaqiyatli yuklandi. Buyruqlar: GSATON, GSATOFF, GSATCLEAR.");
            }
            catch (Exception ex)
            {
                Logger.Error("Plaginni ishga tushirishda xatolik.", ex);
            }
        }

        /// <summary>
        /// Plagin yopilganda (AutoCAD chiqishida) ishga tushadi.
        /// </summary>
        public void Terminate()
        {
            try
            {
                PluginContext.Instance.Disable();
                RibbonUI.Instance.Remove();
                Logger.Info("GoogleSatelliteCAD to'xtatildi.");
            }
            catch (Exception ex)
            {
                Logger.Error("Plaginni to'xtatishda xatolik.", ex);
            }
        }

        private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            // Ribbon ba'zan AutoCAD ishga tushgach kechroq tayyor bo'ladi —
            // hujjat faollashganda yana bir bor qurishga harakat qilamiz.
            RibbonUI.Instance.TryBuild();

            // Agar fon xarita faol bo'lsa, yangi hujjatda ko'rinishni yangilaymiz.
            if (PluginContext.Instance.IsActive)
            {
                PluginContext.Instance.RequestRefresh();
            }
        }

        private void OnQuitWillStart(object sender, EventArgs e)
        {
            try { ConfigManager.Instance.Save(); } catch { /* ignore */ }
        }
    }
}
