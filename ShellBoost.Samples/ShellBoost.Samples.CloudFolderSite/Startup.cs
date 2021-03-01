using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // asp.net standard for getting content types
            services.AddSingleton<IContentTypeProvider>(new FileExtensionContentTypeProvider());

            // to push change events to (through websockets) clients
            services.AddSingleton<IFileSystemEvents, FileSystemEvents>();

            // add our file system as a service and configure it
            // TODO: change the path in appsettings.json to match your environment!!
            services.AddFileSystem(Configuration, options =>
            {
                // TODO: configure some options here
                // options.MaxTempFilesDays = 0;
            });


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

            // see https://github.com/dotnet/aspnetcore/issues/30545
            // app.UseHttpsRedirection();
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
