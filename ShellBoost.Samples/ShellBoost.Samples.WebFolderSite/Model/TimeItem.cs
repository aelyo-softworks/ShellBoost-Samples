using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ShellBoost.Samples.WebFolderSite.Model
{
    // for demonstration purposes, this item is fully dynamic
    // it's an image that contains the current time
    public class TimeItem : Item
    {
        public static readonly Guid TimeItemId = new Guid("3855336a-a8e4-4e57-999e-7211fed043ac"); // an arbitrary id

        public TimeItem()
        {
            Id = TimeItemId;
            Name = "Time is " + DateTime.Now.TimeOfDay + ".png";
            Attributes = FileAttributes.ReadOnly;
            ContentETag = Guid.NewGuid().ToString("N");
        }

        public override string ContentETag { get; }

        public override Stream ContentOpenRead()
        {
            var ms = new MemoryStream();
            using (var bmp = new Bitmap(400, 400))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.DrawString("Time is " + DateTime.Now.TimeOfDay, new Font("Arial", 20), Brushes.Black, 10, 10);
                    g.Flush();
                }
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
            }
            return ms;
        }
    }
}