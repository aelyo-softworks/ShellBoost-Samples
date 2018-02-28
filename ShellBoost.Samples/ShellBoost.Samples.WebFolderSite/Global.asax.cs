using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using ShellBoost.Samples.WebFolderSite.Model;

namespace ShellBoost.Samples.WebFolderSite
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // initialize drive and create some test items
            // note we use long path to avoid problems
            Drive.Root = new Drive(@"\\?\" + Server.MapPath("~/App_Data/Drive"));
            var folder = Drive.Root.EnsureFolder(@"test1\test2");
            for (int i = 0; i < 10; i++)
            {
                var item = folder.EnsureChildItem("test" + i + ".txt");
                File.WriteAllText(item.FullPath, "héllo" + i, Encoding.UTF8);
            }

            // configure json formatting
            GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new JsonGuidConverter());

            // setup routes
            GlobalConfiguration.Configure(config =>
            {
                config.MapHttpAttributeRoutes();

                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );
            });
        }

        private class JsonGuidConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(Guid);
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(((Guid)value).ToString("N"));
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => existingValue;
        }
    }
}