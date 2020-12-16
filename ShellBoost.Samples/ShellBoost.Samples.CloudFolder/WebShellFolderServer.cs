using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolder.Api;
using ShellBoost.Samples.CloudFolder.Events;

namespace ShellBoost.Samples.CloudFolder
{
    public class WebShellFolderServer : ShellFolderServer
    {
        private static readonly Lazy<WebShellFolderServer> _current = new Lazy<WebShellFolderServer>(() => new WebShellFolderServer());
        public static WebShellFolderServer Current => _current.Value;

        private WebShellFolder _root;
        private readonly Lazy<string> _menuIconBitmapPath;
        private readonly ServerEvents _serverEvents;

        public WebShellFolderServer()
        {
            _menuIconBitmapPath = new Lazy<string>(() => GetMenuIconBitmapPath(), true);
            _serverEvents = new ServerEvents(this);
            LocalEvents = new LocalEvents(this);
        }

        public LocalEvents LocalEvents { get; }
        public string MenuIconBitmapPath => _menuIconBitmapPath.Value;

        protected override ShellFolder GetFolderAsRoot(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new WebShellFolder(idl);
            }
            return _root;
        }

        public new void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null) => Configuration.Logger?.Log(level, value, methodName);

        public ShellItem GetItem(Guid apiItemId)
        {
            if (apiItemId == Guid.Empty)
                return _root;

            return _root.GetItem(apiItemId);
        }

        public ShellItem GetRemoteItem(string fileSystemPath)
        {
            if (fileSystemPath == null || (_root == null))
                return null;

            var relative = fileSystemPath.Substring(WebApi.LocalDirectory.FullName.Length);
            if (relative.StartsWith(@"\"))
            {
                relative = relative.Substring(1);
            }

            return _root.ParseItem(relative);
        }

        private string GetMenuIconBitmapPath()
        {
            var cachePath = Path.Combine(Path.GetTempPath(), FolderId.ToString("N") + ".png");
            if (!IOUtilities.FileExists(cachePath))
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType().Namespace + ".UI.CloudFolder.ico"))
                {
                    if (stream == null)
                        throw new InvalidOperationException();
                    
                    // note the 16 size is a 256 colors, not a full RGB color as the Shell's context menu doesn't like that
                    var bmps = IconUtilities.LoadIconsFromStream(stream);
                    var bmp = bmps.FirstOrDefault(i => i.Height == 16);
                    bmp.Save(cachePath);
                }
            }
            return cachePath;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                LocalEvents?.Dispose();
                _serverEvents?.Dispose();
            }
        }
    }
}
