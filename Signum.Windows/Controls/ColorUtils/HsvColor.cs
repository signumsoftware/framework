//
// SupportingClasses.cs 
//
// 
using System;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Ink;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Controls.Primitives;

namespace Signum.Windows.ColorUtils
{
 
    public struct HsvColor
    {
        public double H;
        public double S;
        public double V;
        public HsvColor(double h, double s, double v)
        {
            this.H = h;
            this.S = s;
            this.V = v;
        }

        public static HsvColor FromColor(Color color)
        {
            return FromRGB(color.R, color.G, color.B);
        }

        public static HsvColor FromRGB(int r, int g, int b)
        {
            double h;
            double min = Math.Min(Math.Min(r, g), b);
            double v = Math.Max(Math.Max(r, g), b);
            double delta = v - min;
            double s = (v == 0.0) ? 0.0 : (delta / v);
            if (s == 0.0)
            {
                h = 0.0;
            }
            else
            {
                if (r == v)
                {
                    h = ((double)(g - b)) / delta;
                }
                else if (g == v)
                {
                    h = 2.0 + (((double)(b - r)) / delta);
                }
                else
                {
                    h = 4.0 + (((double)(r - g)) / delta);
                }
                h *= 60.0;
                if (h < 0.0)
                {
                    h += 360.0;
                }
            }
            return new HsvColor(h, s, v / 255.0);
        }

        public Color ToColor()
        {
            return this.ToColor(255);
        }

        public Color ToColor(byte alpha)
        {
            double h = this.H;
            double s = this.S;
            double v = this.V;
            double r,g,b;
            if (s == 0.0)
            {
                r = v; g = v; b = v;
            }
            else
            {
                if (h == 360.0)
                {
                    h = 0.0;
                }
                else
                {
                    h /= 60.0;
                }
                int i = (int)Math.Truncate(h);
                double f = h - i;
                double p = v * (1.0 - s);
                double q = v * (1.0 - (s * f));
                double t = v * (1.0 - (s * (1.0 - f)));
                switch (i)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    default: r = v; g = p; b = q; break;
                }
            }

            return Color.FromArgb(alpha, (byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0));
        }
    }

 





}