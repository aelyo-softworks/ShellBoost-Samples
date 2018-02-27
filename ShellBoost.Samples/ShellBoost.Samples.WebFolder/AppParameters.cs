using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShellBoost.Samples.WebFolder
{
    public class AppParameters
    {
        private static Lazy<Thickness> _windowCaptionHeight = new Lazy<Thickness>(() =>
        {
            // https://stackoverflow.com/questions/28524463/how-to-get-the-default-caption-bar-height-of-a-window-in-windows
            // https://connect.microsoft.com/VisualStudio/feedback/details/763767/the-systemparameters-windowresizeborderthickness-seems-to-return-incorrect-value
            int addedBorder = GetSystemMetrics(SM_CXPADDEDBORDER);
            var tn = SystemParameters.WindowNonClientFrameThickness;
            return new Thickness(tn.Left, tn.Top + addedBorder, tn.Right, tn.Bottom);
        });

        private static Lazy<Thickness> _glassFrameThickness = new Lazy<Thickness>(() =>
        {
            int addedBorder = GetSystemMetrics(SM_CXPADDEDBORDER);
            var tn = SystemParameters.WindowNonClientFrameThickness;
            return new Thickness(2, tn.Top + addedBorder, 2, 2);
        });

        private static Lazy<Thickness> _titleMargin = new Lazy<Thickness>(() =>
        {
            var height = _windowCaptionHeight.Value.Top;
            return new Thickness(8, (height - TitleSize) / 2, 0, 0);
        });

        private static Lazy<ImageSource> _iconSource = new Lazy<ImageSource>(() =>
        {
            using (var stream = Application.GetResourceStream(new Uri("/" + typeof(App).Namespace + ".ico", UriKind.Relative)).Stream)
            {
                using (var icon = new Icon(stream, new System.Drawing.Size(256, 256)))
                {
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
                }
            }
        });

        private static Lazy<Icon> _icon = new Lazy<Icon>(() =>
        {
            using (var stream = Application.GetResourceStream(new Uri("/" + typeof(App).Namespace + ".ico", UriKind.Relative)).Stream)
            {
                return new Icon(stream, new System.Drawing.Size(256, 256));
            }
        });

        public static Thickness WindowCaptionHeight => _windowCaptionHeight.Value;
        public static Thickness GlassFrameThickness => _glassFrameThickness.Value;
        public static Thickness TitleMargin => _titleMargin.Value;
        public static double TitleSize => 12;
        public static ImageSource IconSource => _iconSource.Value;
        public static Icon Icon => _icon.Value;

        private const int SM_CXPADDEDBORDER = 0x5C;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}
