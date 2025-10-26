using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using ShadowPlayReminderWidget.Models;
using ShadowPlayReminderWidget.Services;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ShadowPlayReminderWidget
{
    public sealed partial class MainPage : Page
    {
        private readonly ShadowPlayMonitor _monitor = new ShadowPlayMonitor();
        private XboxGameBarWidget _widget;
        private XboxGameBarWidgetNotificationManager _notificationManager;
        private XboxGameBarAppTargetTracker _targetTracker;
        private bool _notificationsEnabled = true;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _widget = e.Parameter as XboxGameBarWidget;
            if (_widget != null)
            {
                _notificationManager = new XboxGameBarWidgetNotificationManager(_widget);
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            NotificationsToggle.Toggled += OnNotificationsToggled;
            CheckNowButton.Click += OnCheckNowClicked;
            _monitor.StatusChanged += OnMonitorStatusChanged;

            try
            {
                await _monitor.InitializeAsync();
                await _monitor.CheckStatusAsync(ShadowPlayTrigger.Initialize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShadowPlayReminder] Initialization failed: {ex}");
                await UpdateStatusAsync(new ShadowPlayStatusChangedEventArgs(
                    ShadowPlayState.Error,
                    ex.Message,
                    ShadowPlayTrigger.Initialize,
                    DateTimeOffset.Now,
                    ex,
                    true));
            }

            InitializeTargetTracker();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            NotificationsToggle.Toggled -= OnNotificationsToggled;
            CheckNowButton.Click -= OnCheckNowClicked;
            _monitor.StatusChanged -= OnMonitorStatusChanged;
            _monitor.Dispose();

            if (_targetTracker != null)
            {
                _targetTracker.SettingChanged -= OnTargetTrackerSettingChanged;
                _targetTracker.TargetChanged -= OnTargetTrackerTargetChanged;
                _targetTracker = null;
            }
        }

        private async void OnMonitorStatusChanged(object sender, ShadowPlayStatusChangedEventArgs e)
        {
            await UpdateStatusAsync(e);

            if (ShouldSendReminder(e))
            {
                await TrySendReminderAsync();
            }
        }

        private async Task UpdateStatusAsync(ShadowPlayStatusChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LastCheckedText.Text = $"Last checked at {e.CheckedAt.LocalDateTime:t}";
                StatusText.Text = e.Message;

                switch (e.State)
                {
                    case ShadowPlayState.Active:
                        StatusBorder.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 96, 66));
                        break;
                    case ShadowPlayState.Inactive:
                        StatusBorder.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 120, 34, 34));
                        break;
                    case ShadowPlayState.Error:
                        StatusBorder.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 94, 61, 18));
                        break;
                    default:
                        StatusBorder.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 40, 49));
                        break;
                }
            });
        }

        private bool ShouldSendReminder(ShadowPlayStatusChangedEventArgs status)
        {
            if (!_notificationsEnabled)
            {
                return false;
            }

            if (status.State != ShadowPlayState.Inactive)
            {
                return false;
            }

            if (status.Trigger == ShadowPlayTrigger.Watcher)
            {
                return status.StateChanged;
            }

            return false;
        }

        private async Task TrySendReminderAsync()
        {
            var title = "ShadowPlay is off";
            var content = "Start ShadowPlay before you miss your gameplay highlights.";

            if (_notificationManager != null)
            {
                try
                {
                    var notification = new XboxGameBarWidgetNotificationBuilder(title)
                        .Content(content)
                        .BuildNotification();

                    var result = await _notificationManager.TryShowAsync(notification);
                    if (result == XboxGameBarWidgetNotificationShowResult.Denied)
                    {
                        ShowToastFallback(title, content);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ShadowPlayReminder] Game Bar notification failed: {ex}");
                }
            }

            ShowToastFallback(title, content);
        }

        private static void ShowToastFallback(string title, string content)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements.Item(0).AppendChild(toastXml.CreateTextNode(title));
            stringElements.Item(1).AppendChild(toastXml.CreateTextNode(content));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void OnNotificationsToggled(object sender, RoutedEventArgs e)
        {
            _notificationsEnabled = NotificationsToggle.IsOn;
        }

        private async void OnCheckNowClicked(object sender, RoutedEventArgs e)
        {
            await _monitor.CheckStatusAsync(ShadowPlayTrigger.Manual);
        }

        private void InitializeTargetTracker()
        {
            if (_widget == null)
            {
                return;
            }

            try
            {
                _targetTracker = new XboxGameBarAppTargetTracker(_widget);
                _targetTracker.SettingChanged += OnTargetTrackerSettingChanged;

                if (_targetTracker.Setting == XboxGameBarAppTargetSetting.Enabled)
                {
                    _targetTracker.TargetChanged += OnTargetTrackerTargetChanged;
                    _ = HandleTargetChangedAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShadowPlayReminder] Target tracker unavailable: {ex}");
            }
        }

        private void OnTargetTrackerSettingChanged(XboxGameBarAppTargetTracker sender, object args)
        {
            if (sender.Setting == XboxGameBarAppTargetSetting.Enabled)
            {
                sender.TargetChanged -= OnTargetTrackerTargetChanged;
                sender.TargetChanged += OnTargetTrackerTargetChanged;
                _ = HandleTargetChangedAsync();
            }
            else
            {
                sender.TargetChanged -= OnTargetTrackerTargetChanged;
            }
        }

        private async void OnTargetTrackerTargetChanged(XboxGameBarAppTargetTracker sender, object args)
        {
            await HandleTargetChangedAsync();
        }

        private async Task HandleTargetChangedAsync()
        {
            if (!_notificationsEnabled || _targetTracker == null)
            {
                return;
            }

            if (_targetTracker.Setting != XboxGameBarAppTargetSetting.Enabled)
            {
                return;
            }

            XboxGameBarAppTarget target;
            try
            {
                target = _targetTracker.GetTarget();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShadowPlayReminder] Unable to get target: {ex}");
                return;
            }

            if (target == null || !target.IsGame)
            {
                return;
            }

            var status = await _monitor.CheckStatusAsync(ShadowPlayTrigger.Target);
            if (status.State == ShadowPlayState.Inactive)
            {
                await TrySendReminderAsync();
            }
        }
    }
}
