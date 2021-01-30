using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace TTBot.Utilities
{
    public static class Draw
    {
		public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
		{
			Rectangle corner = new Rectangle(x, y, radius, radius);
			GraphicsPath path = new GraphicsPath();
			path.AddArc(corner, 180, 90);
			corner.X = x + width - radius;
			path.AddArc(corner, 270, 90);
			corner.Y = y + height - radius;
			path.AddArc(corner, 0, 90);
			corner.X = x;
			path.AddArc(corner, 90, 90);
			path.CloseFigure();

			g.FillPath(brush, path);
		}

        public static Font GetAdjustedFont(this Graphics graphic, string str, Font originalFont, Size containerSize)
        {
            for (int adjustedSize = (int)originalFont.Size; adjustedSize >= 1; adjustedSize--)
            {
                var testFont = new Font(originalFont.Name, adjustedSize, originalFont.Style);

                var adjustedSizeNew = graphic.MeasureString(str, testFont, containerSize.Width);

                if (containerSize.Height > Convert.ToInt32(adjustedSizeNew.Height)
                    && containerSize.Width > Convert.ToInt32(adjustedSizeNew.Width))
                {
                    return testFont;
                }
            }

            return new Font(originalFont.Name, 1, originalFont.Style);
        }
    }
}
