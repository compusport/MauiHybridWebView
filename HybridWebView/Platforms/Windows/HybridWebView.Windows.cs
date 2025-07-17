using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Storage.Streams;

namespace HybridWebView
{
    partial class HybridWebView
    {
        private CoreWebView2Environment? _coreWebView2Environment;

        private Microsoft.UI.Xaml.Controls.WebView2 PlatformWebView => (Microsoft.UI.Xaml.Controls.WebView2)Handler!.PlatformView!;

        private partial async Task InitializeHybridWebView()
        {
            PlatformWebView.WebMessageReceived += Wv2_WebMessageReceived;

            _coreWebView2Environment = await CoreWebView2Environment.CreateAsync();

            await PlatformWebView.EnsureCoreWebView2Async();

            PlatformWebView.CoreWebView2.Settings.AreDevToolsEnabled = EnableWebDevTools;
            PlatformWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        }

        private partial void NavigateCore(string url)
        {
            PlatformWebView.Source = new Uri(new Uri(url).ToString());
        }


        private protected static string GetHeaderString(string contentType, int contentLength) =>
$@"Content-Type: {contentType}
Content-Length: {contentLength}";

        private void Wv2_WebMessageReceived(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            OnMessageReceived(args.TryGetWebMessageAsString());
        }
    }
}
