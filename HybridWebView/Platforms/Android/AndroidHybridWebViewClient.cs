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

        public override WebResourceResponse? ShouldInterceptRequest(AWebView? view, IWebResourceRequest? request)
        {
            var fullUrl = request?.Url?.ToString();
            if (fullUrl == null || request == null || !request.IsForMainFrame)
                return base.ShouldInterceptRequest(view, request);

            var requestUri = QueryStringHelper.RemovePossibleQueryString(fullUrl);

            System.Diagnostics.Debug.WriteLine($"ShouldInterceptRequest :{request.IsForMainFrame} {request.IsRedirect} {fullUrl}");

            //if (!requestUri.ToLower().StartsWith(HybridWebView.AppOrigin.ToLower())
            //    || requestUri.ToLower().Contains("/signalr/")
            //    || requestUri.ToLower().Contains("/bundles/")
            //    || (!request.IsForMainFrame && !request.IsRedirect)
            //    )
            //{
            //    System.Diagnostics.Debug.WriteLine($"Skipping ShouldInterceptRequest : {requestUri}");
            //    return base.ShouldInterceptRequest(view, request);
            //}

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

            if (new Uri(requestUri) is Uri uri && HybridWebView.AppOriginUri.IsBaseOf(uri))
            {
                var relativePath = HybridWebView.AppOriginUri.MakeRelativeUri(uri).ToString().Replace('/', '\\');

                string contentType;
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = webView.MainFile;
                    contentType = "text/html";
                }
                else
                {
                    var requestExtension = Path.GetExtension(relativePath);
                    contentType = requestExtension switch
                    {
                        ".htm" or ".html" => "text/html",
                        ".js" => "application/javascript",
                        ".css" => "text/css",
                        _ => "text/plain",
                    };
                }

                Stream? contentStream = null;

                // Check to see if the request is a proxy request.
                if (relativePath == HybridWebView.ProxyRequestPath)
                {
                    var args = new HybridWebViewProxyEventArgs(fullUrl);

                    // TODO: Don't block async. Consider making this an async call, and then calling DidFinish when done
                    webView.OnProxyRequestMessage(args).Wait();

                    if (args.ResponseStream != null)
                    {
                        contentType = args.ResponseContentType ?? "text/plain";
                        contentStream = args.ResponseStream;
                    }
                }

                if (contentStream == null)
                {
                    contentStream = KnownStaticFileProvider.GetKnownResourceStream(relativePath!);
                }

                if (contentStream is null)
                {
                    var assetPath = Path.Combine(((HybridWebView)_handler.VirtualView).HybridAssetRoot!, relativePath!);
                    contentStream = PlatformOpenAppPackageFile(assetPath);
                }

                if (contentStream is null)
                {
                    var notFoundContent = "Resource not found (404)";

                    var notFoundByteArray = Encoding.UTF8.GetBytes(notFoundContent);
                    var notFoundContentStream = new MemoryStream(notFoundByteArray);

                    return new WebResourceResponse("text/plain", "UTF-8", 404, "Not Found", GetHeaders("text/plain"), notFoundContentStream);
                }
                else
                {
                    // TODO: We don't know the content length because Android doesn't tell us. Seems to work without it!
                    return new WebResourceResponse(contentType, "UTF-8", 200, "OK", GetHeaders(contentType), contentStream);
                }
            }
            else
            {
                return base.ShouldInterceptRequest(view, request);
            }
        }
        private Stream? PlatformOpenAppPackageFile(string filename)
        {
            filename = PathUtils.NormalizePath(filename);

            try
            {
                return _handler.Context.Assets?.Open(filename);
            }
            catch (Java.IO.FileNotFoundException)
            {
                return null;
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
