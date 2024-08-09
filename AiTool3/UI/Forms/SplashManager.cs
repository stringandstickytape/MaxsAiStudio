namespace AiTool3
{
    public partial class MaxsAiStudio
    {
        public class SplashManager
        {
            private Form splash;
            private Thread splashThread;

            public void ShowSplash()
            {
                splash = new Form();
                splash.Size = new System.Drawing.Size(200, 200);
                splash.StartPosition = FormStartPosition.CenterScreen;
                splash.FormBorderStyle = FormBorderStyle.None;

                var loadingLabel = new Label();
                loadingLabel.Text = "Loading Max's AI Studio";
                loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
                loadingLabel.AutoSize = false;
                loadingLabel.Dock = DockStyle.Fill;
                splash.Controls.Add(loadingLabel);

                splashThread = new Thread(() =>
                {
                    Application.Run(splash);
                });
                splashThread.SetApartmentState(ApartmentState.STA);
                splashThread.Start();
            }

            public void CloseSplash()
            {
                if (splash != null && !splash.IsDisposed)
                {
                    if (splash.InvokeRequired)
                        splash.Invoke(new Action(() => splash.Close()));
                    else
                        splash.Close();
                }

                if (splashThread != null && splashThread.IsAlive)
                    splashThread.Join();
            }
        }

    }

}