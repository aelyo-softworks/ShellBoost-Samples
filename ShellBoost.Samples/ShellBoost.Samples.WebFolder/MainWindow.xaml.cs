using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.WebFolder
{
    public partial class MainWindow : Window
    {
        private HwndSource _source;
        private System.Windows.Forms.NotifyIcon _nicon = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            // NOTE: icon resource must be named same as namespace + icon
            Icon = AppParameters.IconSource;
            _nicon.Icon = AppParameters.Icon;
            _nicon.Text = Assembly.GetEntryAssembly().GetTitle();
            _nicon.ContextMenu = new System.Windows.Forms.ContextMenu();
            _nicon.ContextMenu.MenuItems.Add("Show", Show);
            _nicon.ContextMenu.MenuItems.Add("-");
            _nicon.ContextMenu.MenuItems.Add("Quit", Close);
            _nicon.Visible = true;
            _nicon.DoubleClick += Show;

            Restart.IsEnabled = IntPtr.Size == (Environment.Is64BitOperatingSystem ? 8 : 4);
            if (!Restart.IsEnabled)
            {
                Restart.ToolTip = "Windows Explorer on this machine can only be restarted from a " + (Environment.Is64BitOperatingSystem ? "64" : "32") + "-bit process.";
            }

            TB.Text = "ShellBoost Samples - Copyright © 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved." + Environment.NewLine + Environment.NewLine;
            TB.Text += "Web Drive Folder - " + (IntPtr.Size == 8 ? "64" : "32") + "bit - V" + Assembly.GetExecutingAssembly().GetInformationalVersion() + Environment.NewLine;
            AppendText("Server waiting for requests...");
            AppendText();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // this handles the singleton instance
            _source = (HwndSource)PresentationSource.FromVisual(this);
            _source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == Program.Singleton.Message && WindowState == WindowState.Minimized)
                {
                    Show(null, null);
                    handled = true;
                    return IntPtr.Zero;
                }

                var ret = Program.Singleton.OnWndProc(hwnd, msg, wParam, lParam, true, true, ref handled);
                if (handled)
                    return ret;

                return ret;
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _nicon?.Dispose();
        }

        // hide if manually minimized
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        // minimized if closed
        protected override void OnClosing(CancelEventArgs e)
        {
            if (WindowState == WindowState.Minimized || // if CTRL+SHIFT is pressed, do close
                ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            base.OnClosing(e);
            WindowState = WindowState.Minimized;
        }

        private void Close(object sender, EventArgs e)
        {
            WindowState = WindowState.Minimized;
            Close();
        }

        private void Show(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        public void AppendText() => AppendText(null);
        public void AppendText(string text)
        {
            Dispatcher.BeginInvoke(() =>
            {
                TB.Text += Environment.NewLine;
                if (text != null)
                {
                    TB.Text += DateTime.Now + ": " + text;
                }
            });
        }

        private void Quit_Click(object sender, RoutedEventArgs e) => Close(null, null);

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            var rm = new RestartManager();
            rm.RestartExplorerProcesses((state) =>
            {
                AppendText("Windows Explorer was stopped...");
            }, false, out Exception error);
            AppendText("Windows Explorer was restarted...");
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            ShellFolderServer.RegisterNativeDll(RegistrationMode.User);
            ShellUtilities.RefreshShellViews();
            AppendText("Native proxy was registered to HKCU.");
        }

        private void Unregister_Click(object sender, RoutedEventArgs e)
        {
            ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
            ShellUtilities.RefreshShellViews();
            AppendText("Native proxy was unregistered from HKCU.");
        }
    }
}
