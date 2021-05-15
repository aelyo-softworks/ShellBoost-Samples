using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolderSite
{
    public class WebFolderConfiguration
    {
        public WebFolderConfiguration(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            ShowChoice = Conversions.ChangeType(configuration.GetSection("CloudFolder:Choice")?.Value, true);
            if (!CommandLine.GetArgument("ShowChoice", ShowChoice))
            {
                ShowChoice = false;
            }

            var list = new List<WebFolderConfigurationFileSystem>();
            foreach (var section in configuration.GetSection("CloudFolder:FileSystems").GetChildren())
            {
                if (section.Path == null)
                    continue;

                var fs = new WebFolderConfigurationFileSystem();
                fs.Index = list.Count;
                foreach (var section2 in configuration.GetSection(section.Path).GetChildren())
                {
                    if (section2.Key.EqualsIgnoreCase("IsDefault"))
                    {
                        // last one wins
                        foreach (var fs2 in list)
                        {
                            fs2.IsDefault = false;
                        }
                        fs.IsDefault = true;
                        DefaultFileSystem = fs;
                        continue;
                    }

                    if (section2.Key.EqualsIgnoreCase("TypeName"))
                    {
                        fs.TypeName = section2.Value.Nullify();
                        continue;
                    }

                    if (section2.Key.EqualsIgnoreCase("Description"))
                    {
                        fs.Description = section2.Value.Nullify();
                        continue;
                    }

                    if (section2.Key.EqualsIgnoreCase("Properties") && section2.Path != null)
                    {
                        foreach (var kv in configuration.GetSection(section2.Path).GetChildren().ToDictionary(c1 => c1.Key, c2 => c2.Value))
                        {
                            fs.Properties[kv.Key] = kv.Value;
                        }
                        continue;
                    }
                }

                if (fs.TypeName != null)
                {
                    list.Add(fs);
                }
            }

            FileSystems = list;
            if (DefaultFileSystem == null && list.Count > 0)
            {
                DefaultFileSystem = list[0];
            }
        }

        public bool ShowChoice { get; }
        public IReadOnlyList<WebFolderConfigurationFileSystem> FileSystems { get; }
        public WebFolderConfigurationFileSystem DefaultFileSystem { get; }
    }
}
