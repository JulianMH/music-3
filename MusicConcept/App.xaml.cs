using MusicConcept.Common;
using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace MusicConcept
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;
        
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

            UnhandledException += App_UnhandledException;
        }

        void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if(rootFrame != null)
            {
                ErrorPage.Exception = e.Exception;
                rootFrame.Navigate(typeof(ErrorPage));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active.
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page.
                rootFrame = new Frame();

                // Associate the frame with a SuspensionManager key.
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate.
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        // Something went wrong restoring state.
                        // Assume there is no state and continue.
                    }
                }

                // Place the frame in the current Window.
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter.
                if (!rootFrame.Navigate(typeof(PivotPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active.
            Window.Current.Activate();

            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            Song.UnknownArtist = resourceLoader.GetString("SongUnknownArtist");
            
            var appVersion = Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor;
            if (ApplicationSettings.AppLastStartupVersion.Read() != appVersion)
            {
                ApplicationSettings.AppLastStartupVersion.Save(appVersion);
            }

            var reviewStartupNumber = ApplicationSettings.ReviewReminderStartupNumber.Read();
            if (reviewStartupNumber < 10)
                ApplicationSettings.ReviewReminderStartupNumber.Save(reviewStartupNumber + 1);
            else if(reviewStartupNumber == 10)
            {
                ApplicationSettings.ReviewReminderStartupNumber.Save(11);


                var messageDialog = new MessageDialog(string.Format(resourceLoader.GetString("ReviewMessageDialog"), resourceLoader.GetString("AppName")), 
                        string.Format(resourceLoader.GetString("ReviewMessageDialogTitle"), resourceLoader.GetString("AppName")));

                messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("ReviewMessageDialogYes"), async sender => {

                    var messageContinuedDialog = new MessageDialog(resourceLoader.GetString("ReviewMessageContinuedDialog"),
                            string.Format(resourceLoader.GetString("ReviewMessageDialogTitle"), resourceLoader.GetString("AppName")));

                    messageContinuedDialog.Commands.Add(new UICommand(resourceLoader.GetString("ReviewMessageReview"), async sender2 => await Launcher.LaunchUriAsync(new Uri("ms-windows-store:reviewapp?appid=" + CurrentApp.AppId))));
                    messageContinuedDialog.Commands.Add(new UICommand(resourceLoader.GetString("ReviewMessageMail"), sender2 => AboutPage.ShowMailForm(resourceLoader)));

                    await messageContinuedDialog.ShowAsync();
                    
                }));
                messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("ReviewMessageDialogNo")));

                await messageDialog.ShowAsync();
            }
        }

        private async void ShowOpenSourceMessage()
        {
            if (DateTime.Now < new DateTime(2016, 3, 1))
            {
                try
                {
                    var file = await ApplicationData.Current.LocalFolder.GetFileAsync("HideOpenSourceMessage.txt");
                }
                catch (FileNotFoundException)
                {
                    await ApplicationData.Current.LocalFolder.CreateFileAsync("HideOpenSourceMessage.txt");

                    var resourceLoader = ResourceLoader.GetForViewIndependentUse();
                    var githubLink = "https://github.com/JulianMH/music-3";
                    MessageDialog messageDialog = new MessageDialog(String.Format(resourceLoader.GetString("OpenSourceMessage"), resourceLoader.GetString("AppName"), githubLink));
                    messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("OpenSourceOK")));
                    messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("OpenSourceLookAt"), async action => await Windows.System.Launcher.LaunchUriAsync(new Uri(githubLink))));
                    await messageDialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
