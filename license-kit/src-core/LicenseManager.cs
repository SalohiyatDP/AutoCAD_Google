using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PluginLicensing
{
    /// <summary>Litsenziya tekshiruvi natijasi.</summary>
    public sealed class LicenseResult
    {
        public bool IsValid { get; }
        public string Message { get; }
        public DateTime? ExpiryUtc { get; }

        public LicenseResult(bool isValid, string message, DateTime? expiryUtc = null)
        {
            IsValid = isValid;
            Message = message;
            ExpiryUtc = expiryUtc;
        }
    }

    /// <summary>
    /// Litsenziyani tekshiradi va o'rnatadi.
    ///   license = Base32( GZip( UTF8("ProcessorId|expiryTicks") + 256-baytli RSA imzo ) )
    /// Bu yerda FAQAT OCHIQ kalit (LicenseConfig.PublicKeyXml) ishlatiladi —
    /// imzolash sizdagi MAXFIY kalit bilan (vendor vositasi orqali) qilinadi.
    /// </summary>
    public static class LicenseManager
    {
        /// <summary>Litsenziya fayli: %APPDATA%\&lt;ProductName&gt;\license.lic</summary>
        public static string LicensePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    LicenseConfig.ProductName);
                return Path.Combine(dir, "license.lic");
            }
        }

        /// <summary>Diskdagi o'rnatilgan litsenziyani o'qib tekshiradi.</summary>
        public static LicenseResult CheckInstalled()
        {
            try
            {
                if (!File.Exists(LicensePath))
                    return new LicenseResult(false, "Litsenziya o'rnatilmagan.");
                return Validate(File.ReadAllText(LicensePath));
            }
            catch (Exception ex)
            {
                return new LicenseResult(false, "Litsenziyani o'qishda xato: " + ex.Message);
            }
        }

        /// <summary>Litsenziya matnini tekshiradi va yaroqli bo'lsa faylga saqlaydi.</summary>
        public static LicenseResult Install(string licenseStr)
        {
            LicenseResult result = Validate(licenseStr);
            if (result.IsValid)
            {
                try
                {
                    string dir = Path.GetDirectoryName(LicensePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(LicensePath, (licenseStr ?? string.Empty).Trim());
                }
                catch (Exception ex)
                {
                    return new LicenseResult(false, "Litsenziyani saqlashda xato: " + ex.Message);
                }
            }
            return result;
        }

        /// <summary>Imzo + kompyuter + muddat bo'yicha to'liq tekshiradi.</summary>
        public static LicenseResult Validate(string licenseStr)
        {
            if (string.IsNullOrWhiteSpace(licenseStr))
                return new LicenseResult(false, "Litsenziya bo'sh.");

            try
            {
                byte[] all = LicenseCodec.Decompress(LicenseCodec.Base32Decode(licenseStr.Trim()));
                if (all.Length <= 256)
                    return new LicenseResult(false, "Litsenziya formati noto'g'ri.");

                byte[] payload = new byte[all.Length - 256];
                byte[] signature = new byte[256];
                Buffer.BlockCopy(all, 0, payload, 0, payload.Length);
                Buffer.BlockCopy(all, payload.Length, signature, 0, 256);

                string text = Encoding.UTF8.GetString(payload);
                string[] parts = text.Split('|');
                if (parts.Length != 2)
                    return new LicenseResult(false, "Litsenziya ma'lumoti noto'g'ri.");

                // Kompyuterга bog'lanish: payload ichidagi ProcessorId dan Machine ID qayta hisoblanadi.
                string recomputedId = LicenseCodec.Base32Encode(
                    LicenseCodec.Compress(Encoding.UTF8.GetBytes(parts[0])));
                if (!string.Equals(recomputedId, MachineIdProvider.Get(), StringComparison.Ordinal))
                    return new LicenseResult(false, "Litsenziya bu kompyuter uchun emas.");

                if (!long.TryParse(parts[1], out long ticks))
                    return new LicenseResult(false, "Litsenziya muddati noto'g'ri.");

                var expiryUtc = new DateTime(ticks, DateTimeKind.Utc);
                if (DateTime.UtcNow >= expiryUtc)
                    return new LicenseResult(false, "Litsenziya muddati tugagan (" + expiryUtc.ToLocalTime() + ").", expiryUtc);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(LicenseConfig.PublicKeyXml);
                    bool ok = rsa.VerifyData(Encoding.UTF8.GetBytes(text), CryptoConfig.MapNameToOID("SHA256"), signature);
                    if (!ok)
                        return new LicenseResult(false, "Imzo yaroqsiz — litsenziya soxta yoki o'zgartirilgan.");
                }

                return new LicenseResult(true, "Litsenziya yaroqli. Muddat: " + expiryUtc.ToLocalTime(), expiryUtc);
            }
            catch (Exception ex)
            {
                return new LicenseResult(false, "Tekshirishda xato: " + ex.Message);
            }
        }
    }
}
