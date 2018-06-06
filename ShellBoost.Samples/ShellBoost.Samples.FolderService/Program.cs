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
                var service = new ShellFolderService();
                service.TestStartupAndStop(null);
            }
            else
            {
                var servicesToRun = new ServiceBase[]
                {
                    new ShellFolderService()
                };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
