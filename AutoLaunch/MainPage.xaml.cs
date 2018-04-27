using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AutoLaunch
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            startupCheckBox.IsChecked = Utils.UserSettings.GetValueForKey<bool>(Utils.UserSettings.RUN_APP_AT_STARTUP, false);
        }

        private void StartupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if(startupCheckBox.IsChecked == true)
            {
                Utils.UserSettings.SetValueForKey<bool>(Utils.UserSettings.RUN_APP_AT_STARTUP, true);
            }
            else
            {
                Utils.UserSettings.SetValueForKey<bool>(Utils.UserSettings.RUN_APP_AT_STARTUP, false);
            }
        }
    }
}
