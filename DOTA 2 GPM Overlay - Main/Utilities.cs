using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DOTA_2_GPM_Overlay___Main
{
    public static class Utilities
    {
        public static double LinearInterpolation(double x1, double y1, double x2, double y2, double x)
        {
            double returnY = y1 + (x - x1) * ((y2 - y1) / (x2 - x1));
            return returnY;
        }

        public static Rectangle GetPrimaryScreenResolution()
        {
            return Screen.PrimaryScreen.Bounds;
        }
    }
}
