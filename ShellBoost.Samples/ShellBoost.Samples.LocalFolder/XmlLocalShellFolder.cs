using System.Collections.Generic;
using System.IO;
using System.Xml;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class XmlLocalShellFolder : ShellFolder
    {
        public XmlLocalShellFolder(ShellFolder parent, FileInfo info)
            : base(parent, new StringKeyShellItemId(info.Name))
        {
            Info = info;
            DisplayName = info.Name;
        }

        public FileInfo Info { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            var xml = new XmlDocument();
            try
            {
                xml.Load(Info.FullName);
            }
            catch
            {
                yield break;
            }

            if (xml.DocumentElement == null)
                yield break;

            yield return new XmlElementShellFolder(this, xml.DocumentElement);
        }
    }
}
