﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.CloudFolder.Api;
using ShellBoost.Samples.CloudFolderClient.Events;

namespace ShellBoost.Samples.CloudFolderClient
{
    public partial class Main : Form
    {
        // holds icons
        private readonly ImageList _imageList = new ImageList();

        private ServerEvents _server;
        private Log _log;
        private bool _showHiddenFiles;
        private bool _showTempFiles;

        // internal cut & copy support
        private readonly ConcurrentDictionary<Guid, Tuple<DragDropEffects, WebItem>> _cutCopyItems = new ConcurrentDictionary<Guid, Tuple<DragDropEffects, WebItem>>();
        private readonly List<ServerEventArgs> _serverEvents = new List<ServerEventArgs>();

        public Main()
        {
            Text = Application.ProductName;
            InitializeComponent();
            Icon = Program.AppIcon;

            ShowHiddenFiles = true; // triggers the initial load
            showHiddenStripMenuItem.Checked = true;

            // icons handling
            // get standard folder icon
            var img = IconUtilities.LoadIcons(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), 4).GetPreferredSizeBitmap(16, disposeOthers: true);
            _imageList.Images.Add("folder", img);
            listViewList.SmallImageList = _imageList;
            treeViewFolders.ImageList = _imageList;

            // context menu handling
            contextMenuStripTree.Opening += ContextMenuStripTree_Opening;
            contextMenuStripTree.Opened += ContextMenuStripTree_Opened;
            contextMenuStripList.Opening += ContextMenuStripList_Opening;
            contextMenuStripList.Opened += ContextMenuStripList_Opened;

            aboutToolStripMenuItem.Click += (s, e) => About();
            aboutToolStripMenuItem.Text = "About " + ProductName + "...";
            serverInfoStripMenuItem.Click += (s, e) => ServerInfo();
            exitToolStripMenuItem.Click += (s, e) => Close();
            downloadStripMenuItem.Click += (s, e) => Download();
            openToolStripMenuItem.Click += (s, e) => Open(listViewList.GetSelectedTag<WebItem>());
            th16ToolStripMenuItem.Click += (s, e) => Open(listViewList.GetSelectedTag<WebItem>(), 16);
            th32ToolStripMenuItem.Click += (s, e) => Open(listViewList.GetSelectedTag<WebItem>(), 32);
            th48ToolStripMenuItem.Click += (s, e) => Open(listViewList.GetSelectedTag<WebItem>(), 48);
            th256ToolStripMenuItem.Click += (s, e) => Open(listViewList.GetSelectedTag<WebItem>(), 256);
            newFolderToolStripMenuItem.Click += (s, e) => NewFolder();
            renameToolStripMenuItem.Click += (s, e) => listViewList.GetSelectedItem()?.BeginEdit();
            deleteToolStripMenuItem.Click += (s, e) => Delete(listViewList.GetSelectedTags<WebItem>());
            deleteTreeStripMenuItem.Click += (s, e) => Delete(new WebItem[] { treeViewFolders.GetSelectedTag<WebItem>() });
            uploadToolStripMenuItem.Click += (s, e) => UploadTo(treeViewFolders.GetSelectedTag<WebItem>());
            uploadTreeStripMenuItem.Click += (s, e) => UploadTo(treeViewFolders.GetSelectedTag<WebItem>());
            replaceContentStripMenuItem.Click += (s, e) => UploadTo(treeViewFolders.GetSelectedTag<WebItem>(), listViewList.GetSelectedTag<WebItem>());
            copyIdStripMenuItem.Click += (s, e) => CopyId(treeViewFolders.GetSelectedTag<WebItem>());
            copyId2StripMenuItem.Click += (s, e) => CopyId(listViewList.GetSelectedTag<WebItem>());
            cutStripMenuItem.Click += CutStripMenuItem_Click;
            cutToolStripMenuItem.Click += CutToolStripMenuItem_Click;
            copyStripMenuItem.Click += CopyStripMenuItem_Click;
            copyToolStripMenuItem.Click += CopyToolStripMenuItem_Click;
            pasteStripMenuItem.Click += (s, e) => Paste();
            pasteToolStripMenuItem.Click += (s, e) => Paste();
            eventsLogStripMenuItem.Click += EventsLogStripMenuItem_Click;
            viewStripMenuItem.DropDownOpening += ViewStripMenuItem_DropDownOpening;
            showHiddenStripMenuItem.Click += (s, e) => ShowHiddenFiles = showHiddenStripMenuItem.Checked;
            showTempStripMenuItem.Click += (s, e) => ShowTempFiles = showTempStripMenuItem.Checked;
            refreshStripMenuItem.Click += (s, e) => Reload();

