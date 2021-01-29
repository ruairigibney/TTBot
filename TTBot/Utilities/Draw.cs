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
	}
}
