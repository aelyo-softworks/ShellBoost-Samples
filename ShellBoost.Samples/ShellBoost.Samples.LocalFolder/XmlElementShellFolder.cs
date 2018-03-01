using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class XmlElementShellFolder : ShellFolder
    {
        public XmlElementShellFolder(ShellFolder parent, XmlElement element)
            : base(parent, new StringKeyShellItemId(element.LocalName))
        {
            Element = element;
            DisplayName = element.LocalName;
        }

        public XmlElement Element { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            if (options.HasFlag(SHCONTF.SHCONTF_FOLDERS))
            {
                foreach (var child in Element.ChildNodes.OfType<XmlElement>())
                {
                    yield return new XmlElementShellFolder(this, child);
                }
            }

            if (options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
            {
                foreach (var att in Element.Attributes.OfType<XmlAttribute>())
                {
                    yield return new XmlAttributeShellItem(this, att);
                }
            }
        }
    }
}
