using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GoogleSatelliteCAD.Licensing
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
    /// Litsenziyani tekshiradi va o'rnatadi. Bu yerda FAQAT OCHIQ kalit bo'ladi —
    /// litsenziyani imzolash faqat sizdagi MAXFIY kalit bilan mumkin.
    ///
    /// Format:  base64url(payload) + "." + base64url(signature)
    ///   payload   = UTF8( "machineId|expiryTicksUtc" )
    ///   signature = RSA-2048 / SHA-256 / PKCS#1 v1.5
    /// </summary>
    public static class LicenseManager
    {
        // Bu loyiha (GoogleSatelliteCAD) uchun OCHIQ kalit.
        private const string PublicKeyXml =
            "<RSAKeyValue><Modulus>4T3htOX/p5xS4tgbTVRXQ1JzBaquvR7wGl68TPsz628SrDXUQbbXK7PHqdPZNPZx1n7cj1hrcAtfk4nOWt2twb7ZVmH0QqIW0i5HXm7k9DN7jJDCQySSKpc6dHkmEYC+Cp1U/nevwLsX64EKt4qnawWOUOsdKREo7ffstHdbPIBEeKwQHktuYwev43xFTISaMcvk4YJYLzDg6RGXZnGmrFHFneOhBe2CXSJvGOVJUqpFCCmJqkbzWzXcpDA8mQnVRbVTSy+Kz/YBbWjke1VOb50NPbvHVwFFhhTlYWaAaPvGVe8s/o1KF0WQrRsTJGLAXPCHff9PrxH/JRH9q4khqw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        /// <summary>Litsenziya fayli: %APPDATA%\GoogleSatelliteCAD\license.lic</summary>
        public static string LicensePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GoogleSatelliteCAD");
                return Path.Combine(dir, "license.lic");
            }
        }

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

        public static LicenseResult Install(string licenseStr)
        {
            LicenseResult result = Validate(licenseStr);
            if (result.IsValid)
            {
                try
                {
                    string dir = Path.GetDirectoryName(LicensePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(LicensePath, licenseStr.Trim());
                }
                catch (Exception ex)
                {
                    return new LicenseResult(false, "Litsenziyani saqlashda xato: " + ex.Message);
                }
            }
            return result;
        }

        public static LicenseResult Validate(string licenseStr)
        {
            if (string.IsNullOrWhiteSpace(licenseStr))
                return new LicenseResult(false, "Litsenziya bo'sh.");

            try
            {
                string[] parts = licenseStr.Trim().Split('.');
                if (parts.Length != 2)
                    return new LicenseResult(false, "Litsenziya formati noto'g'ri.");

                byte[] payload = FromBase64Url(parts[0]);
                byte[] signature = FromBase64Url(parts[1]);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(PublicKeyXml);
                    if (!rsa.VerifyData(payload, CryptoConfig.MapNameToOID("SHA256"), signature))
                        return new LicenseResult(false, "Imzo yaroqsiz — litsenziya soxta yoki o'zgartirilgan.");
                }

                string text = Encoding.UTF8.GetString(payload);
                string[] fields = text.Split('|');
                if (fields.Length != 2)
                    return new LicenseResult(false, "Litsenziya ma'lumoti noto'g'ri.");

                if (!string.Equals(fields[0], MachineIdProvider.Get(), StringComparison.Ordinal))
                    return new LicenseResult(false, "Litsenziya bu kompyuter uchun emas.");

                if (!long.TryParse(fields[1], out long ticks))
                    return new LicenseResult(false, "Litsenziya muddati noto'g'ri.");

                var expiryUtc = new DateTime(ticks, DateTimeKind.Utc);
                if (DateTime.UtcNow >= expiryUtc)
                    return new LicenseResult(false, "Litsenziya muddati tugagan (" + expiryUtc.ToLocalTime() + ").", expiryUtc);

                return new LicenseResult(true, "Litsenziya yaroqli. Muddat: " + expiryUtc.ToLocalTime(), expiryUtc);
            }
            catch (Exception ex)
            {
                return new LicenseResult(false, "Tekshirishda xato: " + ex.Message);
            }
        }

        private static byte[] FromBase64Url(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
