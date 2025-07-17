using Foundation;
using WebKit;

namespace HybridWebView
{
    partial class HybridWebView
    {
        private WKWebView PlatformWebView => (WKWebView)Handler!.PlatformView!;

        private partial Task InitializeHybridWebView()
        {
            return Task.CompletedTask;
        }

        private partial void NavigateCore(string url)
        {
            using var nsUrl = new NSUrl(new Uri(url).ToString());
            using var request = new NSUrlRequest(nsUrl);

            PlatformWebView.LoadRequest(request);
        }
    }
}
