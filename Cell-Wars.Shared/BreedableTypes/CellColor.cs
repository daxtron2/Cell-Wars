using System;
using System.Collections.Generic;
using System.Text;
using Ultraviolet;

namespace CellWars.BreedableTypes
{
    public class CellColor : BreedableType
    {
        //double properties for hue, saturation, and value
        private double _hue, _saturation, _value;
        public double Hue { get { return ValidateHue(_hue); } set { _hue = value; } }
        public double Saturation { get { return _saturation; } set { _saturation = value; } }
        public double Value { get { return _value; } set { _value = value; } }

        public Color Color { get { return CellColorExtensions.ColorFromHSV(Hue, Saturation, Value); } }

        public CellColor(double hue, double saturation = 1.0, double value = 1.0)
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        public CellColor(Color color)
        {
            this.HSVFromColor(color);
        }

        //calculate the true distance between the two hsv values
        public static double Distance(CellColor hsv1, CellColor hsv2)
        {
            return 180d - Math.Abs((Math.Abs(hsv1.Hue - hsv2.Hue) % (2 * 180d)) - 180d);
        }


        public static CellColor operator -(CellColor hsv1, CellColor hsv2)
        {
            CellColor negative = new CellColor(-hsv2.Hue, -hsv2.Saturation, -hsv2.Value);
            return hsv1 + negative;
        }
        
        public static CellColor operator +(CellColor hsv1, CellColor hsv2)
        {
            double saturation = hsv1.Saturation + hsv2.Saturation;
            double value = hsv1.Value + hsv2.Value;

            //upper limit checks

            if (saturation > 1)
                saturation = 1;
            if (value > 1)
                value = 1;

            //lower limit checks
            if (saturation < 0)
                saturation = 0;
            if (value < 0)
                value = 0;

            return new CellColor(AddHues(hsv1, hsv2), saturation, value);
        }

        private static double AddHues(CellColor c1, CellColor c2)
        {
            double hue = c1.Hue + c2.Hue;
            
            return ValidateHue(hue);
        }

        private static double ValidateHue(double hue)
        {
            if (hue > 360)
                hue %= 360;
            if (hue < 0)
            {
                hue %= 360;
                hue += 360;
            }
            return hue;
        }

        internal void ShiftTowards(CellColor cellColor)
        {
            if (cellColor.Hue > this.Hue)
            {
                this.Hue += 0.001;
            }
            else
            {
                this.Hue -= 0.001;
            }
        }
    }

    public static class CellColorExtensions
    {
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb((uint)((((((255 << 8) + v) << 8) + t) << 8) + p));
            else if (hi == 1)
                return Color.FromArgb((uint)((((((255 << 8) + q) << 8) + v) << 8) + p));
            else if (hi == 2)
                return Color.FromArgb((uint)((((((255 << 8) + p) << 8) + v) << 8) + t));
            else if (hi == 3)
                return Color.FromArgb((uint)((((((255 << 8) + p) << 8) + q) << 8) + v));
            else if (hi == 4)
                return Color.FromArgb((uint)((((((255 << 8) + t) << 8) + p) << 8) + v));
            else
                return Color.FromArgb((uint)((((((255 << 8) + v) << 8) + p) << 8) + q));
        }

        public static void HSVFromColor(this CellColor cell, Color color)
        {            
            System.Drawing.Color tempSys = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            cell.Hue = tempSys.GetHue();
            cell.Saturation = tempSys.GetSaturation();
            cell.Value = tempSys.GetBrightness();
        }
    }
}
