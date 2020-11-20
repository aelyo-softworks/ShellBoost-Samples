using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolderSync.Utilities
{
    public class FileLogger : ConsoleLogger
    {
        private readonly static SingleThreadTaskScheduler _scheduler;

        static FileLogger()
        {
            _scheduler = new SingleThreadTaskScheduler((t) =>
            {
                t.Name = string.Format("_filelogger{0}", Environment.TickCount);
                return true;
            });
        }

        private int _counter;

        public FileLogger(bool logToConsole = true)
        {
            LogToConsole = logToConsole;
            LogToFile = true;
            AddCounter = true;
            AddThreadId = true;

            var name = string.Format("{0:yyyy}_{0:MM}_{0:dd}_{0:HHmmss}.log", DateTime.Now);
            FilePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Logs", name);
            IOUtilities.FileCreateDirectory(FilePath);
        }

        public bool LogToConsole { get; set; }
        public bool LogToFile { get; set; }
        public string FilePath { get; }

        public override void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null)
        {
            if ((int)MaximumTraceLevel < (int)level)
                return;

            var msg = string.Format("{0}", value).Nullify();
            if (msg != null)
            {
                msg = DateTime.Now + " | " + msg;
            }

            if (LogToConsole)
            {
                base.Log(level, value, methodName);
            }

            if (msg != null && LogToFile)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var writer = new StreamWriter(FilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(level.ToString().PadRight(7) + " | #" + _counter + " | T" + Thread.CurrentThread.ManagedThreadId + " | " + msg);
                        writer.Flush();
                        _counter++;
                    }
                }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            }
        }
    }
}
