using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.FileSystem;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            var conf = new WebFolderConfiguration(Configuration);
            if (conf.DefaultFileSystem == null)
                throw new WebFolderException("0005: No file system is configured.");

            ChoosenFileSystem = conf.DefaultFileSystem;

            Console.WriteLine("ShellBoost Samples - " + (Core.Client.Installer.IsNetCore ? "Core" : null) + "Overview - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());

            if (conf.ShowChoice && conf.FileSystems.Count > 1)
            {
                var maxWait = CommandLine.GetArgument("ChoiceMaxWait", 5000);
                Console.WriteLine();
                Console.WriteLine("Press one of the following keys.");
                Console.WriteLine();
                foreach (var fs in conf.FileSystems)
                {
                    Console.WriteLine("   '" + fs.Index + "' for '" + fs.DisplayName + "' file system.");
                    foreach (var kv in fs.Properties)
                    {
                        Console.WriteLine("       " + kv.Key + ": " + kv.Value);
                    }

                    Console.WriteLine();
                }

                // because Visual Studio will start the web after a certain amount of time
                Console.WriteLine("If you don't press a key within " + maxWait + " ms, the '" + ChoosenFileSystem.DisplayName + "' will be selected automatically.");

                var key = IOExtensions.ConsoleReadKey(maxWait);
                var index = key.KeyChar - '0';
                if (index >= 0 && index < conf.FileSystems.Count)
                {
                    ChoosenFileSystem = conf.FileSystems[index];
                }
            }

            Console.WriteLine("Starting '" + ChoosenFileSystem.DisplayName + "'.");
            Console.WriteLine();
        }

        public IConfiguration Configuration { get; }
        public WebFolderConfigurationFileSystem ChoosenFileSystem { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // asp.net standard for getting content types
            services.AddSingleton<IContentTypeProvider>(new FileExtensionContentTypeProvider());

            // to push change events to (through websockets) clients
            services.AddSingleton<IFileSystemEvents, FileSystemEvents>();

            // add our "Cloud Folder" file system as a service with its configuration
            // TODO: update the information in appsettings.json to match your environment!!
            services.AddSingleton(typeof(IFileSystem), ChoosenFileSystem.Type);
            services.AddSingleton(ChoosenFileSystem);

            services.AddControllers();

            // we use signalR to push change events (use UseAzureSignalR with Azure hosting)
            // https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-concept-scale-aspnet-core
            services.AddSignalR((options) =>
            {
#if DEBUG
                options.EnableDetailedErrors = true;
#endif
            });

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            // To see these ETW traces, use for example https://github.com/smourier/TraceSpy
            // The guid is an arbitrary value that you'll have to add to TraceSpy's ETW providers.
            // note: this will only works on Windows, but will fail gracefully on other OSes
            loggerFactory.AddEventProvider(new Guid("a3f87db5-0cba-4e4e-b712-439980e59870"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<FileSystemHub>("/events");
                endpoints.MapRazorPages();
            });
        }
    }
}
