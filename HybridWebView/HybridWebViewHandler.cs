using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Reflection;

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

        protected override Android.Webkit.WebView CreatePlatformView()
        {
            if (_platformWebView != null)
            {
                // Detach from any previous parent and hand it to this handler
                (_platformWebView.Parent as Android.Views.ViewGroup)?.RemoveView(_platformWebView);

                var handlerField = typeof(MauiWebView).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                handlerField?.SetValue(_platformWebView, this);

                return _platformWebView;
            }
            _platformWebView = new MauiHybridWebView(this, Context)
            {
                LayoutParameters = new Android.Views.ViewGroup.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent)
            };

            _platformWebView.Settings.JavaScriptEnabled = true;
            _platformWebView.Settings.DomStorageEnabled = true;
            _platformWebView.Settings.SetSupportMultipleWindows(true);

            if (OperatingSystem.IsAndroidVersionAtLeast(23) && Context?.ApplicationInfo?.Flags.HasFlag(Android.Content.PM.ApplicationInfoFlags.HardwareAccelerated) == false)
            {
                _platformWebView.SetLayerType(Android.Views.LayerType.Software, null);
            }


            return _platformWebView;
        }

        WebViewSource? _cachedSource;     // keep it so databinding isn’t broken
        string _cachedStartPath;

#if ANDROID
        public override void SetVirtualView(IView view)
        {
            bool reattach = _platformWebView != null;   // we’re re-using the old WebView

            if (reattach && view is HybridWebView wv)
            {
                _cachedSource = wv.Source;  // ① remember the original value
                _cachedStartPath = wv.StartPath;
                wv.Source = null;       // ② hide it from ProcessSourceWhenReady
                wv.StartPath = string.Empty;
            }

            base.SetVirtualView(view);      // MAUI won’t try to navigate

            if (reattach && view is HybridWebView wv2)
            {
                //wv2.Source = _cachedSource;  // Can't reset the Source since it navigates
                wv2.StartPath = _cachedStartPath;
            }
        }
#endif

        protected override void ConnectHandler(Android.Webkit.WebView platformView)
        {
            base.ConnectHandler(platformView);
        }

        public static void MapHybridWebViewClient(IWebViewHandler handler, IWebView webView)
        {
            if (handler is not HybridWebViewHandler platformHandler || handler.PlatformView is null)
                return;

            var pv = handler.PlatformView;

            if (pv.WebViewClient is AndroidHybridWebViewClient existing)
            {
                // --- 1. update the strong reference inside our own class
                typeof(AndroidHybridWebViewClient)
                   .GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance)!
                   .SetValue(existing, platformHandler);

                // --- 2. update the weak reference inside the base class
                typeof(MauiWebViewClient)
                   .GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)!
                   .SetValue(existing, new WeakReference<WebViewHandler?>(platformHandler));

                return; // client already present, nothing else to do
            }

            // Otherwise attach a fresh client (first time only)
            var client = new AndroidHybridWebViewClient(platformHandler);
            pv.SetWebViewClient(client);

            // wire the base-class field once
            typeof(MauiWebViewClient)
               .GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)!
               .SetValue(client, new WeakReference<WebViewHandler?>(platformHandler));
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
            (platformView.Parent as Android.Views.ViewGroup)?.RemoveView(platformView);

        }
#endif
    }
}
