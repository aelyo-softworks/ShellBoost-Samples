using System;
using System.ServiceProcess;
using ShellBoost.Core;

namespace ShellBoost.Samples.FolderService
{
    public partial class ShellFolderService : ServiceBase
    {
        private OverviewShellFolderServer _server;

        public ShellFolderService()
        {
            InitializeComponent();
        }

        internal void InteractiveStartAndStop()
        {
            CommonStart();
            Console.WriteLine("Press ESC to quit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }
            OnStop();
        }

        private void CommonStart(string ipcFormat = null)
        {
            _server?.Stop();
            _server = new OverviewShellFolderServer();
            var config = new ShellFolderConfiguration();

            // we're not impersonating client
            config.ImpersonateClient = false;

            // this will fix an endpoint for clients connecting to a service:
            // they *must* register with "ShellBoost.Samples.FolderService.ShellFolderService" as IpcFormat.
            //
            // clients from interactive run (console) don't need that.
            config.IpcFormat = ipcFormat;

            config.NativeDllRegistration = RegistrationMode.User;
            _server.Start(config);
        }

        protected override void OnStart(string[] args) => CommonStart(GetType().FullName);

        protected override void OnStop()
        {
            _server?.Stop();
            _server = null;
        }
    }
}
