﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Showcase
{
    public sealed partial class SlideShow : Page
    {
        private int currentImage = 0;
        private int imageTime = 10000;
        private ThreadPoolTimer startFadeTimer;
        private DispatcherTimer _hideControlsTimer;
        private CoreDispatcher uiDispatcher;
        private OneDriveItemController _oneDrive = new OneDriveItemController();
        private List<OneDriveItemModel> _images;
        private VoiceCommand _voiceCommand = new VoiceCommand();
        private Dictionary<string, RoutedEventHandler> _voiceCallbacks;

        public SlideShow()
        {
            this.InitializeComponent();
            uiDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            SetBackground(Color.FromArgb(255, 255, 255, 255));

            _voiceCallbacks = new Dictionary<string, RoutedEventHandler>()
            {
                { "Toggle", Toggle_Click },
                { "Previous", Previous_Click },
                { "Next", Next_Click },
            };

            _hideControlsTimer = new DispatcherTimer();
            _hideControlsTimer.Tick += (object timer, object args) =>
            {
                SlideShowControls.Visibility = Visibility.Collapsed;
            };
            _hideControlsTimer.Interval = TimeSpan.FromSeconds(10);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _oneDrive.InitAsync();
            _images = await _oneDrive.GetImagesAsync(null);

            if (_images.Count == 0)
            {
                News.Text = "No images found in user's OneDrive.";
                SlideShowControls.Visibility = Visibility.Collapsed;
                return;
            }

            _hideControlsTimer.Start();
            ForegroundImage.Source = BackgroundImage.Source = await LoadImage(_images[0].Id);
            Play();

            _voiceCommand.AddCommands(_voiceCallbacks);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _hideControlsTimer.Stop();
            _voiceCommand.RemoveCommands(_voiceCallbacks);
        }

        private void OnPointerMoved(object sender, RoutedEventArgs e)
        {
            if (_images != null && _images.Count != 0)
            {
                _hideControlsTimer.Stop();
                _hideControlsTimer.Start();
                SlideShowControls.Visibility = Visibility.Visible;
            }
        }

        private async void StartFadeAnimation(ThreadPoolTimer timer = null)
        {
            await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                ForegroundImage.Source = BackgroundImage.Source;
                ForegroundImage.Opacity = 1;
                currentImage = (currentImage + 1) % _images.Count;
                BackgroundImage.Source = await LoadImage(_images[currentImage].Id);
                SlideShowFade.Begin();
            });
        }

        private void Play()
        {
            startFadeTimer = ThreadPoolTimer.CreatePeriodicTimer(StartFadeAnimation, TimeSpan.FromMilliseconds(imageTime));
        }

        private void Pause()
        {
            startFadeTimer?.Cancel();
            startFadeTimer = null;
        }

        private void ResetTimer()
        {
            if (startFadeTimer != null)
            {
                Pause();
                Play();
            }
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (startFadeTimer != null)
            {
                PlayPauseButton.Content = "\xE768";
                Pause();
            }
            else
            {
                PlayPauseButton.Content = "\xE769";
                Play();
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            ResetTimer();
            currentImage = (currentImage + _images.Count - 2) % _images.Count;
            StartFadeAnimation();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            ResetTimer();
            StartFadeAnimation();
        }

        private void SetTime_Click(object sender, RoutedEventArgs e)
        {
            SetTimePopup.IsOpen = true;
        }

        private void SetTimePopup_ItemClick(object sender, ItemClickEventArgs e)
        {
            TextBlock item = e.ClickedItem as TextBlock;

            switch (item.Text)
            {
                case "Faster transition":
                    Debug.WriteLine("Prev fade time " + FadeAnimation.Duration.TimeSpan.Milliseconds);
                    ForegroundFadeAnimation.Duration = FadeAnimation.Duration = TimeSpan.FromMilliseconds(FadeAnimation.Duration.TimeSpan.TotalMilliseconds / 2);
                    Debug.WriteLine("Fade time " + FadeAnimation.Duration.TimeSpan.Milliseconds);
                    break;

                case "Faster image switch":
                    imageTime /= 2;
                    Debug.WriteLine("Switch time " + imageTime);
                    Pause();
                    Play();
                    break;

                default:
                    Debug.WriteLine("Unknown popup option " + item.Text + "selected");
                    break;
            }
        }

        private void SetStretchType_Click(object sender, RoutedEventArgs e)
        {
            SetStretchTypePopup.IsOpen = true;
        }

        private void SetStretchTypePopup_ItemClick(object sender, ItemClickEventArgs e)
        {
            TextBlock item = e.ClickedItem as TextBlock;
            Stretch stretch;

            switch (item.Text)
            {
                case "None":
                    stretch = Stretch.None;
                    break;

                case "Stretch":
                    stretch = Stretch.Fill;
                    break;

                case "Fit":
                    stretch = Stretch.Uniform;
                    break;

                case "Fill":
                    stretch = Stretch.UniformToFill;
                    break;

                default:
                    Debug.WriteLine("Unknown popup option " + item.Text + "selected");
                    return;
            }
            ForegroundImage.Stretch = BackgroundImage.Stretch = stretch;
        }

        private void ToggleBackground_Click(object sender, RoutedEventArgs e)
        {
            SetBackground(Color.FromArgb(255, 0, 0, 0));
        }

        private void SetBackground(Color color)
        {
            ForegroundImageGrid.Background = MainGrid.Background = new SolidColorBrush(color);
        }

        private async Task<BitmapImage> LoadImage(string id)
        {
            var bitmap = new BitmapImage();
            using (var response = await _oneDrive.Client.Drive.Items[id].Content.Request().GetAsync())
            {
                if (response is MemoryStream)
                {
                    await bitmap.SetSourceAsync(((MemoryStream)response).AsRandomAccessStream());
                }
                else
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await response.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        await bitmap.SetSourceAsync(memoryStream.AsRandomAccessStream());
                    }
                }
            }
            return bitmap;
        }
    }
}
