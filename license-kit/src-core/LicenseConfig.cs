namespace PluginLicensing
{
    /// <summary>
    /// ========================================================================
    ///  YAGONA TAHRIRLANADIGAN FAYL.
    ///  Har bir yangi plagin uchun faqat quyidagi qiymatlarni o'zgartiring.
    ///  Qolgan barcha fayllar (LicenseCodec, MachineIdProvider, LicenseManager,
    ///  LicenseGate, ActivationWindow/Form, LicenseCommands) o'zgarmaydi.
    /// ========================================================================
    /// </summary>
    internal static class LicenseConfig
    {
        // (1) Bu plaginning OCHIQ kaliti.
        //     tools/KalitJuftiYaratish.html -> "Public key (.NET XML)" natijasini shu yerga qo'ying.
        //     MAXFIY kalitni ASLO bu yerga qo'ymang!
        public const string PublicKeyXml =
            "<RSAKeyValue><Modulus>BU_YERGA_PUBLIC_KEY_MODULUS</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        // (2) Mahsulot nomi. Litsenziya fayli: %APPDATA%\<ProductName>\license.lic
        //     Shuningdek aktivatsiya oynasi sarlavhasida ishlatiladi.
        public const string ProductName = "MyPlugin";

        // (3) Buyruq nomlari — HAR PLAGINDA NOYOB bo'lsin!
        //     (Aks holda bir nechta plagin birga yuklanганda AutoCAD "duplicate command" beradi.)
        public const string CmdActivate  = "MYPLUGINACTIVATE"; // aktivatsiya oynasini ochadi (lenta tugmasi shuni chaqiradi)
        public const string CmdMachineId = "MYPLUGINID";       // Machine ID (Product key) ni ko'rsatadi
        public const string CmdInstall   = "MYPLUGINLIC";      // .lic faylni tanlab o'rnatadi
    }
}
