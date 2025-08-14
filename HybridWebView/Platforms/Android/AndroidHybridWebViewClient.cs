using Android.Content;
using Android.Content.PM;
using Android.Net.Http;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Java.Time;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Text;
using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    public class AndroidHybridWebViewClient : MauiWebViewClient
    {
        private HybridWebViewHandler _handler;

        public AndroidHybridWebViewClient(HybridWebViewHandler handler) : base(handler)
        {
            _handler = handler;
        }

        private string? _url;
        public override bool ShouldOverrideUrlLoading(AWebView? view, IWebResourceRequest? request)
        {
            _url = request.Url.ToString();
            System.Diagnostics.Debug.WriteLine($"HybridWebView ShouldOverrideUrlLoading :{request.IsForMainFrame} {request.IsRedirect} {request.Url}");
            return base.ShouldOverrideUrlLoading(view, request);
        }

        public override bool ShouldOverrideUrlLoading(AWebView? view, string? url)
        {
            _url = url;
            System.Diagnostics.Debug.WriteLine($"HybridWebView ShouldOverrideUrlLoading :{url}");
            return base.ShouldOverrideUrlLoading(view, url);
        }

        public override void OnPageStarted(AWebView? view, string? url, Android.Graphics.Bitmap? favicon)
        {
            if (url == "about:blank")
                return;

            if (BeginSkipIfRestoring())
                return;

            System.Diagnostics.Debug.WriteLine($"HybridWebView OnPageStarted :{url}");
            //if (_url != url)
            base.OnPageStarted(view, url, favicon);

            _url = null;
        }
        public override void OnPageFinished(AWebView? view, string? url)
        {
            if (url == "about:blank")
                return;

            if (EndSkipIfRestoring())
                return;                              // swallow synthetic Finished

            base.OnPageFinished(view, url);
        }


        bool _skipNext;                               // local flag, survives rotations
        bool BeginSkipIfRestoring()
        {
            if (_handler.IsRestoringState && !_skipNext)
            {
                _skipNext = true;                    // skip this pair
                return true;
            }
            return false;
        }

        bool EndSkipIfRestoring()
        {
            if (_skipNext)
            {
                _skipNext = false;
                _handler.FinishRestore();            // reset flag in handler
                return true;
            }
            return false;
        }

        public override WebResourceResponse? ShouldInterceptRequest(AWebView? view, IWebResourceRequest? request)
        {
            var fullUrl = request?.Url?.ToString();
            if (fullUrl == null || request == null || !request.IsForMainFrame)
                return base.ShouldInterceptRequest(view, request);

            var requestUri = QueryStringHelper.RemovePossibleQueryString(fullUrl);

            System.Diagnostics.Debug.WriteLine($"HybridWebView ShouldInterceptRequest :{request.IsForMainFrame} {request.IsRedirect} {fullUrl}");

            //if (!requestUri.ToLower().StartsWith(HybridWebView.AppOrigin.ToLower())
            //    || requestUri.ToLower().Contains("/signalr/")
            //    || requestUri.ToLower().Contains("/bundles/")
            //    || (!request.IsForMainFrame && !request.IsRedirect)
            //    )
            //{
            //    System.Diagnostics.Debug.WriteLine($"Skipping ShouldInterceptRequest : {requestUri}");
            //    return base.ShouldInterceptRequest(view, request);
            //}

            try
            {
                var webView = (HybridWebView)_handler.VirtualView;
                var cookieManager = Android.Webkit.CookieManager.Instance;

                if (cookieManager == null || webView == null)
                    return base.ShouldInterceptRequest(view, request);

                //var cookies = hybridWebView.Cookies.GetAllCookies();
                foreach (var item in HybridWebView.AllRequestsCookies)
                {
                    var val = $"{item.Key}={item.Value}";
                    //var co = cookies.FirstOrDefault(o => o.Name == item.Key);
                    //if (!cookies.Any(o => item.Key == o.Name && item.Value == o.Value && o.Path == "/"))
                    //{
                    //    System.Diagnostics.Debug.WriteLine($"Adding cookie {val}");
                    //    hybridWebView.Cookies.Add(new System.Net.Cookie(item.Key, item.Value, "/", request.Url.Host) { Expires = DateTime.Now.AddYears(1) });
                    //}
                    cookieManager.SetCookie("/", val);
                }

                return base.ShouldInterceptRequest(view, request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HybridWebView Error getting cookie manager or webview. " + ex.ToString());
                return base.ShouldInterceptRequest(view, request);
            }
        }

        private protected static IDictionary<string, string> GetHeaders(string contentType) =>
            new Dictionary<string, string> {
                { "Content-Type", contentType },
            };
    }

    public class HybridWebChromeClient : MauiWebChromeClient
    {
        public HybridWebChromeClient(IWebViewHandler handler) : base(handler)
        {

        }
        public override bool OnCreateWindow(AWebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
        {
            return base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
        }

        public override bool OnConsoleMessage(ConsoleMessage? consoleMessage)
        {
            return base.OnConsoleMessage(consoleMessage);
        }

        public override bool OnJsConfirm(AWebView? view, string? url, string? message, JsResult? result)
        {
            return base.OnJsConfirm(view, url, message, result);
        }

        public override bool OnJsPrompt(AWebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
        {
            return base.OnJsPrompt(view, url, message, defaultValue, result);
        }

        public override bool OnJsAlert(AWebView? view, string? url, string? message, JsResult? result)
        {
            return base.OnJsAlert(view, url, message, result);
        }
    }

    public class MauiHybridWebView : MauiWebView
    {
        public MauiHybridWebView(WebViewHandler handler, Context context) : base(handler, context) { }

        public override void LoadUrl(string url)
        {
            if (url == "about:blank")
                return;

            base.LoadUrl(url);
        }
        public override void LoadUrl(string url, IDictionary<string, string> additionalHttpHeaders)
        {
            if (url == "about:blank")
                return;

            base.LoadUrl(url, additionalHttpHeaders);
        }
    }
}
