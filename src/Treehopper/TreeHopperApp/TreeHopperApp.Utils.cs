using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeHopperViewer.Utils
{
    //Class to hold utility functions like color, etc.
    public class AppUtils
    {
        // Get the color for a rectangle based on the index and total number of rectangles
        public static Color GetRectangleColor(int index, int totalRectangles, bool rainbowEnabled)
        {
            if (rainbowEnabled) // If rainbow is enabled, use the rainbow color scheme
            {
                // Calculate the hue value based on the index and total number of rectangles
                float hue = (120f / totalRectangles) * index + 240;
                // Calculate the brightness value based on the index
                float brightness = 0.5f + (float)index / (totalRectangles - 1) * 0.5f;
                return ColorFromAhsb(255, hue, 1, brightness);
            }
            else
            {
                // Default pink color
                return Color.DeepPink;
            }
        }
        //Converts HSB to RGB
        public static Color ColorFromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            // Calculate the RGB values from HSB (Hue, Saturation, Brightness)
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (int)(hue / 60);
            // Convert the brightness value to a 0-255 value
            brightness *= 255;
            int v = (int)brightness;
            int p = (int)(brightness * (1 - saturation));
            int q = (int)(brightness * (1 - f * saturation));
            int t = (int)(brightness * (1 - (1 - f) * saturation));
            // Return the color based on the hue value
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
