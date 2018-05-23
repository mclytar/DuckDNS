using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DuckDNS
{
    class Windows
    {
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner,
           [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        const int CSIDL_STARTUP = 0x7;

        public static string GetStartupPath()
        {
            StringBuilder path = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, path, CSIDL_STARTUP, false);
            return path.ToString();
        }

        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        public static Image GetUACIcon()
        {
            var size = SystemInformation.SmallIconSize;
            var image = LoadImage(IntPtr.Zero, "#106", 1, size.Width, size.Height, 0);

            if (image == IntPtr.Zero)
            {
                return null;
            }

            using (var icon = Icon.FromHandle(image))
            {
                var bitmap = new Bitmap(size.Width, size.Height);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawIcon(icon, new Rectangle(0, 0, size.Width, size.Height));
                }

                return bitmap;
            }
        }
    }
}
