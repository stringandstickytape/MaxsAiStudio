public class TemplateMenuItem : ToolStripMenuItem
{
    public event EventHandler? EditClicked;

    private const int ButtonWidth = 16;
    private bool _isMouseOverButton;

    public TemplateMenuItem(string text) : base(text)
    {
        TextAlign = ContentAlignment.MiddleLeft;
        AutoSize = false;
        Height = 32;
        Width = 400;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Draw the background
        base.OnPaint(e);

        // Calculate text rectangle
        Rectangle textRect = new Rectangle(48, 0, Width - ButtonWidth - 10, Height);

        // Draw the edit button
        Rectangle buttonRect = new Rectangle(0, 0, 32, 32);

        using (Bitmap bmp2 = new Bitmap(32, 32))
        using (Graphics g = Graphics.FromImage(bmp2))
        {
            // Change background color to pink when mouse is over the button
            g.Clear(_isMouseOverButton ? Color.GreenYellow : Color.LightGray);

            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                g.DrawString("Edit", new Font("Segoe UI", 8), Brushes.Black, 0, 4);
            }
            e.Graphics.DrawImage(bmp2, buttonRect);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool wasOverButton = _isMouseOverButton;
        _isMouseOverButton = e.X < 48;

        if (wasOverButton != _isMouseOverButton)
        {
            Invalidate();
        }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (_isMouseOverButton)
        {
            EditClicked?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            base.OnMouseDown(e);
        }
    }



    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_isMouseOverButton)
        {
            _isMouseOverButton = false;
            Invalidate();
        }
    }

}