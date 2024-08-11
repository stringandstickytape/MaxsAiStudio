namespace AiTool3.Helpers
{
    internal static class SplitContainerHelper
    {
        internal static void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            SplitContainer sc = (sender as SplitContainer)!;

            Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                e.Graphics.FillRectangle(brush, splitterRect);
            }
        }
    }
}