            // listview events handling
            listViewList.DoubleClick += (s, e) => Open(listViewList.GetSelectedTag<WebItem>());
            listViewList.DragOver += (s, e) => e.Effect = DragDropEffects.Copy;
            listViewList.DragDrop += (s, e) => Paste(treeViewFolders.GetSelectedTag<WebItem>(), e.Data);
            listViewList.AfterLabelEdit += ListViewList_AfterLabelEdit;

            // treeview events handling
            treeViewFolders.BeforeExpand += OnTreeViewFoldersBeforeExpand;
            treeViewFolders.AfterCollapse += OnTreeViewFoldersAfterCollapse;
            treeViewFolders.AfterSelect += OnTreeViewFoldersAfterSelect;
            treeViewFolders.NodeMouseClick += (s, e) => treeViewFolders.SelectedNode = e.Node; // right click selects
            treeViewFolders.DragOver += TreeViewFolders_DragOver;
            treeViewFolders.DragDrop += (s, e) => Paste(treeViewFolders.GetSelectedTag<WebItem>(), e.Data);

            // load root node
            Cursor = Cursors.WaitCursor;
        }

        public bool ShowHiddenFiles
        {
            get => _showHiddenFiles;
            set
            {
                if (_showHiddenFiles == value)
                    return;

                _showHiddenFiles = value;
                Reload();
            }
        }

        public bool ShowTempFiles
        {
            get => _showTempFiles;
            set
            {
                if (_showTempFiles == value)
                    return;

                _showTempFiles = value;
                Reload();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                Delete(listViewList.GetSelectedTags<WebItem>());
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == 13) // enter cannot be used as shortcut key
            {
                Open(listViewList.GetSelectedTag<WebItem>());
                return;
            }

            if (e.KeyChar == 27) // escape cancels cut/paste
            {
                _cutCopyItems.Clear();
                return;
            }
            base.OnKeyPress(e);
        }

        private void ViewStripMenuItem_DropDownOpening(object sender, EventArgs e) => eventsLogStripMenuItem.Checked = _log != null;

        private void EventsLogStripMenuItem_Click(object sender, EventArgs e)
        {
            if (eventsLogStripMenuItem.Checked)
            {
                if (_log == null)
                {
                    _log = new Log();
                    _log.AddEvents(_serverEvents);
                    _log.Show();
                    _log.FormClosed += (s, ev) => _log = null;
                    _log.ClearLog += ClearLog;

                    // find a place around this window
                    var screen = Screen.FromHandle(Handle);

                    // right or left?
                    var leftSpace = Location.X - screen.Bounds.X;
                    var rightSpace = screen.Bounds.Right - Location.X - Size.Width;
                    if (leftSpace > rightSpace)
                    {
                        _log.Location = new Point(Location.X - _log.Width, Location.Y);
                    }
                    else
                    {
                        _log.Location = new Point(Location.X + Size.Width, Location.Y);
                    }
                }
            }
            else
            {
                if (_log != null)
                {
                    _log.ClearLog -= ClearLog;
                    _log.Close();
                    _log = null;
                }
            }
        }

        private void ClearLog(object sender, EventArgs e) => _serverEvents.Clear();

        private void TreeViewFolders_DragOver(object sender, DragEventArgs e)
        {
            var pt = treeViewFolders.PointToClient(new Point(e.X, e.Y));
            var node = treeViewFolders.GetNodeAt(pt);
            if (node != null)
            {
                e.Effect = DragDropEffects.Copy;
                node.Expand();
            }
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            _cutCopyItems.Clear();
            foreach (var item in listViewList.GetSelectedTags<WebItem>())
            {
                _cutCopyItems[item.Id] = new Tuple<DragDropEffects, WebItem>(DragDropEffects.Copy, item);
            }
        }

