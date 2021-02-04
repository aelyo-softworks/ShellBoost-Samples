using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolderClient
{
    internal static class Extensions
    {
        public static string GetDisplayName(this WebItem item) => item != null ? (item.Id != Guid.Empty ? item.Name : "<Root>") : string.Empty;

        public static void ShowMessage(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static DialogResult ShowConfirm(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName + " - Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        public static void ShowError(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName + " - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        public static void ShowWarning(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName + " - Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static IEnumerable<T> EnumerateAllTags<T>(this TreeView tree)
        {
            if (tree == null)
                yield break;

            foreach (TreeNode node in tree.Nodes)
            {
                foreach (var child in EnumerateAllTags<T>(node))
                {
                    yield return child;
                }
            }
        }

        public static IEnumerable<T> EnumerateAllTags<T>(this TreeNode node)
        {
            if (node == null)
                yield break;

            var tag = node.Tag;
            if (tag != null && typeof(T).IsAssignableFrom(tag.GetType()))
                yield return (T)tag;

            foreach (TreeNode child in node.Nodes)
            {
                foreach (var grandChild in EnumerateAllTags<T>(child))
                {
                    yield return grandChild;
                }
            }
        }

        public static IEnumerable<TreeNode> EnumerateAll(this TreeView tree)
        {
            if (tree == null)
                yield break;

            foreach (TreeNode node in tree.Nodes)
            {
                foreach (var child in EnumerateAll(node))
                {
                    yield return child;
                }
            }
        }

        public static IEnumerable<TreeNode> EnumerateAll(this TreeNode node)
        {
            if (node == null)
                yield break;

            yield return node;
            foreach (TreeNode child in node.Nodes)
            {
                foreach (var grandChild in EnumerateAll(child))
                {
                    yield return grandChild;
                }
            }
        }

        public static T GetSelectedTag<T>(this ListView list)
        {
            var tag = list.GetSelectedItem()?.Tag;
            if (tag == null || !typeof(T).IsAssignableFrom(tag.GetType()))
                return default;

            return (T)tag;
        }

        public static IEnumerable<T> GetSelectedTags<T>(this ListView list)
        {
            foreach (var item in list.GetSelectedItems())
            {
                var tag = item.Tag;
                if (tag == null || !typeof(T).IsAssignableFrom(tag.GetType()))
                    continue;

                yield return (T)tag;
            }
        }

        public static T GetSelectedTag<T>(this TreeView tree)
        {
            var tag = tree.SelectedNode?.Tag;
            if (tag == null || !typeof(T).IsAssignableFrom(tag.GetType()))
                return default;

            return (T)tag;
        }

        public static ListViewItem GetSelectedItem(this ListView list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            return list.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
        }

        public static IEnumerable<ListViewItem> GetSelectedItems(this ListView list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            return list.SelectedItems.OfType<ListViewItem>();
        }

        public static void BeginInvoke(this Control control, Action action)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            control.BeginInvoke(action);
        }
    }
}
