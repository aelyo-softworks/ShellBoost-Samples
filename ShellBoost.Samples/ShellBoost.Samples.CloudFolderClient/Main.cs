using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.FileSystem;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Sql;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderClient
{
    public partial class Main : Form
    {
        private readonly ImageList _imageList = new ImageList();

        public Main()
        {
            InitializeComponent();
            Icon = Program.AppIcon;
            contextMenuStripList.Opened += ContextMenuStripList_Opened;
            exitToolStripMenuItem.Click += (s, e) => Close();
            openToolStripMenuItem.Click += OpenToolStripMenuItemClick;
            newFolderToolStripMenuItem.Click += (s, e) => NewFolder();

            // only this code is hardcoded for the SQL file system
            var fs = new SqlFileSystem();
            var dic = new Dictionary<string, string>();

            // TODO: change the connection string with your context
            dic["ConnectionString"] = "Server=(localdb)\\mssqllocaldb;Database=CloudFolder;Integrated Security=True";
            fs.Initialize(null, null, dic);
            FileSystem = fs;

            var img = IconUtilities.LoadIcons(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), 4).GetPreferredSizeBitmap(16, disposeOthers: true);
            _imageList.Images.Add("folder", img); // folder

            listViewList.SmallImageList = _imageList;
            listViewList.DoubleClick += OnListViewListDoubleClick;

            treeViewFolders.ImageList = _imageList;
            treeViewFolders.BeforeExpand += OnTreeViewFoldersBeforeExpand;
            treeViewFolders.AfterCollapse += OnTreeViewFoldersAfterCollapse;
            treeViewFolders.AfterSelect += OnTreeViewFoldersAfterSelect;
            _ = Task.Run(async () => await LoadTreeFolder(treeViewFolders.Nodes, null).ConfigureAwait(false));
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

        private void NewFolder()
        {
            if (treeViewFolders.SelectedNode.Tag is not IFolderInfo folder)
                return;

            _ = Task.Run(async () =>
            {
                await folder.CreateAsync("New folder", new CreateOptions { Attributes = FileAttributes.Directory, EnsureUniqueName = true }).ConfigureAwait(false);
            });
        }

        private void Open(IFileSystemInfo item)
        {
            if (item == null)
                return;

            if (item.IsFolder())
            {
                var parentNode = treeViewFolders.Nodes.Find(item.ParentId.ToString(), true).FirstOrDefault();
                if (parentNode == null)
                    return;

                parentNode.Expand();
                // wait for expansion
                _ = Task.Run(() =>
                {
                    BeginInvoke((Action)(() =>
                    {
                        var node = treeViewFolders.Nodes.Find(item.Id.ToString(), true).FirstOrDefault();
                        if (node != null)
                        {
                            treeViewFolders.SelectedNode = node;
                            node.Expand();
                        }
                    }));
                });
                return;
            }

            var file = (IFileInfo)item;
            if (file.Length == 0)
            {
                MessageBox.Show(this, "File '" + file.GetDisplayName() + "' has no content.", Application.ProductName);
                return;
            }

            _ = Task.Run(async () =>
            {
                // create a temp file
                var fileName = IOUtilities.PathToValidFileName(file.Name);
                var ext = Path.GetExtension(fileName);
                var temp = Path.Combine(Path.GetTempPath(), "sbcfc", file.Id.ToString(), fileName);
                IOUtilities.FileCreateDirectory(temp);

                // write fs stream to it
                using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                using var fileStream = File.OpenWrite(temp);
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);

                // open it using whatever shell is configured for
                var psi = new ProcessStartInfo(temp);
                psi.UseShellExecute = true;
                Process.Start(psi);
            });
        }

        private void Delete(IFileSystemInfo item)
        {
        }

        private void Upload(IFileSystemInfo item)
        {
        }

        private void ContextMenuStripList_Opened(object sender, EventArgs e)
        {
            openToolStripMenuItem.Visible = listViewList.SelectedItems.Count > 0;
            deleteToolStripMenuItem.Visible = openToolStripMenuItem.Visible;
            renameToolStripMenuItem.Visible = openToolStripMenuItem.Visible;
            toolStripSeparator2.Visible = openToolStripMenuItem.Visible;

            if (openToolStripMenuItem.Visible)
            {
                contextMenuStripList.Tag = listViewList.SelectedItems[0].Tag;
            }
        }

        private void OnListViewListDoubleClick(object sender, EventArgs e)
        {
            if (listViewList.SelectedItems.Count == 0)
                return;

            Open(listViewList.SelectedItems[0].Tag as IFileSystemInfo);
        }

        private void OpenToolStripMenuItemClick(object sender, EventArgs e) => Open(contextMenuStripList.Tag as IFileSystemInfo);

        private void OnTreeViewFoldersAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is not IFolderInfo folder)
                return;

            listViewList.Items.Clear();
            _ = Task.Run(async () => await LoadListFolder(folder).ConfigureAwait(false));
        }

        private void OnTreeViewFoldersAfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.Nodes.Clear();
            e.Node.Nodes.Add(string.Empty);
        }

        private void OnTreeViewFoldersBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == string.Empty)
            {
                // lazy load
                var folder = (IFolderInfo)e.Node.Tag;
                _ = Task.Run(async () =>
                {
                    var first = true;
                    var options = new EnumerateOptions { IncludeFiles = false, IncludeHidden = ShowHiddenFiles, SortByName = true };
                    await foreach (var child in folder.EnumerateAsync(options).ConfigureAwait(false))
                    {
                        var wasFirst = first;
                        first = false;
                        BeginInvoke((Action)(() =>
                        {
                            if (wasFirst)
                            {
                                wasFirst = false;
                                e.Node.Nodes.Clear();
                            }

                            var node = new TreeNode(child.GetDisplayName());
                            node.Name = child.Id.ToString();
                            node.Tag = child;
                            e.Node.Nodes.Add(node);

                            node.ImageKey = "folder";
                            node.Nodes.Add(string.Empty);
                        }));
                    }

                    if (first)
                    {
                        e.Node.Nodes.Clear();
                    }
                });
            }
        }

        public IFileSystem FileSystem { get; }
        public bool ShowHiddenFiles { get; set; }

        public async Task LoadListFolder(IFolderInfo folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            var options = new EnumerateOptions { FoldersFirst = true, IncludeHidden = ShowHiddenFiles };
            await foreach (var child in folder.EnumerateAsync(options).ConfigureAwait(false))
            {
                BeginInvoke((Action)(() =>
                {
                    var item = new ListViewItem(child.Id.ToString());
                    item.Tag = child;
                    listViewList.Items.Add(item);

                    item.SubItems.Add(child.GetDisplayName());
                    if (child.IsFolder())
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
                        item.SubItems.Add(((IFileInfo)child).Length.ToString());
                    }

                    //item.SubItems.Add(child.Attributes.ToFullString());
                    item.SubItems.Add(child.LastWriteTimeUtc.ToLocalTime().ToString());
                    item.SubItems.Add(child.CreationTimeUtc.ToLocalTime().ToString());
                    item.SubItems.Add(child.LastAccessTimeUtc.ToLocalTime().ToString());
                }));
            }
        }

        public async Task LoadTreeFolder(TreeNodeCollection nodes, IFolderInfo folder)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            if (folder == null)
            {
                folder = (IFolderInfo)await FileSystem.GetItemAsync(Guid.Empty).ConfigureAwait(false);
                if (folder == null)
                    return;
            }

            BeginInvoke((Action)(() =>
            {
                var node = new TreeNode(folder.GetDisplayName());
                node.Name = folder.Id.ToString();
                node.Tag = folder;
                nodes.Add(node);

                // lazy
                node.Nodes.Add(string.Empty);

                if (folder.Id == Guid.Empty)
                {
                    node.Expand();
                }
            }));
        }
    }
}