        private void CopyStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            _cutCopyItems.Clear();
            var folder = treeViewFolders.GetSelectedTag<WebItem>();
            if (folder != null)
            {
                _cutCopyItems[folder.Id] = new Tuple<DragDropEffects, WebItem>(DragDropEffects.Copy, folder);
            }
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            _cutCopyItems.Clear();
            foreach (var item in listViewList.GetSelectedTags<WebItem>())
            {
                _cutCopyItems[item.Id] = new Tuple<DragDropEffects, WebItem>(DragDropEffects.Move, item);
            }
        }

        private void CutStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            _cutCopyItems.Clear();
            var folder = treeViewFolders.GetSelectedTag<WebItem>();
            if (folder != null)
            {
                _cutCopyItems[folder.Id] = new Tuple<DragDropEffects, WebItem>(DragDropEffects.Move, folder);
            }
        }

        private void ListViewList_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Label))
                return;

            if (listViewList.Items[e.Item].Tag is not WebItem item)
                return;

            if (item.Name.EqualsIgnoreCase(e.Label))
                return;

            Rename(item, e.Label);
        }

        private void ContextMenuStripList_Opening(object sender, CancelEventArgs e)
        {
            if (treeViewFolders.GetSelectedTag<WebItem>() == null)
            {
                e.Cancel = true;
                return;
            }

            var item = listViewList.GetSelectedTag<WebItem>();
            downloadStripMenuItem.Enabled = item != null && !item.IsFolder;
            th16ToolStripMenuItem.Visible = item != null;
            th32ToolStripMenuItem.Visible = item != null;
            th48ToolStripMenuItem.Visible = item != null;
            th256ToolStripMenuItem.Visible = item != null;
            toolStripSeparator6.Visible = item != null;
            if (item != null)
            {
                var isImage = WebApi.IsSupportedThumbnailFile(Path.GetExtension(item.Name));
                th16ToolStripMenuItem.Enabled = isImage;
                th32ToolStripMenuItem.Enabled = isImage;
                th48ToolStripMenuItem.Enabled = isImage;
                th256ToolStripMenuItem.Enabled = isImage;
            }
        }

        private void ContextMenuStripTree_Opening(object sender, CancelEventArgs e)
        {
            if (treeViewFolders.GetSelectedTag<WebItem>() == null)
            {
                e.Cancel = true;
                return;
            }
        }

        private void ContextMenuStripTree_Opened(object sender, EventArgs e)
        {
            var folder = treeViewFolders.GetSelectedTag<WebItem>();
            if (folder == null)
                return;

            deleteTreeStripMenuItem.Enabled = folder.Id != WebApi.ServerInfo.RootId;
            cutStripMenuItem.Enabled = deleteTreeStripMenuItem.Enabled;
            pasteStripMenuItem.Enabled = IsPasteEnabled();
        }

        private void ContextMenuStripList_Opened(object sender, EventArgs e)
        {
            openToolStripMenuItem.Visible = listViewList.SelectedItems.Count > 0;
            deleteToolStripMenuItem.Visible = openToolStripMenuItem.Visible;
            renameToolStripMenuItem.Visible = openToolStripMenuItem.Visible;
            toolStripSeparator2.Visible = openToolStripMenuItem.Visible;
            cutToolStripMenuItem.Visible = openToolStripMenuItem.Visible;
            copyToolStripMenuItem.Visible = openToolStripMenuItem.Visible;
            replaceContentStripMenuItem.Visible = openToolStripMenuItem.Visible;
            pasteToolStripMenuItem.Enabled = IsPasteEnabled();
        }

        private void OnTreeViewFoldersAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is not WebItem folder)
                return;

            _ = Task.Run(() => LoadListFolder(folder));
        }

        private void OnTreeViewFoldersAfterCollapse(object sender, TreeViewEventArgs e)
        {
            // this allow a refresh on re-expand
            Lazyfy(e.Node);

            var folder = treeViewFolders.GetSelectedTag<WebItem>();
            if (folder != null)
            {
                _ = Task.Run(() => LoadListFolder(folder));
            }
        }

        private void OnTreeViewFoldersBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Tag is not WebItem folder)
                return;

            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == string.Empty)
            {
                _ = Task.Run(() =>
                {
                    this.BeginInvoke(() => e.Node.Nodes.Clear());
                    var options = new EnumerateOptions { IncludeFiles = false, IncludeHidden = ShowHiddenFiles, IncludeTemps = ShowTempFiles, SortByName = true };
                    foreach (var child in WebApi.EnumerateChildren(folder, options))
                    {
                        this.BeginInvoke(() => LoadTreeFolder(e.Node.Nodes, child));
                    }
                });
            }
        }

        private bool IsPasteEnabled()
        {
            if (Clipboard.ContainsData(ShellDataObjectFormat.CFSTR_SHELLIDLIST))
                return true;

            return !_cutCopyItems.IsEmpty;
        }

        private async Task OnServerEvent(ServerEventArgs e)
        {
            _serverEvents.Add(e);
            _log?.AddEvent(e);

            var item = await WebApi.GetAsync(e.Id).ConfigureAwait(false);
            this.BeginInvoke(() =>
            {
                // refresh folder list
                var folder = treeViewFolders.GetSelectedTag<WebItem>();
                if (folder?.Id == e.ParentId)
                {
                    _ = Task.Run(() => LoadListFolder(folder));
                }

                switch (e.Type)
                {
                    case WatcherChangeTypes.Created:
                        resetParentNode();
                        break;

                    case WatcherChangeTypes.Deleted:
                        var deletedNode = treeViewFolders.Nodes.Find(e.Id.ToString(), true).FirstOrDefault();
                        if (deletedNode != null)
                        {
                            deletedNode.Remove();
                        }
                        break;

                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Renamed:
                        if (item != null && item.IsFolder)
                        {
                            if (e.OldParentId.HasValue) // moved?
                            {
                                // remove from source
                                var sourceNode = treeViewFolders.Nodes.Find(e.Id.ToString(), true).FirstOrDefault();
                                if (sourceNode != null)
                                {
                                    sourceNode.Remove();
                                }

                                // add to new parent
                                resetParentNode();
                            }
                            else
                            {
                                // get node if exist and change name if needed
                                var changedNode = treeViewFolders.Nodes.Find(e.Id.ToString(), true).FirstOrDefault();
                                if (changedNode != null)
                                {
                                    if (changedNode.Text != item.GetDisplayName())
                                    {
                                        changedNode.Text = item.GetDisplayName();
                                    }
                                }
                            }
                        }
                        break;
                }

                void resetParentNode()
                {
                    var parentNode = treeViewFolders.Nodes.Find(e.ParentId.ToString(), true).FirstOrDefault();
                    if (parentNode != null)
                    {
                        if (parentNode.IsExpanded)
                        {
                            if (item != null && item.IsFolder)
                            {
                                var existingNode = parentNode.Nodes.Find(item.Id.ToString(), true).FirstOrDefault();
                                if (existingNode == null)
                                {
                                    var newNode = LoadTreeFolder(parentNode.Nodes, item);
                                    if (newNode != null)
                                    {
                                        newNode.EnsureVisible();
                                    }
                                }
                            }
                        }
                        else
                        {
                            parentNode.Expand();
                        }
                    }
                }
            });
        }

        private void Download()
        {
            var item = listViewList.GetSelectedTag<WebItem>();
            if (item == null || item.IsFolder) // we don't support download whole folders
                return;

            var ofd = new SaveFileDialog();
            ofd.Title = "Choose";
            ofd.CheckPathExists = true;
            ofd.FileName = item.Name;
            ofd.ValidateNames = true;
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            var dlg = new Download(item, ofd.FileName);
            dlg.ShowDialog(this);
        }

        private void ServerInfo()
        {
            var si = WebApi.ServerInfo;
            if (si == null)
                return;

            var msg = "File System: " + si.FileSystem + Environment.NewLine;
            msg += "Id: " + si.Id + Environment.NewLine;
            msg += "Bitness: " + si.Bitness + Environment.NewLine;
            msg += "Configuration: " + si.Configuration + Environment.NewLine;
            msg += "File Version: " + si.FileVersion + Environment.NewLine;
            msg += "Informational Version: " + si.InformationalVersion + Environment.NewLine;
            msg += "Time Difference: " + si.TimeDifference + Environment.NewLine;
            msg += "Root Id: " + si.RootId.ToString("N");
            this.ShowMessage(msg);
        }

        private void About()
        {
            var dlg = new About();
            dlg.ShowDialog(this);
        }

        private void Reload()
        {
            treeViewFolders.Nodes.Clear();
            _ = Task.Run(async () =>
            {
                try
                {
                    var id = WebApi.ServerInfo.Id; // this will connect to server, if it fails here, check connections, server, etc.
                }
                catch (Exception e)
                {
                    this.BeginInvoke(() =>
                    {
                        this.ShowError(e.Message);
                        Close();
                    });
                    return;
                }

                _server = new ServerEvents();
                _server.Event += async (s, e) => await OnServerEvent(e).ConfigureAwait(false);
                var root = await WebApi.GetAsync(WebApi.ServerInfo.RootId).ConfigureAwait(false);

                // select root node
                this.BeginInvoke(() =>
                {
                    Cursor = Cursors.Default;
                    Text = Application.ProductName + " - " + WebApi.RootUrl;
                    LoadTreeFolder(treeViewFolders.Nodes, root);
                    if (treeViewFolders.Nodes.Count > 0)
                    {
                        treeViewFolders.SelectedNode = treeViewFolders.Nodes[0];
                    }
                });
            });
        }

        private void NewFolder()
        {
            var folder = treeViewFolders.GetSelectedTag<WebItem>();
            if (folder == null)
                return;

            _ = Task.Run(async () =>
            {
                await WebApi.CreateAsync(folder.Id, "New folder", FileAttributes.Directory).ConfigureAwait(false);
            });
        }

        private static void Paste(WebItem folder, IDataObject dataObject)
        {
            if (dataObject == null)
                return;

            // favor CFSTR_SHELLIDLIST ("Shell IDList Array") format, use ShellBoost utilities
            var idList = ShellDataObjectFormat.GetCFSTR_SHELLIDLIST(dataObject);
            if (idList.Count == 0)
                return;

            _ = Task.Run(async () =>
            {
                foreach (var idl in idList)
                {
                    var item = Item.FromIDList(idl, false);
                    if (item != null)
                    {
                        using var stream = item.OpenRead();
                        await UploadAsync(folder, item.SIGDN_NORMALDISPLAY, stream).ConfigureAwait(false);
                    }
                }
            });
        }

        private void Paste()
        {
            var folder = treeViewFolders.GetSelectedTag<WebItem>();
            if (listViewList.Focused)
            {
                var item = listViewList.GetSelectedTag<WebItem>();
                if (item != null && item.IsFolder)
                {
                    folder = item;
                }
            }
            Paste(folder);
        }

        private void Paste(WebItem folder)
        {
            if (folder == null)
                return;

            if (_cutCopyItems.IsEmpty)
            {
                Paste(folder, Clipboard.GetDataObject());
                return;
            }

            var kvs = _cutCopyItems.ToArray();
            _cutCopyItems.Clear();
            _ = Task.Run(async () =>
            {
                foreach (var kv in kvs)
                {
                    var options = new MoveOptions();
                    if (kv.Value.Item1 == DragDropEffects.Copy)
                    {
                        options.Copy = true;
                    }

                    await WebApi.MoveAsync(kv.Value.Item2, folder.Id, options).ConfigureAwait(false);
                }
            });
        }

        private static void Rename(WebItem item, string newName)
        {
            if (item == null || string.IsNullOrWhiteSpace(newName))
                return;

            _ = Task.Run(async () =>
            {
                var options = new RenameOptions();
                await WebApi.RenameAsync(item, newName).ConfigureAwait(false);
            });
        }

        private void CopyId(WebItem item)
        {
            if (item == null)
                return;

            Clipboard.SetText(item.Id.ToString());
        }

        private void Open(WebItem item, int? width = null)
        {
            if (item == null)
                return;

            if (item.IsFolder)
            {
                var parentNode = treeViewFolders.Nodes.Find(item.ParentId.ToString(), true).FirstOrDefault();
                if (parentNode == null)
                    return;

                parentNode.Expand();
                // wait for expansion
                _ = Task.Run(() =>
                {
                    this.BeginInvoke(() =>
                    {
                        var node = treeViewFolders.Nodes.Find(item.Id.ToString(), true).FirstOrDefault();
                        if (node != null)
                        {
                            treeViewFolders.SelectedNode = node;
                            node.Expand();
                        }
                    });
                });
                return;
            }

            if (item.Length == 0)
            {
                this.ShowWarning("File '" + item.Name + "' has no content.");
                return;
            }

            _ = Task.Run(async () =>
            {
                // create a temp file
                var fileName = IOUtilities.PathToValidFileName(item.Name);
                var ext = Path.GetExtension(fileName);
                var temp = Path.Combine(Path.GetTempPath(), "sbcfc", item.Id.ToString(), fileName);
                IOUtilities.FileCreateDirectory(temp);

                // write fs stream to it
                using var fileStream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                await WebApi.DownloadAsync(item, fileStream, width).ConfigureAwait(false);

                // open it using whatever shell is configured for
                var psi = new ProcessStartInfo(temp);
                psi.UseShellExecute = true;
                Process.Start(psi);
            });
        }

        private void Delete(IEnumerable<WebItem> items)
        {
            if (items == null)
                return;

            var array = items.Where(item => item != null).ToArray();
            if (array.Length == 0)
                return;

            if (this.ShowConfirm("Are you sure you want to delete '" + string.Join(", ", array.Select(item => item.GetDisplayName())) + "'?") != DialogResult.Yes)
                return;

            _ = Task.Run(async () =>
            {
                foreach (var item in array)
                {
                    await WebApi.DeleteAsync(item).ConfigureAwait(false);
                }
            });
        }

        private void UploadTo(WebItem folder, WebItem replaceContentItem = null)
        {
            if (folder == null)
                return;

            var ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.RestoreDirectory = true;
            if (replaceContentItem != null)
            {
                var ext = replaceContentItem.Extension.ToLowerInvariant();
                ofd.Filter = ext + " files (*." + ext + ")|*" + ext + "|All files (*.*)|*.*";
                ofd.Multiselect = false;
            }

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            var fileNames = ofd.FileNames;
            _ = Task.Run(async () =>
                {
                    if (replaceContentItem != null)
                    {
                        using var file = File.OpenRead(ofd.FileName);
                        await ReplaceContentAsync(replaceContentItem, file).ConfigureAwait(false);
                    }
                    else
                    {
                        foreach (var fileName in fileNames)
                        {
                            using var file = File.OpenRead(fileName);
                            await UploadAsync(folder, Path.GetFileName(fileName), file).ConfigureAwait(false);
                        }
                    }
                });
        }

        private static async Task<WebItem> ReplaceContentAsync(WebItem item, Stream inputStream)
        {
            if (item == null)
                return null;

            return await WebApi.UploadAsync(item, inputStream).ConfigureAwait(false);
        }

        private static async Task<WebItem> UploadAsync(WebItem folder, string name, Stream inputStream)
        {
            if (folder == null)
                return null;

            var item = await WebApi.CreateAsync(folder.Id, name).ConfigureAwait(false);
            return await WebApi.UploadAsync(item, inputStream).ConfigureAwait(false);
        }

        private void LoadListFolder(WebItem folder)
        {
            if (folder == null)
                return;

            var options = new EnumerateOptions { FoldersFirst = true, IncludeHidden = ShowHiddenFiles, IncludeTemps = ShowTempFiles, SortByName = true };
            var children = WebApi.EnumerateChildren(folder, options);
            this.BeginInvoke(() =>
            {
                listViewList.Items.Clear();
                foreach (var child in children)
                {
                    var item = new ListViewItem(child.GetDisplayName());
                    item.Tag = child;
                    listViewList.Items.Add(item);

                    item.SubItems.Add(child.Id.ToString());
                    if (child.IsFolder)
                    {
                        item.ImageKey = "folder";
                        item.SubItems.Add(string.Empty); // size
                    }
                    else
                    {
                        var ext = Path.GetExtension(child.Name).ToLowerInvariant();
                        if (!_imageList.Images.ContainsKey(ext))
                        {
                            var img = ShellUtilities.GetImageForExtension(ext, new Size(16, 16));
                            if (img != null)
                            {
                                _imageList.Images.Add(ext, img);
                            }
                        }

                        item.ImageKey = ext;
                        item.SubItems.Add(child.Length.ToString());
                    }

                    item.SubItems.Add(child.Attributes.ToFullString());
                    item.SubItems.Add(child.LastWriteTimeUtc.ToLocalTime().ToString());
                    item.SubItems.Add(child.CreationTimeUtc.ToLocalTime().ToString());
                    item.SubItems.Add(child.LastAccessTimeUtc.ToLocalTime().ToString());
                }
            });
        }

        private static TreeNode LoadTreeFolder(TreeNodeCollection nodes, WebItem folder)
        {
            if (folder == null)
                return null;

            var node = new TreeNode(folder.GetDisplayName());
            node.Name = folder.Id.ToString();
            node.Tag = folder;
            nodes.Add(node);
            Lazyfy(node);

            if (folder.Id == WebApi.ServerInfo.RootId)
            {
                node.Expand();
            }
            return node;
        }

        private static void Lazyfy(TreeNode node)
        {
            node.Nodes.Clear();
            node.Nodes.Add(string.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _imageList?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
