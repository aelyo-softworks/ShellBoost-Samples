using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.CloudFolder.Api;
using ShellBoost.Samples.CloudFolder.UI;

namespace ShellBoost.Samples.CloudFolder
{
    public class WebShellFolder : ShellFolder, IObjectWithApiItem
    {
        public static readonly PropertyKey ServerIdPk = new PropertyKey(new Guid("17431cab-75f4-426e-8939-f14b2c77421a"), PropertyKey.PID_FIRST_USABLE + 1);
        public static readonly PropertyKey LocalStatusPk = new PropertyKey(new Guid("17431cab-75f4-426e-8939-f14b2c77421a"), PropertyKey.PID_FIRST_USABLE + 2);

        // root folder
        public WebShellFolder(ShellItemIdList idList)
            : base(idList)
        {
            CanLink = true;
            IsDropTarget = true;

            ApiItem = new WebItem(); // root's id is guid empty
            ApiItem.Attributes |= FileAttributes.Directory;
            ApiItem.IsRoot = true;

            Attributes |= SFGAO.SFGAO_STORAGE;

            // root is a "physical" shell item
            FileSystemPath = WebApi.LocalDirectory.FullName;
            DisplayName = "Samples.CloudFolder";
            DefineExtraColumns();
        }

        // child folder
        public WebShellFolder(WebShellFolder parent, WebItem apiItem)
            : base(parent, new GuidKeyShellItemId(apiItem.Id))
        {
            // this is reserved for non-root folders
            if (apiItem.Id == Guid.Empty)
                throw new InvalidOperationException();

            ApiItem = apiItem;
            DisplayName = apiItem.Name;
            DateCreated = apiItem.CreationTimeUtc.ToLocalTime();
            DateModified = apiItem.LastWriteTimeUtc.ToLocalTime();
            DateAccessed = apiItem.LastAccessTimeUtc.ToLocalTime();
            CanDelete = true;
            CanLink = true;
            CanPaste = true;
            CanRename = true;
            IsHidden = apiItem.IsHidden;

            // for some reason, this is necessary to support "New" menu
            Attributes |= SFGAO.SFGAO_STORAGE;

            // this is a "physical" shell item
            FileSystemPath = Path.Combine(parent.FileSystemPath ?? WebApi.LocalDirectory.FullName, apiItem.Name);
            DefineExtraColumns();
        }

        public WebItem ApiItem { get; }
        public new WebShellFolderServer FolderServer => (WebShellFolderServer)base.FolderServer;

        #region Dynamic columns support
        // add some custom columns
        private void DefineExtraColumns()
        {
            // this property is the server 's identifier, by default it's hidden
            // LocalShellItem and LocalShellServer don't have this property so when it's empty, it means we're looking
            // at the representation of a local item that's more recent than the corresponding server one
            AddColumn(ServerIdPk);

            // this property gives some information on synchronization
            AddColumn(LocalStatusPk);
        }

        protected override void OnGetDynamicColumnDetailsEvent(object sender, GetDynamicColumnDetailsEventArgs e)
        {
            // detail some info about our custom column
            if (e.PropertyKey == ServerIdPk)
            {
                e.Name = "Server Identifier";
                e.HResult = ShellUtilities.S_OK;
            }

            if (e.PropertyKey == LocalStatusPk)
            {
                e.Name = "Sync State";
                e.HResult = ShellUtilities.S_OK;
            }

            base.OnGetDynamicColumnDetailsEvent(sender, e);
        }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            // gives the value of our custom columns
            // note the Shell doesn't support extended types such as GUID
            // so we pass it as a string
            if (key == ServerIdPk)
            {
                value = ApiItem.Id.ToString();
                return true;
            }

            if (key == LocalStatusPk)
            {
                value = ApiItem.GetLocalStatus(FileSystemPath);
                return true;
            }

            // file equivalence for transfer operations, preview, etc.
            if (key == PropertyStore.IdentityName)
            {
                // we need the folder created to support "New menus..."
                value = FileSystemPath;
                ApiItem.EnsureLocalAsync(FileSystemPath).Wait();
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }
        #endregion

        #region Children enumeration & direct get support
        // this is a necessary ShellBoost override for better performance (direct access instead of implicit call to EnumItems)
        // https://www.shellboost.com/Doc/Developer-s-Guide/Items-enumeration
        protected override ShellItem GetItem(string displayName)
        {
            var apiItem = WebApi.GetChild(ApiItem, displayName);
            return ShellItemFromApi(apiItem);
        }

