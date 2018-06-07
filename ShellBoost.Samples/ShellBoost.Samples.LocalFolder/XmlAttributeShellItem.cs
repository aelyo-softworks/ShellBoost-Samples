using System.Xml;
using ShellBoost.Core;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.LocalFolder
{
    public class XmlAttributeShellItem : ShellItem
    {
        public XmlAttributeShellItem(ShellFolder parent, XmlAttribute attribute)
            : base(parent, new StringKeyShellItemId(attribute.LocalName))
        {
            Attribute = attribute;
            DisplayName = attribute.LocalName;
            var iconsPath = ((RootFolder)parent.Root).Server.IconsDllPath;

            // note the icon index syntax: the index must be negative when passed to the Windows Shell
            Thumbnail = new ShellThumbnail(iconsPath, -LocalShellFolderServer.AttributeIconIndex);
        }

        public XmlAttribute Attribute { get; }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            // OverlayIconLocation is not a Windows property, it's a ShellBoost special property
            if (key == PropertyStore.OverlayIconLocation)
            {
                var iconsPath = ((RootFolder)Parent.Root).Server.IconsDllPath;
                
                // note the icon index syntax: the index must be negative when passed to the Windows Shell
                switch (Attribute.Value)
                {
                    case "Error":
                        value = iconsPath + ",-" + LocalShellFolderServer.ErrorOverlayIconIndex;
                        return true;

                    case "Warning":
                        value = iconsPath + ",-" + LocalShellFolderServer.WarningOverlayIconIndex;
                        return true;
                }
            }

            return base.TryGetPropertyValue(key, out value);
        }
    }
}
