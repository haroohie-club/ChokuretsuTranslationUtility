using SkiaSharp;
using System;

namespace HaruhiChokuretsuLib.Util
{
    public class CIEXYZ
    {
        /// <summary>
        /// Gets an empty CIEXYZ structure.
        /// </summary>
        public static readonly CIEXYZ Empty = new();
        /// <summary>
        /// Gets the CIE D65 (white) structure.
        /// </summary>
        public static readonly CIEXYZ D65 = new(0.9505, 1.0, 1.0890);


        private double _x;
        private double _y;
        private double _z;

        public static bool operator ==(CIEXYZ item1, CIEXYZ item2)
        {
            return (
                item1.X == item2.X
                && item1.Y == item2.Y
                && item1.Z == item2.Z
                );
        }

        public static bool operator !=(CIEXYZ item1, CIEXYZ item2)
        {
            return (
                item1.X != item2.X
                || item1.Y != item2.Y
                || item1.Z != item2.Z
                );
        }

        /// <summary>
        /// Gets or sets X component.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = (value > 0.9505) ? 0.9505 : ((value < 0) ? 0 : value);
            }
        }

        /// <summary>
        /// Gets or sets Y component.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = (value > 1.0) ? 1.0 : ((value < 0) ? 0 : value);
            }
        }

        /// <summary>
        /// Gets or sets Z component.
        /// </summary>
        public double Z
        {
            get
            {
                return _z;
            }
            set
            {
                _z = (value > 1.089) ? 1.089 : ((value < 0) ? 0 : value);
            }
        }

        public CIEXYZ(double x, double y, double z)
        {
            _x = (x > 0.9505) ? 0.9505 : ((x < 0) ? 0 : x);
            _y = (y > 1.0) ? 1.0 : ((y < 0) ? 0 : y);
            _z = (z > 1.089) ? 1.089 : ((z < 0) ? 0 : z);
        }

        public CIEXYZ()
        {
            _x = 0;
            _y = 0;
            _z = 0;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return (this == (CIEXYZ)obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
    }

    public class CIELab
    {
        /// <summary>
        /// Gets an empty CIELab structure.
        /// </summary>
        public static readonly CIELab Empty = new();

        public static bool operator ==(CIELab item1, CIELab item2)
        {
            return (
                item1.L == item2.L
                && item1.A == item2.A
                && item1.B == item2.B
                );
        }

        public static bool operator !=(CIELab item1, CIELab item2)
        {
            return (
                item1.L != item2.L
                || item1.A != item2.A
                || item1.B != item2.B
                );
        }


        /// <summary>
        /// Gets or sets L component.
        /// </summary>
        public double L { get; set; }

        /// <summary>
        /// Gets or sets a component.
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Gets or sets a component.
        /// </summary>
        public double B { get; set; }

        public CIELab(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }

        public CIELab()
        {
            L = 0;
            A = 0;
            B = 0;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return (this == (CIELab)obj);
        }

        public override int GetHashCode()
        {
            return L.GetHashCode() ^ A.GetHashCode() ^ B.GetHashCode();
        }
    }

    public static class ColorConvesion
    {
        public static CIELab ToLab(this SKColor rgb)
        {
            return RGBtoLab(rgb.Red, rgb.Green, rgb.Blue);
        }

        private static CIELab RGBtoLab(byte red, byte green, byte blue)
        {
            return XYZtoLab(RGBtoXYZ(red, green, blue));
        }

        private static CIEXYZ RGBtoXYZ(byte red, byte green, byte blue)
        {
            // normalize red, green, blue values
            double rLinear = red / 255.0;
            double gLinear = green / 255.0;
            double bLinear = blue / 255.0;

            // convert to a sRGB form
            double r = (rLinear > 0.04045) ? Math.Pow((rLinear + 0.055) / (
                1 + 0.055), 2.2) : (rLinear / 12.92);
            double g = (gLinear > 0.04045) ? Math.Pow((gLinear + 0.055) / (
                1 + 0.055), 2.2) : (gLinear / 12.92);
            double b = (bLinear > 0.04045) ? Math.Pow((bLinear + 0.055) / (
                1 + 0.055), 2.2) : (bLinear / 12.92);

            // converts
            return new CIEXYZ(
                (r * 0.4124 + g * 0.3576 + b * 0.1805),
                (r * 0.2126 + g * 0.7152 + b * 0.0722),
                (r * 0.0193 + g * 0.1192 + b * 0.9505)
                );
        }
        
        public static CIELab XYZtoLab(this CIEXYZ xyz)
        {
            return XYZtoLab(xyz.X, xyz.Y, xyz.Z);
        }

        private static CIELab XYZtoLab(double x, double y, double z)
        {
            CIELab lab = new()
            {
                L = 116.0 * Fxyz(y / CIEXYZ.D65.Y) - 16,
                A = 500.0 * (Fxyz(x / CIEXYZ.D65.X) - Fxyz(y / CIEXYZ.D65.Y)),
                B = 200.0 * (Fxyz(y / CIEXYZ.D65.Y) - Fxyz(z / CIEXYZ.D65.Z))
            };

            return lab;
        }

        private static double Fxyz(double t)
        {
            return ((t > 0.008856) ? Math.Pow(t, (1.0 / 3.0)) : (7.787 * t + 16.0 / 116.0));
        }

        public static SKColor LabtoRGB(CIELab lab)
        {
            return LabtoRGB(lab.L, lab.A, lab.B);
        }

        public static SKColor LabtoRGB(double l, double a, double b)
        {
            return XYZtoRGB(LabtoXYZ(l, a, b));
        }
        private static CIEXYZ LabtoXYZ(double l, double a, double b)
        {
            double delta = 6.0 / 29.0;

            double fy = (l + 16) / 116.0;
            double fx = fy + (a / 500.0);
            double fz = fy - (b / 200.0);

            return new CIEXYZ(
                (fx > delta) ? CIEXYZ.D65.X * (fx * fx * fx) : (fx - 16.0 / 116.0) * 3 * (
                    delta * delta) * CIEXYZ.D65.X,
                (fy > delta) ? CIEXYZ.D65.Y * (fy * fy * fy) : (fy - 16.0 / 116.0) * 3 * (
                    delta * delta) * CIEXYZ.D65.Y,
                (fz > delta) ? CIEXYZ.D65.Z * (fz * fz * fz) : (fz - 16.0 / 116.0) * 3 * (
                    delta * delta) * CIEXYZ.D65.Z
                );
        }

        public static SKColor XYZtoRGB(this CIEXYZ xyz)
        {
            return XYZtoRGB(xyz.Z, xyz.Y, xyz.Z);
        }

        public static SKColor XYZtoRGB(double x, double y, double z)
        {
            double[] Clinear = new double[3];
            Clinear[0] = x * 3.2410 - y * 1.5374 - z * 0.4986; // red
            Clinear[1] = -x * 0.9692 + y * 1.8760 - z * 0.0416; // green
            Clinear[2] = x * 0.0556 - y * 0.2040 + z * 1.0570; // blue

            for (int i = 0; i < 3; i++)
            {
                Clinear[i] = (Clinear[i] <= 0.0031308) ? 12.92 * Clinear[i] : (
                    1 + 0.055) * Math.Pow(Clinear[i], (1.0 / 2.4)) - 0.055;
            }

            return new SKColor(
                Convert.ToByte(double.Parse(string.Format("{0:0.00}",
                    Clinear[0] * 255.0))),
                Convert.ToByte(double.Parse(string.Format("{0:0.00}",
                    Clinear[1] * 255.0))),
                Convert.ToByte(double.Parse(string.Format("{0:0.00}",
                    Clinear[2] * 255.0)))
                );
        }
    }
}