        // this is a necessary ShellBoost override for better performance (direct access instead of implicit call to EnumItems)
        // https://www.shellboost.com/Doc/Developer-s-Guide/Items-enumeration
        public override ShellItem GetItem(ShellItemId id)
        {
            var guidPidl = KeyShellItemId.From(id.Data, false) as GuidKeyShellItemId;
            if (guidPidl == null)
                return null;

            if (guidPidl.Value == Guid.Empty)
                return Root;

            var apiItem = WebApi.GetAsync(guidPidl.Value).Result;
            return ShellItemFromApi(apiItem);
        }

        // note this is not optimal as we only do this to refresh views (from ServerEvents).
        // we could therefore check if a view is not opened on the folder first in ServerEvents (using ShellBoost.Core.WindowsShell.View.Windows)
        // we could also add a cache here
        // etc.
        public ShellItem GetItem(Guid id)
        {
            var options = SHCONTF.SHCONTF_NONFOLDERS | SHCONTF.SHCONTF_FOLDERS | SHCONTF.SHCONTF_INCLUDEHIDDEN;
            var item = EnumItems(options).OfType<IObjectWithApiItem>().FirstOrDefault(o => o.ApiItem.Id == id);
            if (item != null)
                return (ShellItem)item;

            foreach (var child in EnumItems(options).OfType<WebShellFolder>())
            {
                var item2 = child.GetItem(id);
                if (item2 != null)
                    return item2;
            }
            return null;
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            var items = ApiItem.EnumerateChildren(options);
            foreach (var item in items)
            {
                var shellItem = ShellItemFromApi(item);
                yield return shellItem;
            }
        }

        private ShellItem ShellItemFromApi(WebItem item)
        {
            if (item == null)
                return null;

            if (item.IsFolder)
                return new WebShellFolder(this, item);

            return new WebShellItem(this, item);
        }
        #endregion

        #region Save As Support
        public override bool TryParseItem(ShellBindContext context, string displayName, out int eatenCharacters, out SFGAO attributes, out ShellItemIdList relativeIdl)
        {
            eatenCharacters = 0;
            attributes = SFGAO.SFGAO_NONE;

            // Windows Shell is calling us because it found no item
            // This may be called in the context of a common dialog box 

            // is it for creation? item does not necessarily exist
            if (context.Mode.HasFlag(STGM.STGM_CREATE))
            {
                // the default SFGAO for an item (+ SFGAO_FILESYSTEM)
                // note for creation mode we only send items back, not folders.
                attributes = DefaultItemAttributes | SFGAO.SFGAO_FILESYSTEM;

                // come up with some PIDL that we'll be able to recognize later (using ValidateNotExistingPidl)
                // you can use your usual "real" ShellItemIdList but in this sample, we use fake ones because we don't want to create guids that don't exist in the web (api).
                relativeIdl = new ShellItemIdList();
                relativeIdl.Add(new StringKeyShellItemId(displayName));
                return true;
            }

            // here, item must exist
            // displayName can be a name or a path (relative) and can ends with \ so we normalize it
            var path = IOUtilities.StripTerminatingPathSeparators(displayName);
            var item = ParseItem(path, out relativeIdl); // this will call into our GetItem implementation recursively
            if (item != null)
            {
                attributes = item.Attributes;
                return true;
            }

            return false; // don't go to base, we know better
        }

        private static string ValidateNotExistingPidl(ShellItemIdList pidl)
        {
            // this must be compatible with what we sent in TryParseItem method
            var stringPidl = KeyShellItemId.From(pidl?.Last?.Data, false) as StringKeyShellItemId;
            return stringPidl?.Value;
        }

        public override bool TryGetAttributes(ShellItemIdList relativeIdl, out SFGAO attributes)
        {
            var name = ValidateNotExistingPidl(relativeIdl);
            if (name == null) // ? this is not ours
                return base.TryGetAttributes(relativeIdl, out attributes);

            // the default SFGAO for an item (+ SFGAO_FILESYSTEM)
            attributes = DefaultItemAttributes | SFGAO.SFGAO_FILESYSTEM;
            return true;
        }

        public override bool TryGetDisplayName(ShellItemIdList relativeIdl, out string displayName)
        {
            var name = ValidateNotExistingPidl(relativeIdl);
            if (name == null) // ? this is not ours
                return base.TryGetDisplayName(relativeIdl, out displayName);

            displayName = name;
            return true;
        }
        #endregion

