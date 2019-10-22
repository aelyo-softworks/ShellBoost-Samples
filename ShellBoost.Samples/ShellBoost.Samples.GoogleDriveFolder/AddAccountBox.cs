using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // We wrote this custom ICodeReceiver because Google's LocalServerCodeReceiver has issues when the user cancels, plus Google's implementation cannot be embedded in a form / dialog.
    public partial class AddAccountBox : Form
    {
        // note this is private, you must go through the GetNewCodeReceiver method to display this.
        private AddAccountBox(string redirectUrl, string authorizationUrl, bool clearCookies)
        {
            // run the embedded web server
            Task.Run(() => RunServerAsync(redirectUrl)).ContinueWith(t =>
            {
                // setup the response
                ResponseUrl = t.Result;

                // we close the form
                BeginInvoke(new Action(() => Close()));
            });

            InitializeComponent();
            Icon = Program.AppIcon;

            // cookies can be a pain especially for testing
            if (clearCookies)
            {
                SuppressCookiesPersistence();
            }

            webBrowserMain.Navigate(authorizationUrl);
        }

        // if this stays null, it means the form was closed early or an error occured
        public AuthorizationCodeResponseUrl ResponseUrl { get; private set; }

        private async Task<AuthorizationCodeResponseUrl> RunServerAsync(string redirectUri)
        {
            // start a micro web server
            using (var listener = new HttpListener())
            {
                if (!redirectUri.EndsWith("/"))
                {
                    redirectUri += "/";
                }

                listener.Prefixes.Add(redirectUri);
                listener.Start();

                // here, we'll wait for redirection from our hosted webbrowser
                var context = await listener.GetContextAsync();

                // browser has navigated to our small http server
                // answer something we're just interested by the url's query string
                var html = string.Format("<html><body></body></html>");
                var buffer = Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = buffer.Length;
                var stream = context.Response.OutputStream;
                var responseTask = stream.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
                {
                    // close the stream and the web server
                    stream.Close();
                    listener.Stop();
                });

                // get code from response
                var query = context.Request.Url.Query;
                if (string.IsNullOrEmpty(query) || !query.StartsWith("?"))
                    return null;

                var responseUrl = new AuthorizationCodeResponseUrl(query.Substring(1));
                return responseUrl;
            }
        }

        private static void SuppressCookiesPersistence()
        {
            const int INTERNET_OPTION_SUPPRESS_BEHAVIOR = 81;
            const int INTERNET_SUPPRESS_COOKIE_PERSIST = 3;
            var buffer = INTERNET_SUPPRESS_COOKIE_PERSIST;
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SUPPRESS_BEHAVIOR, ref buffer, Marshal.SizeOf<int>());
        }

        [DllImport("wininet", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, ref int lpBuffer, int dwBufferLength);

        // Google's auth ICodeReceiver custom implementation
        public static ICodeReceiver GetNewCodeReceiver(bool clearCookies = true) => new FormCodeReceiver(clearCookies);

        private class FormCodeReceiver : ICodeReceiver
        {
            public FormCodeReceiver(bool clearCookies)
            {
                ClearCookies = clearCookies;
                RedirectUri = "http://127.0.0.1:" + GetRandomUnusedPort();
            }

            public bool ClearCookies { get; }
            public string RedirectUri { get; }

            public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken)
            {
                // show our form
                // note you can add &hl=EN to force the language to english for example
                var authorizationUrl = url.Build().AbsoluteUri;
                using (var dlg = new AddAccountBox(RedirectUri, authorizationUrl, ClearCookies))
                {
                    dlg.ShowDialog();
                    var result = dlg.ResponseUrl;

                    // form closed too early, or any error not caught by Google's auth code
                    if (result == null)
                        throw new TaskCanceledException();

                    return Task.FromResult(result);
                }
            }

            private static int GetRandomUnusedPort()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                try
                {
                    listener.Start();
                    return ((IPEndPoint)listener.LocalEndpoint).Port;
                }
                finally
                {
                    listener.Stop();
                }
            }
        }
    }
}
