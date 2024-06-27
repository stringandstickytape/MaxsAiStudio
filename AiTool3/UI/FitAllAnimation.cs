namespace AiTool3.UI
{
    public class FitAllAnimation
    {
        private NetworkDiagramControl control;
        private System.Windows.Forms.Timer fitAllTimer;
        private float targetZoomFactor;
        private Point targetPanOffset;
        private float initialZoomFactor;
        private Point initialPanOffset;
        private const int AnimationDuration = 250;
        private int animationProgress;

        public FitAllAnimation(NetworkDiagramControl control)
        {
            this.control = control;
            fitAllTimer = new System.Windows.Forms.Timer();
            fitAllTimer.Interval = 16;
            fitAllTimer.Tick += FitAllTimerTick;
        }

        public void Start(List<NetworkDiagramControl.Node> nodes, float currentZoomFactor, Point currentPanOffset)
        {
            if (nodes.Count == 0)
            {
                return;
            }

            CalculateTargetValues(nodes);

            initialZoomFactor = currentZoomFactor;
            initialPanOffset = currentPanOffset;
            animationProgress = 0;

            fitAllTimer.Start();
        }

        private void CalculateTargetValues(List<NetworkDiagramControl.Node> nodes)
        {
            // Calculate bounding rectangle
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            foreach (var node in nodes)
            {
                if (node.Bounds.Left < left) left = node.Bounds.Left;
                if (node.Bounds.Top < top) top = node.Bounds.Top;
                if (node.Bounds.Right > right) right = node.Bounds.Right;
                if (node.Bounds.Bottom > bottom) bottom = node.Bounds.Bottom;
            }
            Rectangle boundingRect = new Rectangle(left - 5, top - 5, right - left + 10, bottom - top + 10);

            // Calculate new zoom factor
            float horizontalZoom = (float)control.Width / boundingRect.Width;
            float verticalZoom = (float)control.Height / boundingRect.Height;
            targetZoomFactor = Math.Min(horizontalZoom, verticalZoom);
            targetZoomFactor = Math.Min(NetworkDiagramControl.MaxZoom, Math.Max(NetworkDiagramControl.MinZoom, targetZoomFactor));

            // Calculate new pan offset
            float centeredOffsetX = (control.Width - boundingRect.Width * targetZoomFactor) / 2f - boundingRect.Left * targetZoomFactor;
            float centeredOffsetY = (control.Height - boundingRect.Height * targetZoomFactor) / 2f - boundingRect.Top * targetZoomFactor;
            targetPanOffset = new Point((int)centeredOffsetX, (int)centeredOffsetY);
        }

        private void FitAllTimerTick(object sender, EventArgs e)
        {
            animationProgress += fitAllTimer.Interval;
            float percentageComplete = Math.Min(1, (float)animationProgress / AnimationDuration);

            control.ZoomFactor = Lerp(initialZoomFactor, targetZoomFactor, percentageComplete);
            control.PanOffset = Lerp(initialPanOffset, targetPanOffset, percentageComplete);

            control.Invalidate();

            if (percentageComplete >= 1)
            {
                fitAllTimer.Stop();
            }
        }

        private float Lerp(float start, float end, float percentage)
        {
            return start + (end - start) * percentage;
        }

        private Point Lerp(Point start, Point end, float percentage)
        {
            return new Point(
                (int)(start.X + (end.X - start.X) * percentage),
                (int)(start.Y + (end.Y - start.Y) * percentage)
            );
        }
    }
}