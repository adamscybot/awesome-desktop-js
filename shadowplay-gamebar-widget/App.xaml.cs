using System;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ShadowPlayReminderWidget
{
    sealed partial class App : Application
    {
        private XboxGameBarWidget _widget;
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            XboxGameBarWidgetActivatedEventArgs widgetArgs = null;

            if (args.Kind == ActivationKind.Protocol)
            {
                if (args is IProtocolActivatedEventArgs protocolArgs &&
                    string.Equals(protocolArgs.Uri.Scheme, "ms-gamebarwidget", StringComparison.OrdinalIgnoreCase))
                {
                    widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                }
            }

            if (widgetArgs != null && widgetArgs.IsLaunchActivation)
            {
                var rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;

                _widget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
                rootFrame.Navigate(typeof(MainPage), _widget);

                Window.Current.Closed += OnWidgetWindowClosed;
                Window.Current.Activate();
            }
        }

        private void OnWidgetWindowClosed(object sender, CoreWindowEventArgs e)
        {
            _widget = null;
            Window.Current.Closed -= OnWidgetWindowClosed;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), null);
            }

            Window.Current.Activate();
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            _widget = null;
            deferral.Complete();
        }
    }
}
