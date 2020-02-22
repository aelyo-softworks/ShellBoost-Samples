using System;
using System.ServiceProcess;

namespace ShellBoost.Samples.FolderService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                // this is only for console testing (note: the application must be of Console type, not Windows)
                var service = new ShellFolderService();
                service.InteractiveStartAndStop();
            }
            else
            {
                // service mode
                var servicesToRun = new ServiceBase[]
                {
                    new ShellFolderService()
                };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
