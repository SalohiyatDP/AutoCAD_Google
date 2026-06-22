using System;

namespace GoogleSatelliteCAD.Projection
{
    /// <summary>
    /// Aylanma ellipsoid (referens-ellipsoid) parametrlari.
    /// Geodezik hisob-kitoblarda Yer shaklini ifodalaydi.
    /// </summary>
    public sealed class Ellipsoid
    {
        /// <summary>Katta yarim o'q a (metr).</summary>
        public double A { get; }

        /// <summary>Kichik yarim o'q b (metr).</summary>
        public double B { get; }

        /// <summary>Siqilish (flattening) f = (a-b)/a.</summary>
        public double F { get; }

        /// <summary>Birinchi ekssentrisitet kvadrati e^2 = (a^2-b^2)/a^2.</summary>
        public double E2 { get; }

        /// <summary>Ikkinchi ekssentrisitet kvadrati e'^2 = (a^2-b^2)/b^2.</summary>
        public double Ep2 { get; }

        /// <param name="a">Katta yarim o'q (metr).</param>
        /// <param name="invF">Teskari siqilish 1/f.</param>
        public Ellipsoid(double a, double invF)
        {
            A = a;
            F = 1.0 / invF;
            B = a * (1.0 - F);
            E2 = F * (2.0 - F);
            Ep2 = E2 / (1.0 - E2);
        }

        /// <summary>WGS84 ellipsoidi (GPS, Google, EPSG:4326/3857).</summary>
        public static readonly Ellipsoid WGS84 = new Ellipsoid(6378137.0, 298.257223563);

        /// <summary>Krassovskiy 1940 ellipsoidi (Pulkovo 1942 / SK-42 uchun).</summary>
        public static readonly Ellipsoid Krassovsky1940 = new Ellipsoid(6378245.0, 298.3);
    }

    /// <summary>
    /// Datum (geodezik tayanch) o'rtasidagi 7 parametrli Helmert
    /// (Bursa-Wolf) o'tkazish koeffitsiyentlari.
    /// Bir datumdan ikkinchisiga (masalan, Pulkovo1942 -> WGS84) o'tishda ishlatiladi.
    /// </summary>
    public sealed class DatumShift
    {
        public double Dx { get; }   // metr
        public double Dy { get; }   // metr
        public double Dz { get; }   // metr
        public double Rx { get; }   // radian
        public double Ry { get; }   // radian
        public double Rz { get; }   // radian
        public double Scale { get; } // o'lchovsiz (1 + ppm*1e-6)

        /// <param name="dx">X siljish (metr).</param>
        /// <param name="dy">Y siljish (metr).</param>
        /// <param name="dz">Z siljish (metr).</param>
        /// <param name="rxSec">X aylanish (yoy soniyasida).</param>
        /// <param name="rySec">Y aylanish (yoy soniyasida).</param>
        /// <param name="rzSec">Z aylanish (yoy soniyasida).</param>
        /// <param name="ppm">Masshtab xatosi (million ulushda).</param>
        public DatumShift(double dx, double dy, double dz,
                          double rxSec, double rySec, double rzSec, double ppm)
        {
            Dx = dx;
            Dy = dy;
            Dz = dz;
            const double secToRad = Math.PI / (180.0 * 3600.0);
            Rx = rxSec * secToRad;
            Ry = rySec * secToRad;
            Rz = rzSec * secToRad;
            Scale = 1.0 + ppm * 1e-6;
        }

        /// <summary>
        /// Pulkovo 1942 (SK-42) -> WGS84 standart parametrlari.
        /// (Rossiya/MDH hududi uchun keng tarqalgan qiymatlar.)
        /// </summary>
        public static readonly DatumShift Pulkovo1942ToWgs84 =
            new DatumShift(23.92, -141.27, -80.9, 0.0, 0.35, 0.82, -0.12);
    }
}
