using System;
using System.Collections.Generic;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.PhysicalOverview
{
    // this folder is not backed by a physical directory
    public class RootFolder : RootShellFolder
    {
        public RootFolder(PhysicalOverviewShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Hello = new HelloWorldItem(this);

            // get a path like C:\Users\<user>\AppData\Local\ShellBoost.Samples.PhysicalOverview\Root
            RootPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), GetType().Namespace), "Root");

            // ensure it exists
            var di = new DirectoryInfo(RootPath);
            if (!di.Exists)
            {
                di.Create();
            }

            RootPhysical = new PhysicalFolder(this, di);
            Server = server;

            // we want to know when something changes in the real folder so we can update ours
            // note we don't use .NET's FileSystemWatcher but a Shell-oriented ShellBoost-provider tool instead base on native Shell's APIs.
            ChangeNotifier = new ChangeNotifier(ShellUtilities.GetIdList(RootPath), true);
            ChangeNotifier.Notify += OnChangeNotifierNotify;
        }

        public string RootPath { get; }
        public PhysicalOverviewShellFolderServer Server { get; }
        public HelloWorldItem Hello { get; }
        public PhysicalFolder RootPhysical { get; }
        public ChangeNotifier ChangeNotifier { get; }

        protected override void OnManageColumnsEvent(object sender, ManageColumnsEventArgs e)
        {
            var col = e.GetColumn(Props.System.DateModified);
            if (col != null)
            {
                col.State |= CM_STATE.CM_STATE_VISIBLE;// | CM_STATE.CM_STATE_ALWAYSVISIBLE;
            }
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            yield return Hello;
            yield return RootPhysical;
        }

        private void OnChangeNotifierNotify(object sender, ChangeNotifyEventArgs e)
        {
            // get our equivalent item 
            if (e.FileSystemPath1 != null)
            {
                // is it really about us?
                if (e.FileSystemPath1.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    // get relative path and normalize a bit
                    var relPath = IOUtilities.PathRemoveStartSlash(e.FileSystemPath1.Substring(RootPath.Length));
                    switch (e.Event)
                    {
                        case SHCNE.SHCNE_DELETE:
                        case SHCNE.SHCNE_CREATE:
                        case SHCNE.SHCNE_RMDIR:
                        case SHCNE.SHCNE_MKDIR:
                            // we need to get the path's parent, because the path points to an item that doesn't exist or was deleted
                            relPath = Path.GetDirectoryName(relPath);
                            break;
                    }

                    var item = RootPhysical.ParseItem(relPath);
                    if (item != null)
                    {
                        // note we could be smarter and handle event one by one
                        item.NotifyUpdate();
                    }
                }
            }
        }
    }
}
