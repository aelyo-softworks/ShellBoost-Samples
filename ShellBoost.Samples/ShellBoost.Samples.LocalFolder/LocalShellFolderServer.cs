using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellFolderServer : ShellFolderServer
    {
        public const int AttributeIconIndex = 100;
        public const int ErrorOverlayIconIndex = 101;
        public const int WarningOverlayIconIndex = 102;

        private RootFolder _root;

        public LocalShellFolderServer(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (!info.Exists)
                throw new ArgumentException(null, nameof(info));

            Info = info;
            IconsDllPath = Path.GetFullPath("icons.dll");
            if (!File.Exists(IconsDllPath))
            {
                // use ShellBoost utility to extract .NET embedded resources and insert them into a Win32 resource-only .DLL file
                IconUtilities.SaveAsDll(Assembly.GetExecutingAssembly(), typeof(Program).Namespace + ".Resources.Attribute.ico", IconsDllPath, AttributeIconIndex, 1033, false);
                IconUtilities.SaveAsDll(Assembly.GetExecutingAssembly(), typeof(Program).Namespace + ".Resources.ErrorOverlay.ico", IconsDllPath, ErrorOverlayIconIndex, 1033, false);
                IconUtilities.SaveAsDll(Assembly.GetExecutingAssembly(), typeof(Program).Namespace + ".Resources.WarningOverlay.ico", IconsDllPath, WarningOverlayIconIndex, 1033, false);
            }
        }

        public DirectoryInfo Info { get; }
        public string IconsDllPath { get; }

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new RootFolder(this, idl);
            }
            return _root;
        }
    }
}
