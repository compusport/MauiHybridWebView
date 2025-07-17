using Foundation;
using UIKit;
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

        public partial async Task ClearAllCookiesAsync()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                var store = PlatformWebView.Configuration.WebsiteDataStore.HttpCookieStore;

                var cookies = await store.GetAllCookiesAsync();
                foreach (var c in cookies)
                {
                    await store.DeleteCookieAsync(c);
                }
            }

            //foreach (var domain in HybridWebView.AllRequestsCookies)
            //{
            //    var domainUrl = NSUrl.FromString(domain.Key);
            //    foreach (var cookie in NSHttpCookieStorage.SharedStorage.CookiesForUrl(domainUrl))
            //    {
            //        NSHttpCookieStorage.SharedStorage.DeleteCookie(cookie);
            //        _cookieDomains[domain.Key] = domain.Value - 1;
            //    }
            //}
        }
    }
}
