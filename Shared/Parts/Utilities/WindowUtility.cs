using Formula81.XrmToolBox.Shared.Parts.Components;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Formula81.XrmToolBox.Shared.Parts.Utilities
{
    public class WindowUtility
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public static Point CalculateCenter(double width, double height, Rect windowBounds)
        {
            var windowWidth = windowBounds.Right - windowBounds.Left;
            var windowHeight = windowBounds.Bottom - windowBounds.Top;
            var x = windowBounds.Left + ((windowWidth / 2) - (width / 2));
            var y = windowBounds.Top + ((windowHeight / 2) - (height / 2));
            return new Point((int)x, (int)y);
        }
    }
}
