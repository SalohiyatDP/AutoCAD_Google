using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using GoogleSatelliteCAD.UI;

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

                // Sozlamalarni yuklaymiz (singleton tashabbuskori).
                var _ = ConfigManager.Instance.Settings;

                // Ribbon mavjud bo'lsa darhol quramiz, aks holda Ribbon paydo bo'lishini kutamiz.
                RibbonUI.Instance.TryBuild();

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
