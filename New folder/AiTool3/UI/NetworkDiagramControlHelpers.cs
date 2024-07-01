using System.Drawing.Drawing2D;

internal static class NetworkDiagramControlHelpers
{

    public static GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = radius * 2;
        Rectangle arc = new Rectangle(x, y, diameter, diameter);

        path.AddArc(arc, 180, 90);
        arc.X = x + width - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = y + height - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = x;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}