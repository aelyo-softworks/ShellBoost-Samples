using System;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.WebFolder
{
    public class Program
    {
        public static readonly SingleInstance Singleton = new SingleInstance(typeof(App).FullName);

        [STAThread]
        public static void Main(string[] args) =>
            
            // NOTE: if this always return false, close & restart Visual Studio
            // this is probably due to the vshost.exe thing
            Singleton.RunFirstInstance(() =>
            {
                var app = new App();
                ErrorBox.HandleExceptions(app);
                app.InitializeComponent();
                app.Run();
            });
    }
}