        #region Shell's "New" menu support
        // allow all menus commands to pass "as is"
        protected override bool MapContextMenuCommand(ShellFolder folder, IntPtr hwndOwner, string command, ref int inputId) => true;

        protected override void OnMenuInvoke(ShellMenuInvokeEventArgs e)
        {
            if (e.Verb != null)
            {
                // user is using "New ..." menu in the Explorer command bar
                // note if the user is using the "New..." menu in the folder's Context menu, it will not get there but be handled by WebShellFolderServer's notifier creation handling
                if (e.Verb.EqualsIgnoreCase("NewFolder") || e.Verb.EqualsIgnoreCase("NewLink") || e.Verb.StartsWith("."))
                {
                    // come up with a new file name
                    // since we're virtual we use a temp path

                    // here, we use the Shell itself to come up with a new file name (roundtrip api)
                    // note this can be costy in terms of performance (server call, etc.)
                    // so, we could choose arbitrary name instead, or ask the server for a new name
                    var files = new List<string>();
                    var folders = new List<string>();
                    foreach (var item in ApiItem.EnumerateChildren())
                    {
                        if (item.IsFolder)
                        {
                            folders.Add(IOUtilities.PathToValidFileName(item.Name));
                        }
                        else
                        {
                            files.Add(IOUtilities.PathToValidFileName(item.Name));
                        }
                    }

                    // use ShellBoost's utility classes
                    var options = new CreateNewItemOptions();
                    options.ExistingFileNames = files;
                    options.ExistingFolderNames = folders;
                    var path = Menu.CreateNewItem(e.Verb, options, false);
                    if (path != null)
                    {
                        var name = ApiItem.GetNewName(Path.GetFileName(path));
                        if (IOUtilities.DirectoryExists(path))
                        {
                            WebApi.CreateAsync(ApiItem.Id, null, name, FileAttributes.Directory);
                        }
                        else
                        {
                            WebApi.CreateAsync(ApiItem.Id, path, name);
                        }

                        // cleanup temp files
                        IOUtilities.DirectoryDelete(options.TargetPath, true, false);
                    }
                    return;
                }
            }

            // copy & cut support
            if (e.Command == DFM_CMD.DFM_CMD_COPY || e.Command == DFM_CMD.DFM_CMD_MOVE)
            {
                // make sure items are present locally
                // note if the past action is too fast, items may not be here yet (user will have to press "Retry")
                foreach (var si in e.Items)
                {
                    if (si is not IObjectWithApiItem apiItem)
                        continue;

                    Task.Run(() => apiItem.ApiItem.EnsureLocalAsync(si.FileSystemPath));
                }
                return;
            }

            // note DFM_CMD_DELETE is unhandled here so will fallback in OnOperate RecycleItem / RemoveItem
            base.OnMenuInvoke(e);
        }
        #endregion

        #region Rename & Delete support
        protected override void OnOperate(ShellOperationEventArgs e)
        {
            // some advanced errors number can be found in %ProgramFiles(x86)%\Windows Kits\10\Include\<SDK Version>\um\sherrors.h (for example with SDK Version = 10.0.19041.0)

            var item = (e.Item as IObjectWithApiItem)?.ApiItem;
            switch (e.Operation)
            {
                case ShellOperation.RecycleItem:
                    // our recycle bin is only local (a server side recycle bin may be better, but not implemented here)
                    if (e.Item is IObjectWithApiItem si && e.Item.FileSystemPath != null)
                    {
                        // we copy the file in an accessible folder so the user will be able to get to it when pulling out from recycle bin
                        var recycledPath = si.ApiItem.Recycle();
                        using (var fo = new FileOperation())
                        {
                            fo.SetOperationFlags(FOF.FOFX_RECYCLEONDELETE);
                            fo.DeleteItem(recycledPath);
                            fo.PerformOperations();
                        }

                        if (item.DeleteAsync(e.Item.FileSystemPath, new DeleteOptions { Recursive = true }).Result)
                        {
                            // tell the Shell not to process children (in case of a folder), as the server did it (or failed)
                            const int COPYENGINE_S_DONT_PROCESS_CHILDREN = 0x00270008;
                            e.HResult = COPYENGINE_S_DONT_PROCESS_CHILDREN;
                            NotifyUpdate();
                        }
                        else
                        {
                            // we can only reuse well-known error, for example
                            e.HResult = ShellUtilities.ERROR_RETRY;
                        }
                    }
                    break;

                case ShellOperation.RemoveItem:
                    if (item.DeleteAsync(e.Item.FileSystemPath, new DeleteOptions { Recursive = true }).Result)
                    {
                        // tell the Shell not to process children (in case of a folder), as the server did it (or failed)
                        const int COPYENGINE_S_DONT_PROCESS_CHILDREN = 0x00270008;
                        e.HResult = COPYENGINE_S_DONT_PROCESS_CHILDREN;
                        NotifyUpdate();
                    }
                    else
                    {
                        // we can only reuse well-known error, for example
                        e.HResult = ShellUtilities.ERROR_RETRY;
                    }
                    break;

                case ShellOperation.RenameItem:
                case ShellOperation.SetNameOf:
                    var result = item.RenameAsync(e.NewName, e.Item.FileSystemPath).Result;
                    if (result != null)
                    {
                        e.HResult = ShellUtilities.S_OK;
                        NotifyUpdate();
                    }
                    else
                    {
                        // we can only reuse well-known error, for example
                        e.HResult = ShellUtilities.ERROR_RETRY;
                    }
                    break;
            }
            base.OnOperate(e);
        }
        #endregion

