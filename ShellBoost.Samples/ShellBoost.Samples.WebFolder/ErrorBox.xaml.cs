using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.WebFolder
{
    public partial class ErrorBox : Window
    {
        public ErrorBox(Exception error)
            : this(error, error?.ToString())
        {
        }

        public ErrorBox(Exception error, string errorDetails)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));

            InitializeComponent();

            Title = Assembly.GetEntryAssembly().GetName().Name + " Error";
            ErrorDetails.Visibility = Visibility.Collapsed;
            ErrorDetails.Text = error.GetAllMessages();

            if (!string.IsNullOrWhiteSpace(errorDetails))
            {
                ErrorDetails.Text += Environment.NewLine + Environment.NewLine + "-- Details -- " + Environment.NewLine + Environment.NewLine + errorDetails;
            }

            ErrorDetails.Text += Environment.NewLine + Environment.NewLine + "-- Diagnostics -- " + Environment.NewLine + Environment.NewLine + GetDebugInformation();
            Image.Source = StockIcon.GetStockBitmap(StockIconId.ERROR, StockIcon.SHGSI.SHGSI_LARGEICON);
            ErrorText.Text = error.GetInterestingExceptionMessage();
        }

        public Exception Error { get; }
        public bool QuitRequested { get; internal set; }

        private string GetDebugInformation() => DiagnosticsInformation.Serialize(null);

        public static void HandleExceptions(Application app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            //if (!Debugger.IsAttached)
            {
                AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            }

            //if (!Debugger.IsAttached)
            {
                app.DispatcherUnhandledException += OnDispatcherUnhandledException;
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            bool quit = HandleException(e.Exception);
            e.Handled = true;
            if (quit || !Debugger.IsAttached)
            {
                Environment.Exit(0);
            }
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex == null)
                return;

            bool quit = HandleException(ex);
            if (quit || !Debugger.IsAttached)
            {
                Environment.Exit(0);
            }
        }

        public static bool HandleException(Exception error)
        {
            if (error == null)
                return false;

            var dlg = new ErrorBox(error);
            dlg.ShowDialog();
            return dlg.QuitRequested;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e) => Close();

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            const string CopyName = "Copy";
            if (CopyName.Equals(Details.Content))
            {
                Clipboard.SetText(ErrorDetails.Text);
                return;
            }

            Details.Content = CopyName;
            ErrorDetails.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(() =>
            {
                WindowsUtilities.CenterWindow(new WindowInteropHelper(this).Handle);
            }, DispatcherPriority.Background);
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            QuitRequested = true;
            Close();
        }
    }
}
