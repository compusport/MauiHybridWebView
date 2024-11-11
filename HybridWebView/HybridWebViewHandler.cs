using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace HybridWebView
{
    public partial class HybridWebViewHandler : WebViewHandler
    {
        public static IPropertyMapper<IWebView, IWebViewHandler> HybridWebViewMapper = new PropertyMapper<IWebView, IWebViewHandler>(WebViewHandler.Mapper)
        {
#if __ANDROID__
            [nameof(Android.Webkit.WebViewClient)] = MapHybridWebViewClient,
            [nameof(Android.Webkit.WebChromeClient)] = MapHybridWebChromeClient,
#endif
        };

        public HybridWebViewHandler() : base(HybridWebViewMapper, CommandMapper)
        {
        }

        public HybridWebViewHandler(IPropertyMapper? mapper = null, CommandMapper? commandMapper = null)
            : base(mapper ?? HybridWebViewMapper, commandMapper ?? CommandMapper)
        {
        }

#if ANDROID

        private static Android.Webkit.WebView? _platformWebView;
        //private static Android.OS.Bundle? _savedState;

        protected override Android.Webkit.WebView CreatePlatformView()
        {
            //var platformWebView = new MauiHybridWebView(this, Context)
            //{
            //    LayoutParameters = new Android.Views.ViewGroup.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent)
            //};

            //platformWebView.Settings.JavaScriptEnabled = true;
            //platformWebView.Settings.DomStorageEnabled = true;
            //platformWebView.Settings.SetSupportMultipleWindows(true);

            //if (_savedState != null)
            //    platformWebView.RestoreState(_savedState);

            //return platformWebView;
            if (_platformWebView == null)
            {
                _platformWebView = new MauiHybridWebView(this, Context)
                {
                    LayoutParameters = new Android.Views.ViewGroup.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent)
                };

                _platformWebView.Settings.JavaScriptEnabled = true;
                _platformWebView.Settings.DomStorageEnabled = true;
                _platformWebView.Settings.SetSupportMultipleWindows(true);
            }
            else
            {
                var handlerField = typeof(MauiWebView).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                handlerField?.SetValue(_platformWebView, this);
            }

            return _platformWebView;
        }

        protected override void ConnectHandler(Android.Webkit.WebView platformView)
        {
            base.ConnectHandler(platformView);
        }

        public static void MapHybridWebViewClient(IWebViewHandler handler, IWebView webView)
        {
            if (handler is HybridWebViewHandler platformHandler && handler?.PlatformView != null)
            {
                var webViewClient = handler.PlatformView.WebViewClient as AndroidHybridWebViewClient;
                if (webViewClient == null)
                {
                    webViewClient = new AndroidHybridWebViewClient(platformHandler);
                }
                
                handler.PlatformView.SetWebViewClient(webViewClient);

                var handlerField = typeof(AndroidHybridWebViewClient).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                handlerField?.SetValue(webViewClient, platformHandler);
                
                handlerField = typeof(MauiWebViewClient).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                handlerField?.SetValue(webViewClient, new WeakReference<WebViewHandler?>(platformHandler));


                // TODO: There doesn't seem to be a way to override MapWebViewClient() in maui/src/Core/src/Handlers/WebView/WebViewHandler.Android.cs
                // in such a way that it knows of the custom MauiWebViewClient that we're creating. So, we use private reflection to set it on the
                // instance. We might end up duplicating WebView/BlazorWebView anyway, in which case we wouldn't need this workaround.
                var webViewClientField = typeof(WebViewHandler).GetField("_webViewClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);

                // Starting in .NET 8.0 the private field is gone and this call isn't necessary, so we only set if it needed
                webViewClientField?.SetValue(handler, webViewClient);

            }
        }

		public static void MapHybridWebChromeClient(IWebViewHandler handler, IWebView webView)
		{
            if (handler?.PlatformView == null)
                return;

            if (handler is HybridWebViewHandler platformHandler && handler.PlatformView.WebChromeClient is not HybridWebChromeClient)
            {
                handler.PlatformView.SetWebChromeClient(new HybridWebChromeClient(platformHandler));
            }
            else if (handler is WebViewHandler viewHandler)
            {
                var handlerField = typeof(Android.Webkit.WebChromeClient).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                handlerField?.SetValue(handler.PlatformView.WebChromeClient, viewHandler);
            }
		}

        protected override void DisconnectHandler(Android.Webkit.WebView platformView)
		{
            //_savedState?.Dispose();

            //_savedState = new Android.OS.Bundle();
            //platformView.SaveState(_savedState);

            //Do not Disconnect to prevent breaking the PlatformView
            //base.DisconnectHandler(platformView);
        }
#endif
    }
}
