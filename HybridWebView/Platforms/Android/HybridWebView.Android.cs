using Android.Views;
using Android.Webkit;
using Java.Interop;
using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    partial class HybridWebView
    {
        private static readonly string AppHostAddress = "0.0.0.0";

        /// <summary>
        /// Gets the application's base URI. Defaults to <c>https://0.0.0.0/</c>
        /// </summary>
        private static readonly string AppOrigin = $"https://{AppHostAddress}/";

        internal static readonly Uri AppOriginUri = new(AppOrigin);

        private HybridWebViewJavaScriptInterface? _javaScriptInterface;

        private MauiHybridWebView PlatformWebView => (MauiHybridWebView)Handler?.PlatformView!;


        private partial Task InitializeHybridWebView()
        {
            // Note that this is a per-app setting and not per-control, so if you enable
            // this, it is enabled for all Android WebViews in the app.
            AWebView.SetWebContentsDebuggingEnabled(enabled: EnableWebDevTools);

            if (PlatformWebView == null)
                return Task.CompletedTask;

            PlatformWebView.Settings.JavaScriptEnabled = true;
            PlatformWebView.OverScrollMode = OverScrollMode.Never;
            //PlatformWebView.Settings.SetAppCacheEnabled(true);
            PlatformWebView.Settings.BuiltInZoomControls = true;
            PlatformWebView.Settings.DisplayZoomControls = false;
            //Fixe texte du bracket si zoomed out
            PlatformWebView.Settings.MinimumFontSize = 1;

            PlatformWebView.Settings.SetGeolocationEnabled(true);
            //e.Settings.SetGeolocationDatabasePath(e.Context.FilesDir.Path);

            //Fixe accessibility size increase
            PlatformWebView.Settings.TextZoom = 100;

            if (_javaScriptInterface == null)
            {
                _javaScriptInterface = new HybridWebViewJavaScriptInterface(this);
                PlatformWebView.AddJavascriptInterface(_javaScriptInterface, "hybridWebViewHost");
            }

            return Task.CompletedTask;
        }

        private partial void NavigateCore(string url)
        {
            var cookieManager = Android.Webkit.CookieManager.Instance;
            if (cookieManager != null)
            {
                foreach (var item in HybridWebView.AllRequestsCookies)
                {
                    var val = $"{item.Key}={item.Value}";
                    //var co = cookies.FirstOrDefault(o => o.Name == item.Key);
                    //if (!cookies.Any(o => item.Key == o.Name && item.Value == o.Value && o.Path == "/"))
                    //{
                    //    System.Diagnostics.Debug.WriteLine($"Adding cookie {val}");
                    //    Cookies.Add(new System.Net.Cookie(item.Key, item.Value, "/", AppOriginUri.Host) { Expires = DateTime.Now.AddYears(1) });
                    //}
                    cookieManager.SetCookie("/", val);
                }
                cookieManager.Flush();
            }

            PlatformWebView.LoadUrl(new Uri(AppOriginUri, url).ToString(), HybridWebView.AdditionalHeaders);
        }

        public partial Task ClearAllCookiesAsync()
        {
            var cookieManager = Android.Webkit.CookieManager.Instance;
            if (cookieManager == null)
                return Task.CompletedTask;

            // Ne pas faire car ca flush les cookies de antiforgery
            // cookieManager.RemoveAllCookies(null);

            foreach (var item in HybridWebView.AllRequestsCookies)
            {
                var val = $"{item.Key}={item.Value}";
                cookieManager.SetCookie("/", val);
            }

            cookieManager.RemoveSessionCookies(null); // Use RemoveSessionCookies instead of RemoveExpiredCookie
            cookieManager.Flush();

            PlatformWebView.ClearCache(true);

            return Task.CompletedTask;
        }

        private sealed class HybridWebViewJavaScriptInterface : Java.Lang.Object
        {
            private readonly HybridWebView _hybridWebView;

            public HybridWebViewJavaScriptInterface(HybridWebView hybridWebView)
            {
                _hybridWebView = hybridWebView;
            }

            [JavascriptInterface]
            [Export("sendMessage")]
            public void SendMessage(string message)
            {
                _hybridWebView.OnMessageReceived(message);
            }
        }
    }
}
