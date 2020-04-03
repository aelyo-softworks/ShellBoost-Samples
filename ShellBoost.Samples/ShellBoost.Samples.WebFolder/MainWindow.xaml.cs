using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.WebFolder.Api;
using ShellBoost.Samples.WebFolder.Folder;

namespace ShellBoost.Samples.WebFolder
{
    public partial class MainWindow : Window
    {
        private HwndSource _source;
        private Thread _serverThread;
        private AutoResetEvent _serverStopEvent;
        private System.Windows.Forms.NotifyIcon _nicon = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            Open.IsEnabled = false;

            _serverStopEvent = new AutoResetEvent(false);
            _serverThread = new Thread(WebDriveThread);
            _serverThread.IsBackground = true;
            _serverThread.Start();

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

            TB.Text = "ShellBoost Samples - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved." + Environment.NewLine;
            TB.Text += "ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion() + Environment.NewLine + Environment.NewLine;
            TB.Text += "Web Drive Folder - " + (IntPtr.Size == 8 ? "64" : "32") + "bit - V" + Assembly.GetExecutingAssembly().GetInformationalVersion() + Environment.NewLine;
            AppendText();

            Task.Run(async () => await PingServer());
        }

        private async Task PingServer()
        {
            // get root
            try
            {
                var item = await WebFolderApi.GetItemAsync(Guid.Empty);
                if (item != null)
                {
                    AppendText("Server at " + WebFolderApi.ApiBaseUrl + " was successfully pinged.");
                }
            }
            catch (Exception e)
            {
                AppendText("Server at " + WebFolderApi.ApiBaseUrl + " can't be pinged (Error: '" + e.Message + "'). Please make sure the WebFolderSite web site is started.");
            }
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

        private void WebDriveThread(object state)
        {
            do
            {
                try
                {
                    using (var server = new WebShellFolderServer())
                    {
                        var config = new ShellFolderConfiguration();
                        config.Logger = new Logger(this);

                        server.Start(config);
                        AppendText("Started listening on proxy id " + ShellFolderServer.ProxyId);

                        if (ShellFolderServer.LocationFolderId != Guid.Empty)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                Open.IsEnabled = true;
                            });
                        }
                        _serverStopEvent.WaitOne();
                        return;
                    }
                }
                catch (Exception e)
                {
                    AppendText(e.Message);
                    Thread.Sleep(1000);
                }
            }
            while (true);
        }

        private class Logger : ILogger
        {
            private MainWindow _window;

            public Logger(MainWindow window)
            {
                _window = window;
            }

            public void Log(TraceLevel level, object value, string methodName)
            {
                _window.AppendText("[" + level + "]" + methodName + ": " + value);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_serverStopEvent != null)
            {
                _serverStopEvent.Set();
                _serverStopEvent.Dispose();
            }

            var thread = _serverThread;
            if (thread != null)
            {
                thread.Join(1000);
            }
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
            if (text != null)
            {
                text = DateTime.Now + "[" + Thread.CurrentThread.ManagedThreadId + "]: " + text;
            }

            Dispatcher.BeginInvoke(() =>
            {
                TB.Text += Environment.NewLine;
                if (text != null)
                {
                    TB.Text += text;
                }
                //TB.ScrollToEnd();
            });
        }

        private void Quit_Click(object sender, RoutedEventArgs e) => Close(null, null);

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                var rm = new RestartManager();
                rm.RestartExplorerProcesses((s) =>
                {
                    AppendText("Windows Explorer was stopped...");
                }, false, out Exception error);
                AppendText("Windows Explorer was restarted...");
            });
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

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var id = ShellFolderServer.LocationFolderId;
            var kn = KnownFolder.Get(id);
            var idl = kn.GetIdList(Core.WindowsShell.KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT);

            dynamic window = new ShellUtilities.ShellBrowserWindow();
            ShellUtilities.CoAllowSetForegroundWindow(window);
            window.Visible = true;
            window.Navigate2(idl.Data);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => TB.Clear();
    }
}
