using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public static class WebBrowserUtilities
    {
        // see https://stackoverflow.com/questions/28526826/web-browser-control-emulation-issue-feature-browser-emulation/28626667#28626667
        public static void SetWebBrowserFeatures()
        {
            // don't change the registry if running in-proc inside Visual Studio
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;

            var appName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";

            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION", appName, GetBrowserEmulationMode(), RegistryValueKind.DWord);

            // enable the features which are "On" for the full Internet Explorer browser
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", appName, 1, RegistryValueKind.DWord);
            Registry.SetValue(featureControlRegKey + "FEATURE_AJAX_CONNECTIONEVENTS", appName, 1, RegistryValueKind.DWord);
            Registry.SetValue(featureControlRegKey + "FEATURE_GPU_RENDERING", appName, 1, RegistryValueKind.DWord);
            Registry.SetValue(featureControlRegKey + "FEATURE_WEBOC_DOCUMENT_ZOOM", appName, 1, RegistryValueKind.DWord);
            Registry.SetValue(featureControlRegKey + "FEATURE_NINPUT_LEGACYMODE", appName, 0, RegistryValueKind.DWord);
        }

        public static uint GetBrowserEmulationMode()
        {
            int browserVersion = 0;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }

            if (browserVersion < 7)
                throw new ApplicationException("Unsupported version of Microsoft Internet Explorer!");

            uint mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode. 
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode. 
                    break;

                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode. 
                    break;

                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                    
                    break;

                case 10:
                    mode = 10000; // Internet Explorer 10.
                    break;
            }
            return mode;
        }
    }
}
