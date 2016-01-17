using MusicConcept.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : AppPage
    {
        private ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();

        public AboutPage()
        {
            this.InitializeComponent();

            var appVersion = Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor;
            VersionTextBlock.Text = string.Format(ResourceLoader.GetForViewIndependentUse().GetString("VersionFormat"), appVersion);
            resourceLoader = ResourceLoader.GetForViewIndependentUse();

            AppTitleTextBlock.Text = resourceLoader.GetString("AppName");

        }

        private void MailButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMailForm(resourceLoader);
        }

        public static async void ShowMailForm(ResourceLoader resourceLoader)
        {
            var appVersion = Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor;
            
            var mail = new EmailMessage();
            mail.Subject = string.Format(resourceLoader.GetString("SupportMailSubject"), resourceLoader.GetString("AppName"));
            mail.Body = string.Format(resourceLoader.GetString("SupportMailBody"), resourceLoader.GetString("AppName"), appVersion);

            mail.To.Add(new EmailRecipient()
            {
                Address = "support@philbi.de"
            });

            await EmailManager.ShowComposeNewEmailAsync(mail);
        }

        private async void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/JulianMH/music-3"));
        }
    }
}
