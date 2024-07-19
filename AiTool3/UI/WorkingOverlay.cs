using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace AiTool3.UI
{
    public class WorkingOverlay : Control
    {
        private static WebView2 _sharedWebView;
        private static int _instanceCount = 0;
        private bool _isWorking = false;

        public WorkingOverlay()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.FromArgb(128, Color.White);
            _instanceCount++;
        }

        ~WorkingOverlay()
        {
            _instanceCount--;
            if (_instanceCount == 0 && _sharedWebView != null)
            {
                _sharedWebView.Dispose();
                _sharedWebView = null;
            }
        }

        public bool IsWorking
        {
            get => _isWorking;
            set
            {
                if (_isWorking != value)
                {
                    _isWorking = value;
                    UpdateWebView();
                    Invalidate();
                }
            }
        }

        private async void UpdateWebView()
        {
            if (_isWorking)
            {
                if (_sharedWebView == null)
                {
                    _sharedWebView = new WebView2();
                    await _sharedWebView.EnsureCoreWebView2Async();
                }
                if (!Controls.Contains(_sharedWebView))
                {
                    Controls.Add(_sharedWebView);
                }
                ResizeWebView();
                InjectHtmlAndJs();
            }
            else
            {
                if (Controls.Contains(_sharedWebView))
                {
                    Controls.Remove(_sharedWebView);
                }
            }
        }

        private void ResizeWebView()
        {
            if (_sharedWebView != null)
            {
                _sharedWebView.Width = Width;
                _sharedWebView.Height = Height;
                _sharedWebView.Left = 0;
                _sharedWebView.Top = 0;
            }
        }

        private void InjectHtmlAndJs()
        {
            string html = @"<html>
<head>
    <style>
        body { 
            margin: 0; 
            overflow: hidden;
            background-color: transparent;
        }
        canvas {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
        }
    </style>
</head>
<body>
    <canvas id=""animationCanvas""></canvas>
    <script>
        const canvas = document.getElementById('animationCanvas');
        const ctx = canvas.getContext('2d');

        let width, height;
        const particles = [];
        const particleCount = 100;
        const connectionDistance = 100;
        const colors = ['#ff0000', '#00ff00', '#0000ff'];

        function resizeCanvas() {
            width = window.innerWidth;
            height = window.innerHeight;
            canvas.width = width;
            canvas.height = height;
        }

        class Particle {
            constructor() {
                this.x = Math.random() * width;
                this.y = Math.random() * height;
                this.vx = (Math.random() - 0.5) * 1;
                this.vy = (Math.random() - 0.5) * 1;
                this.radius = Math.random() * 2 + 1;
                this.color = colors[Math.floor(Math.random() * colors.length)];
            }

            update() {
                this.x += this.vx;
                this.y += this.vy;

                if (this.x < 0 || this.x > width) this.vx *= -1;
                if (this.y < 0 || this.y > height) this.vy *= -1;
            }
        }

        function init() {
            resizeCanvas();
            particles.length = 0;
            for (let i = 0; i < particleCount; i++) {
                particles.push(new Particle());
            }
        }

        function drawParticles() {
            ctx.clearRect(0, 0, width, height);

            particles.forEach(particle => {
                particle.update();

                ctx.beginPath();
                ctx.arc(particle.x, particle.y, particle.radius, 0, Math.PI * 2);
                ctx.fillStyle = particle.color;
                ctx.fill();
            });
        }

        function drawConnections() {
            ctx.lineWidth = 1.5;
            ctx.globalAlpha = 0.6;

            for (let i = 0; i < particles.length; i++) {
                for (let j = i + 1; j < particles.length; j++) {
                    const dx = particles[i].x - particles[j].x;
                    const dy = particles[i].y - particles[j].y;
                    const distance = Math.sqrt(dx * dx + dy * dy);

                    if (distance < connectionDistance) {
                        ctx.beginPath();
                        ctx.moveTo(particles[i].x, particles[i].y);
                        ctx.lineTo(particles[j].x, particles[j].y);
                        ctx.strokeStyle = particles[i].color;
                        ctx.stroke();
                    }
                }
            }

            ctx.globalAlpha = 1;
        }

        function animate() {
            ctx.clearRect(0, 0, width, height);
            drawParticles();
            drawConnections();
            requestAnimationFrame(animate);
        }

        window.addEventListener('resize', () => {
            resizeCanvas();
            init();
        });

        init();
        animate();
    </script>
</body>
</html>";

            _sharedWebView.NavigateToString(html);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ResizeWebView();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_isWorking)
            {
                using (var brush = new SolidBrush(ForeColor))
                {
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString("Working...", Font, brush, ClientRectangle, stringFormat);
                }
            }
        }
    }
}