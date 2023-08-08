using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeHopperViewer.Utils
{
    public class AppUtils
    {

        public static Color GetRectangleColor(int index, int totalRectangles, bool rainbowEnabled)
        {
            if (rainbowEnabled)
            {
                // Calculate the hue value based on the index and total number of rectangles
                float hue = (120f / totalRectangles) * index + 120;

                // Calculate the brightness value based on the index
                float brightness = 0.5f + (float)index / (totalRectangles - 1) * 0.5f;

                return ColorFromAhsb(255, hue, 1, brightness);
            }
            else
            {
                // Default pink color (you can change this to any other color you prefer)
                return Color.DeepPink;
            }
        }

        public static Color ColorFromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            // Calculate the RGB values from HSB (Hue, Saturation, Brightness)
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (int)(hue / 60);

            brightness *= 255;
            int v = (int)brightness;
            int p = (int)(brightness * (1 - saturation));
            int q = (int)(brightness * (1 - f * saturation));
            int t = (int)(brightness * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(alpha, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(alpha, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(alpha, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(alpha, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(alpha, t, p, v);
            else
                return Color.FromArgb(alpha, v, p, q);
        }
    }
}
