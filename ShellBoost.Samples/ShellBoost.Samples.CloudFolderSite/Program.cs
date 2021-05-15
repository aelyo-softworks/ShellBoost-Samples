using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ShellBoost.Samples.CloudFolderSite
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // we added this to disable the 30M request limit https://github.com/aspnet/Announcements/issues/267
                // note: although it seems to be effective for Kestrel, for some reason, it doesn't when using IIS or IISExpress
                // the choice between Kestrel or IIS is made using Visual Studio's "Start" green button
                services.Configure<IISServerOptions>(options =>
                {
                    options.MaxRequestBodySize = null;
                });

                services.Configure<KestrelServerOptions>(options =>
                {
                    options.Limits.MaxRequestBodySize = null;
                });

                services.Configure<HttpSysOptions>(options =>
                {
                    options.MaxRequestBodySize = null;
                });
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