        #region Copy/Paste and Drag/Drop support
        protected override void OnDragDropTarget(DragDropTargetEventArgs e)
        {
            // set the effect based upon KeyState
            // adapted from https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.drageventargs.keystate (we don't support link in that sample)
            if ((e.KeyState & 4) == 4 && e.AllowedEffect.HasFlag(DragDropEffects.Move))
            {
                // SHIFT state for move.
                e.Effect = DragDropEffects.Move;
            }
            else if ((e.KeyState & 8) == 8 && e.AllowedEffect.HasFlag(DragDropEffects.Copy))
            {
                // CTL state for copy.
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.AllowedEffect.HasFlag(DragDropEffects.Move))
            {
                // by default, the drop action should be move, if allowed.
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.Copy;
            }

            // usually, the most complete information is from the "CFSTR_SHELLIDLIST" or ""Shell IDList Array" clipboard format
            // this is reflected into the ItemsIdLists property or e's DataObject
            // you can browse each item in that list

            // prepare from drop to another folder
            if (e.Type == DragDropTargetEventType.Begin)
            {
                // make sure we're trusted https://docs.microsoft.com/en-us/windows/win32/shell/clipboard#cfstr_untrusteddragdrop
                e.AddFormat("UntrustedDragDrop", BitConverter.GetBytes(0));

                // make sure items are present locally
                // note if the drop action is too fast, items may not be here yet (user will have to press "Retry")
                // copies could be made synchronously to avoid that
                foreach (var idList in e.DataObject.ItemsIdLists)
                {
                    // check it's a WebShellItem or WebShellFolder
                    var si = GetItem(idList.Last);
                    if (si == null || si.FileSystemPath == null)
                        continue;

                    if (si is not IObjectWithApiItem apiItem)
                        continue;

                    // this is a callback event, no need to wait
                    // we do this recursively, if there are folders
                    Task.Run(() => apiItem.ApiItem.EnsureLocalAsync(si.FileSystemPath, true));
                }
                return;
            }

            // someone Dragged & Dropped something on this folder, or Copied and Pasted something on his folder
            if (e.Type == DragDropTargetEventType.DragDrop)
            {
                // here, we do things in another thread, don't lock the user
                Task.Run(UpdloadItems);

                async Task UpdloadItems()
                {
                    foreach (var idList in e.DataObject.ItemsIdLists)
                    {
                        // you can also use GetFileSystemPath() or GetPath() method
                        // but items may not be physical (just like us...)

                        // so, we use the shell API
                        var item = Item.FromIDList(idList);

                        WebItem newItem;
                        // detect a call from our own extension
                        // so we can user server calls instead of transferring streams
                        var requestItem = new WebItem();
                        var sourceId = item.GetPropertyValue<Guid>(ServerIdPk);
                        if (sourceId != Guid.Empty)
                        {
                            requestItem.Id = sourceId;
                            newItem = await requestItem.MoveAsync(ApiItem.Id, new MoveOptions { Copy = e.Effect.HasFlag(DragDropEffects.Copy) }).ConfigureAwait(false);
                        }
                        else
                        {
                            using (var stream = item.OpenRead())
                            {
                                requestItem.ParentId = ApiItem.Id;
                                requestItem.Name = ApiItem.GetNewName(item.SIGDN_NORMALDISPLAY);
                                requestItem.Attributes = item.IsFileSystemFolder ? FileAttributes.Directory : FileAttributes.Normal;
                                var date = item.DateAccessed;
                                if (date.HasValue)
                                {
                                    requestItem.LastAccessTimeUtc = date.Value.ToUniversalTime();
                                }

                                date = item.DateCreated;
                                if (date.HasValue)
                                {
                                    requestItem.CreationTimeUtc = date.Value.ToUniversalTime();
                                }

                                date = item.DateModified;
                                if (date.HasValue)
                                {
                                    requestItem.LastWriteTimeUtc = date.Value.ToUniversalTime();
                                }

                                newItem = await requestItem.UploadAsync(stream).ConfigureAwait(false);
                            }
                        }
                        NotifyUpdate();
                    }
                }
            }
        }
        #endregion

