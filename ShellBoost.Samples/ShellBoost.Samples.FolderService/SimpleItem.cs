using ShellBoost.Core;

namespace ShellBoost.Samples.FolderService
{
    public class SimpleItem : ShellItem
    {
        public SimpleItem(RootFolder parent, string text)
            : base(parent, new StringKeyShellItemId(text))
        {
        }
    }
}
