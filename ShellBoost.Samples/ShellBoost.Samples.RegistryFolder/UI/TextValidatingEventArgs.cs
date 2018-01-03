using System.ComponentModel;

namespace ShellBoost.Samples.RegistryFolder.UI
{
    public class TextValidatingEventArgs : CancelEventArgs
    {
        public TextValidatingEventArgs(string newText) => NewText = newText;
        public string NewText { get; }
    }
}
