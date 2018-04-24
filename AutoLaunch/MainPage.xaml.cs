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
        private MediaPlayer m_mediaPlayer = null;
        private MediaPlayer m_localMediaPlayer = null;
        private MediaBinder m_localMediaBinder = null;
        private MediaSource m_localMediaSource = null;
        private Deferral m_deferral = null;
        private DispatcherTimer m_timer = null;
        private Audio.AudioOutput m_audioOutput;
        private Audio.AudioInput m_audioInput;
        private string SessionConnectedTaskName = "SessionConnectedTrigger";
        private string SessionConnectedTaskEntryPoint = "BackgroundTask.SessionConnectedTrigger";

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await StartAudio();
            await StartRecording();
            //UnregisterBackgroundTask();
            //RegisterBackgroundTask();
        }

        public async Task StartRecording()
        {
            if (m_audioInput != null)
            {
                m_audioInput.Stop();
                m_audioInput = null;
            }

            if (m_audioOutput != null)
            {
                m_audioOutput.Stop();
                m_audioOutput = null;
            }

            m_audioOutput = new Audio.AudioOutput();
            m_audioInput = new Audio.AudioInput();
            m_audioInput.OnAudioInput += OnAudioInput;
            await m_audioOutput.Start();
            await m_audioInput.Start();
        }

        public async Task StartAudio()
        {
            BookNetworkForBackground();
              
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer = null;
            }

            m_timer = new DispatcherTimer();
            if (m_timer != null)
            {
                m_timer.Interval = TimeSpan.FromMilliseconds(5000);
                m_timer.Tick += Timer_Tick;
                m_timer.Start();
            }
            await PlayAudio();
        }

        private void OnAudioInput(NAudio.Wave.IWaveBuffer data)
        {
            m_audioOutput.Send(data.ByteBuffer);
        }

        public bool BookNetworkForBackground()
        {
            bool result = false;
            try
            {
                var smtc = SystemMediaTransportControls.GetForCurrentView();
                smtc.IsPlayEnabled = true;
                smtc.IsPauseEnabled = true;

                if (m_localMediaBinder == null)
                {
                    m_localMediaBinder = new Windows.Media.Core.MediaBinder();
                    if (m_localMediaBinder != null)
                    {
                        m_localMediaBinder.Binding += LocalMediaBinder_Binding;
                    }
                }
                if (m_localMediaSource == null)
                {
                    m_localMediaSource = Windows.Media.Core.MediaSource.CreateFromMediaBinder(m_localMediaBinder);
                }
                if (m_localMediaPlayer == null)
                {
                    m_localMediaPlayer = new Windows.Media.Playback.MediaPlayer();
                    if (m_localMediaPlayer != null)
                    {
                        m_localMediaPlayer.CommandManager.IsEnabled = false;
                        m_localMediaPlayer.Source = m_localMediaSource;
                        result = true;
                        Debug.WriteLine("Booking network for Background task successful");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception while booking network for Background task: Exception: " + ex.Message);
            }
            Debug.WriteLine("Booking network for Background task failed");
            return result;
        }

        // Method used to keep the network on while the application is in background
        private void LocalMediaBinder_Binding(Windows.Media.Core.MediaBinder sender, Windows.Media.Core.MediaBindingEventArgs args)
        {
            m_deferral = args.GetDeferral();
            Debug.WriteLine("Booking network for Background task running...");
        }

        /// <summary>
        /// This method is called every second.
        /// </summary>
        private async void Timer_Tick(object sender, object e)
        {
            await PlayAudio();
        }

        private async Task PlayAudio()
        {
            await StartRecording();
#if false
            m_mediaPlayer = null;
            m_mediaPlayer = new Windows.Media.Playback.MediaPlayer();

            m_mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("https://ccrma.stanford.edu/~jos/mp3/gtr-nylon22.mp3"));
            m_mediaPlayer.Play();
#endif
        }

        private async void recordButton_Click(object sender, RoutedEventArgs e)
        {
            await StartRecording();
        }

        private void RegisterBackgroundTask()
        {
            var requestTask = BackgroundExecutionManager.RequestAccessAsync();
            var builder = new BackgroundTaskBuilder();
            builder.Name = SessionConnectedTaskName;
            builder.TaskEntryPoint = SessionConnectedTaskEntryPoint;
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.SessionConnected, false));
            var task = builder.Register();

            //status.Text = "Backgrond Task registered. Close app and change the timezone settings to see the app launched to foreground from a background task.";
        }

        private void UnregisterBackgroundTask()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == SessionConnectedTaskName)
                {
                    task.Value.Unregister(true);
                    break;
                }
            }
        }

    }
}