        #region Additional menu items
        protected override void MergeContextMenu(ShellFolder folder, IReadOnlyList<ShellItem> items, ShellMenu existingMenu, ShellMenu appendMenu)
        {
            if (folder.IsRoot && items.Count == 0) // context menu in Explorer's left TreeView
            {
                var properties = new ShellMenuItem(appendMenu, "&Properties");
                properties.Invoke += OnRootProperties;
                appendMenu.Items.Add(properties);
            }
            else
            {
                // only with selected items
                if (items.Count > 0)
                {
                    var folderItem = new ShellMenuItem(appendMenu, "Cloud Folder");
                    folderItem.BitmapPath = FolderServer.MenuIconBitmapPath;

                    var clearLocal = new ShellMenuItem(appendMenu, "Free up space");
                    clearLocal.Verb = "CloudFolder.free";
                    clearLocal.Invoke += OnClearLocal;
                    folderItem.Items.Add(clearLocal);

                    var downloadLocally = new ShellMenuItem(appendMenu, "Download on this device");
                    downloadLocally.Verb = "CloudFolder.download";
                    downloadLocally.Invoke += DownloadLocally;
                    folderItem.Items.Add(downloadLocally);

                    var pushToServer = new ShellMenuItem(appendMenu, "Push to server");
                    pushToServer.Verb = "CloudFolder.push";
                    pushToServer.Invoke += PushToServer;
                    folderItem.Items.Add(pushToServer);

                    appendMenu.Items.Add(folderItem);

                    // add the "Sent To" menu
                    appendMenu.Items.Add(new ShellMenuSendToItem());
                }
            }
        }

        private void PushToServer(object sender, ShellMenuInvokeEventArgs e)
        {
            foreach (var si in e.Items)
            {
                if (si.FileSystemPath == null || si is not IObjectWithApiItem apiItem)
                    continue;

                // already only on server,
                if (!IOUtilities.PathExists(si.FileSystemPath))
                    continue;

                Task.Run(() => FolderServer.LocalEvents.SynchronizeFile(si.FileSystemPath));
            }
            NotifyUpdate();
        }

        private void DownloadLocally(object sender, ShellMenuInvokeEventArgs e)
        {
            foreach (var si in e.Items)
            {
                if (si.FileSystemPath == null || si is not IObjectWithApiItem apiItem)
                    continue;

                Task.Run(() => apiItem.ApiItem.EnsureLocalAsync(si.FileSystemPath));
            }
            NotifyUpdate();
        }

        private void OnClearLocal(object sender, ShellMenuInvokeEventArgs e)
        {
            foreach (var item in e.Items)
            {
                IOUtilities.PathDelete(item.FileSystemPath, true, false);
            }
            NotifyUpdate();
        }

        private void OnRootProperties(object sender, ShellMenuInvokeEventArgs e)
        {
            var ctx = ShellContext.Current.Clone();
            TaskUtilities.EnsureSTAThreadTask(() =>
            {
                // find the top view window handle
                var viewHandle = Core.WindowsShell.View.GetOwnerViewHandle(e.HwndOwner);
                using (var form = new RootProperties())
                {
                    form.MemoryCacheCleared += (s, e2) =>
                    {
                        WebApi.ClearCache();
                    };
                    // use native proxy's 32x32 icon
                    form.Icon = FolderServer.NativeProxy.Icons.FirstOrDefault(i => i.Height == 32).ToIcon();
                    form.SelectedObject = WebApi.ServerInfo;
                    WindowsUtilities.RunForm(form, viewHandle);
                }
            });
        }

        protected override void OnRefresh(object sender, EventArgs e) => WebApi.ClearCache();

        #endregion
    }
}