using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ShellBoost.Core;

namespace ShellBoost.Samples.LocalFolder
{
    public class XmlAttributeShellItem : ShellItem
    {
        public XmlAttributeShellItem(ShellFolder parent, XmlAttribute attribute)
            : base(parent, new StringKeyShellItemId(attribute.LocalName))
        {
            Attribute = attribute;
            DisplayName = attribute.LocalName;
        }

        public XmlAttribute Attribute { get; }
    }
}
