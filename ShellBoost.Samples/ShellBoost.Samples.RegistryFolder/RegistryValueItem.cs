using System;
using System.Linq;
using Microsoft.Win32;
using ShellBoost.Core;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryValueItem : ShellItem
    {
        public RegistryValueItem(RegistryKeyFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CanDelete = true;
            CanRename = true;
            KeyName = name;
            DisplayName = name;
            bool iconIsString = true;
            using (var key = Parent.OpenKey(false))
            {
                if (key != null)
                {
                    Type = "REG_SZ";
                    if (IsDefault)
                    {
                        Data = key.GetValue(null);
                    }
                    else
                    {
                        Data = key.GetValue(DisplayName);
                        var kind = key.GetValueKind(DisplayName);
                        switch (kind)
                        {
                            // Must match enums we declared in RegistryFolder.propdesc schema file.
                            case RegistryValueKind.Binary:
                                Type = "REG_BINARY";
                                iconIsString = false;
                                break;

                            case RegistryValueKind.DWord:
                                Type = "REG_DWORD";
                                iconIsString = false;
                                break;

                            case RegistryValueKind.ExpandString:
                                Type = "REG_EXPAND_SZ";
                                break;

                            case RegistryValueKind.MultiString:
                                Type = "REG_MULTI_SZ";
                                break;

                            case RegistryValueKind.QWord:
                                Type = "REG_QWORD";
                                iconIsString = false;
                                break;
                        }
                    }
                }
            }

            Thumbnail = new AssemblyResourceShellThumbnail(Parent.Root.Server, GetType().Namespace + ".Resources." + (iconIsString ? "REG_SZ.ico" : "REG_BINARY.ico"));
        }

        public virtual bool IsDefault => false;
        public new RegistryKeyFolder Parent => (RegistryKeyFolder)base.Parent;
        public object Data { get; }
        public string Type { get; }
        public virtual string KeyName { get; }

        public string DataAsText
        {
            get
            {
                var data = Data;
                if (data is int i)
                    return "0x" + i.ToString("x8") + " (" + i + ")";

                if (data is long l)
                    return "0x" + l.ToString("x16") + " (" + l + ")";

                if (data is byte[] bytes)
                {
                    // display a max of 64 items
                    if (bytes.Length > 64)
                        return string.Join(" ", bytes.Take(64).Select(b => b.ToString("x2"))) + " ...";

                    return string.Join(" ", bytes.Select(b => b.ToString("x2")));
                }

                if (data is string[] strings)
                {
                    // display a max of 64 items
                    if (strings.Length > 64)
                        return string.Join(", ", strings.Take(64)) + " ...";

                    return string.Join(", ", strings);
                }

                if (data == null)
                    return "(value not set)";

                return string.Format("{0}", data);
            }
        }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            if (key == RegistryKeyFolder.DataProperty.PropertyKey)
            {
                value = DataAsText;
                return true;
            }

            if (key == RegistryKeyFolder.TypeProperty.PropertyKey)
            {
                value = Type;
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }

        protected override bool TryCompare(ShellItem other, ShellFolderColumn column, out CompareIdReturnValue value)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other is RegistryValueItem otherValue)
            {
                if (IsDefault)
                {
                    if (otherValue.IsDefault)
                    {
                        value = CompareIdReturnValue.LeftEqualsRight;
                        return true;
                    }

                    value = CompareIdReturnValue.LeftPrecedesRight;
                    return true;
                }
                else if (otherValue.IsDefault)
                {
                    value = CompareIdReturnValue.LeftFollowsRight;
                    return true;
                }
            }
            return base.TryCompare(other, column, out value);
        }
    }
}
