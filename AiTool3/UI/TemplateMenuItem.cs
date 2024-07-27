public class TemplateMenuItem : ToolStripMenuItem
{
    public event EventHandler? EditClicked;

    private const int ButtonWidth = 16;
    private bool _isMouseOverButton;
    public bool IsSelected { get; set; }

    public TemplateMenuItem(string text, ref ToolStripMenuItem dropDownItems) : base(text)
    {
        TextAlign = ContentAlignment.MiddleLeft;
        AutoSize = false;
        Height = 32;
        ToolTipText = "CTRL-click to use System Prompt with current conversation;\nSHIFT-click to delete";

        // get the owner's font settings
        var parentFont = dropDownItems.Owner.Font;

        // measure "text" using that font
        var size = TextRenderer.MeasureText(text, parentFont);

        Width = size.Width+48;

        dropDownItems.DropDownItems.Add(this);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Draw the background
        

        // Calculate text rectangle
        Rectangle textRect = new Rectangle(48, 0, Width-48, Height);

        // check if any child objects are IsSelecetd
        var highlightAsParent = false;
        foreach (var item in DropDownItems)
        {
            if (item is TemplateMenuItem tmi && tmi.IsSelected)
            {
                highlightAsParent = true;
                break;
            }
        }

        // paint the textRect blue
        if (IsSelected || highlightAsParent)
        {
            e.Graphics.FillRectangle(Brushes.LightBlue, textRect);

            // draw red dashed border round the outside of textRect
            using (Pen pen = new Pen(Color.Red, 2))
            {
                // shrink textRect by 1 pixel to avoid clipping the border, and two on the right

                textRect.Inflate(-1, -1);

                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(pen, textRect);
            }

        }

        base.OnPaint(e);


        // Draw the edit button
        Rectangle buttonRect = new Rectangle(0, 0, 32, 32);
        
        using (Bitmap bmp2 = new Bitmap(32, 32))
        using (Graphics g = Graphics.FromImage(bmp2))
        {
            // Change background color to gryel when mouse is over the button
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