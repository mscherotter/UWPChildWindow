using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PreviewWindowDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CoreDispatcher _previewDispatcher;
        private bool _isDraggingPreview;
        private UserInteractionMode _currentUserInteractionMode;

        public MainPage()
        {
            var viewSettings = UIViewSettings.GetForCurrentView();

            _currentUserInteractionMode = viewSettings.UserInteractionMode;

            this.InitializeComponent();

            CoreWindow.GetForCurrentThread().SizeChanged += MainPage_SizeChanged;

            PreviewPopupContents.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            PreviewPopupContents.ManipulationStarted += PreviewPopup_ManipulationStarted;
            PreviewPopupContents.ManipulationDelta += PreviewPopup_ManipulationDelta;
            PreviewPopupContents.ManipulationCompleted += PreviewPopup_ManipulationCompleted;

            ApplicationView.GetForCurrentView().Title = "Main View";

            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var applicationView = ApplicationView.GetForCurrentView();

            applicationView.Consolidated += MainPage_Consolidated;
        }

        private async void MainPage_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Main page consolidated: " + args.IsUserInitiated);

            if (args.IsUserInitiated)
            {
                // you could so saving here

                await HidePreviewWindowAsync(true);
            }
        }

        private void PreviewPopup_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_isDraggingPreview)
            {
                e.Handled = true;
                _isDraggingPreview = false;
            }
        }

        private void PreviewPopup_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (_isDraggingPreview)
            {
                PreviewPopup.HorizontalOffset += e.Delta.Translation.X;
                PreviewPopup.VerticalOffset += e.Delta.Translation.Y;
                e.Handled = true;
            }
        }

        private void PreviewPopup_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _isDraggingPreview = true;

            e.Handled = true;
        }

        // this is called when switching to tablet mode
        private async void MainPage_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            var viewSettings = UIViewSettings.GetForCurrentView();

            if (viewSettings.UserInteractionMode == UserInteractionMode.Touch && _currentUserInteractionMode == UserInteractionMode.Mouse)
            {
                await HidePreviewWindowAsync(false);

                PreviewPopup.IsOpen = true;
            }

            _currentUserInteractionMode = viewSettings.UserInteractionMode;
        }

        private async System.Threading.Tasks.Task HidePreviewWindowAsync(bool shutdown)
        {
            if (_previewDispatcher == null)
            {
                return;
            }

            await _previewDispatcher.RunAsync(CoreDispatcherPriority.High, delegate
            {
                _previewDispatcher = null;

                _previewDispatcher = null;

                // close the preview window
                CoreWindow.GetForCurrentThread().Close();

                if (shutdown)
                {
                    // do any saving here

                    CoreApplication.Exit();
                }
            });
        }

        private async void OnFloat(object sender, RoutedEventArgs e)
        {
            PreviewPopup.IsOpen = false;

            var view = CoreApplication.CreateNewView();

            _previewDispatcher = view.Dispatcher;

            SplitView.IsPaneOpen = false;

            var anchorViewId = ApplicationView.GetForCurrentView().Id;

            await view.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async delegate
            {
                var viewId = ApplicationView.GetApplicationViewIdForWindow(CoreWindow.GetForCurrentThread());

                var frame = new Frame();

                frame.Navigate(typeof(PreviewPage));

                Window.Current.Content = frame;

                Window.Current.Activate();

                var applicationView = ApplicationView.GetForCurrentView();
                
                applicationView.Consolidated += PreviewWindow_Consolidated;

                applicationView.Title = "Preview";

                var shown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(viewId, ViewSizePreference.UseMinimum, anchorViewId, ViewSizePreference.Default);

                applicationView.SetPreferredMinSize(new Windows.Foundation.Size(320, 500));

                bool resized = applicationView.TryResizeView(new Windows.Foundation.Size(320, 500));

                System.Diagnostics.Debug.WriteLine($"Shown: {shown}, Resized: {resized}");
            });
        }

        private async void PreviewWindow_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"Consolidated: user initiated: {args.IsUserInitiated}");

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                OpenPaneButton.IsChecked = false;
            });
        }

        private void ShowPreview(object sender, RoutedEventArgs e)
        {
            this.PreviewPopup.IsOpen = true;
            SplitView.IsPaneOpen = false;

        }

        private async void HidePreview(object sender, RoutedEventArgs e)
        {
            this.PreviewPopup.IsOpen = false;
            SplitView.IsPaneOpen = false;
            await HidePreviewWindowAsync(false);
        }

        private void ClosePreviewPopup(object sender, RoutedEventArgs e)
        {
            PreviewPopup.IsOpen = false;
        }

        private void OpenPane(object sender, RoutedEventArgs e)
        {
            PreviewPopup.IsOpen = false;
            SplitView.IsPaneOpen = true;
        }
    }
}
