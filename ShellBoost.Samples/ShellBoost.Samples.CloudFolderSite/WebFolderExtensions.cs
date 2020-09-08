using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShellBoost.Samples.CloudFolderSite.FileSystem;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Sql;

namespace ShellBoost.Samples.CloudFolderSite
{
    public static class WebFolderExtensions
    {
        public const string ConfigPath = "CloudFolder:FileSystem:";

        public static void AddFileSystem(this IServiceCollection services, IConfiguration configuration, Action<WebFolderOptions> setupAction = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddSingleton((sp) =>
            {
                var typeName = configuration[ConfigPath + "TypeName"];
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    typeName = typeof(SqlFileSystem).AssemblyQualifiedName;
                }

                if (!(Activator.CreateInstance(Type.GetType(typeName, true)) is IFileSystem fs))
                    throw new WebFolderException("0005: Type '" + typeName + "' is not an " + nameof(IFileSystem) + ".");

                var fse = sp.GetService<IFileSystemEvents>();
                fs.Initialize(setupAction, fse, GetProperties(configuration));
                return fs;
            });
        }

        private static IDictionary<string, string> GetProperties(IConfiguration configuration) => configuration.GetSection(ConfigPath + "Properties").GetChildren().ToDictionary(c1 => c1.Key, c2 => c2.Value);
    }
}
