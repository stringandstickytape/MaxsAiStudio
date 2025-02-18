﻿using System;
using System.Collections.Generic;
using System.Windows;

namespace AiStudio4
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WindowManager
    {
        private static WindowManager? _instance;
        private readonly Dictionary<string, MainWindow> _windows;
        private readonly object _lock = new object();

        public static WindowManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WindowManager();
                }
                return _instance;
            }
        }

        private WindowManager()
        {
            _windows = new Dictionary<string, MainWindow>();
        }

        public MainWindow CreateNewWindow(string windowId)
        {
            lock (_lock)
            {
                if (_windows.ContainsKey(windowId))
                {
                    _windows[windowId].Activate();
                    return _windows[windowId];
                }

                var window = new MainWindow();
                window.Title = windowId.StartsWith("main-") ? "AiStudio4" : $"AiStudio4 - Conversation {windowId}";

                // Handle window closing to remove it from dictionary
                window.Closed += (s, e) =>
                {
                    RemoveWindow(windowId);
                };

                _windows.Add(windowId, window);
                window.Show();
                return window;
            }
        }

        public void RemoveWindow(string windowId)
        {
            lock (_lock)
            {
                if (_windows.ContainsKey(windowId))
                {
                    _windows.Remove(windowId);
                }
            }
        }

        public MainWindow? GetWindow(string windowId)
        {
            lock (_lock)
            {
                return _windows.TryGetValue(windowId, out var window) ? window : null;
            }
        }

        public IEnumerable<MainWindow> GetAllWindows()
        {
            lock (_lock)
            {
                return _windows.Values;
            }
        }

        public void CloseAllWindows()
        {
            lock (_lock)
            {
                foreach (var window in _windows.Values)
                {
                    window.Close();
                }
                _windows.Clear();
            }
        }

        public bool HasOpenWindows()
        {
            lock (_lock)
            {
                return _windows.Count > 0;
            }
        }

        // Optional: Method to arrange windows on screen
        public void ArrangeWindows()
        {
            lock (_lock)
            {
                int count = _windows.Count;
                if (count == 0) return;

                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                // Simple grid arrangement
                int cols = (int)Math.Ceiling(Math.Sqrt(count));
                int rows = (int)Math.Ceiling((double)count / cols);

                double windowWidth = screenWidth / cols;
                double windowHeight = screenHeight / rows;

                int currentRow = 0;
                int currentCol = 0;

                foreach (var window in _windows.Values)
                {
                    window.Width = windowWidth;
                    window.Height = windowHeight;
                    window.Left = currentCol * windowWidth;
                    window.Top = currentRow * windowHeight;

                    currentCol++;
                    if (currentCol >= cols)
                    {
                        currentCol = 0;
                        currentRow++;
                    }
                }
            }
        }
    }
}