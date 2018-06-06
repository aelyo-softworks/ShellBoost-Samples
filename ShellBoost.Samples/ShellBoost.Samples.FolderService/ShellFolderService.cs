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

        internal void TestStartupAndStop(string[] args)
        {
            OnStart(args);
            Console.WriteLine("Press ESC to quit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            _server?.Stop();
            _server = new OverviewShellFolderServer();
            var config = new ShellFolderConfiguration();
            config.NativeDllRegistration = RegistrationMode.User;
            _server.Start(config);
        }

        protected override void OnStop()
        {
            _server?.Stop();
            _server = null;
        }
    }
}
