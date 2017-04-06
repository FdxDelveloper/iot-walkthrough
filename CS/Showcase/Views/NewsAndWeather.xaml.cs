﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Showcase
{
    /// <summary>
    /// Page with customizable news and local/outside weather information.
    /// </summary>
    public sealed partial class NewsAndWeather : Page
    {
        public ObservableCollection<NewsModel> NewsGrid { get { return _news; } }

        private ObservableCollection<NewsModel> _news = new ObservableCollection<NewsModel>();
        private CoreDispatcher uiThreadDispatcher = null;
        private BingNews _bing = new BingNews();
        private OpenWeatherMap _weather = new OpenWeatherMap();

        // Properties.
        private bool _isFahrenheit;

        public NewsAndWeather()
        {
            this.InitializeComponent();
            uiThreadDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _bing.NewsUpdate += NewsUpdate;
            _bing.Start();
            _weather.WeatherUpdate += WeatherUpdate;
            _weather.Start();
            AppServiceBridge.RequestReceived += PropertyUpdate;
            AppServiceBridge.RequestUpdate(new List<string> { "ConfigTemperatureUnit", "temperature", "humidity", "pressure" });
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _bing.NewsUpdate -= NewsUpdate;
            _bing.Stop();

            _weather.WeatherUpdate -= WeatherUpdate;
            _weather.Stop();
            AppServiceBridge.RequestReceived -= PropertyUpdate;
        }

        private async void PropertyUpdate(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            ValueSet message = args.Request.Message;

            if (message.TryGetValue("ConfigTemperatureUnit", out object unit))
            {
                _isFahrenheit = (string)unit == "Fahrenheit";
            }

            if (message.TryGetValue("temperature", out object temperature) | message.TryGetValue("humidity", out object humidity) | message.TryGetValue("pressure", out object pressure))
            {
                await uiThreadDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    if (temperature != null)
                    {
                        Temperature.Text = FormatTemperature((double)temperature);
                    }

                    if (humidity != null)
                    {
                        Humidity.Text = FormatHumidity((double)humidity);
                    }

                    if (pressure != null)
                    {
                        Pressure.Text = FormatPressure((double)pressure);
                    }
                });
            }
        }

        private async void NewsUpdate(object sender, EventArgs args)
        {
            BingNews.NewsUpdateEventArgs news = (BingNews.NewsUpdateEventArgs)args;
            await uiThreadDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                _news.Clear();
                foreach (var x in news.UpdatedNews)
                {
                    _news.Add(x);
                }
            });
        }

        private void News_ItemClick(object sender, ItemClickEventArgs e)
        {
            NewsModel news = (NewsModel)e.ClickedItem;
            Frame.Navigate(typeof(WebViewPage), news.Url);
        }

        private async void WeatherUpdate(object sender, EventArgs args)
        {
            WeatherModel weather = ((OpenWeatherMap.WeatherUpdateEventArgs)args).UpdatedWeather;
            await uiThreadDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutsideIcon.Source = new BitmapImage(new Uri(weather.Icon));
                OutsideCondition.Text = weather.Condition;
                OutsideTemperature.Text = FormatTemperature(weather.Temperature);
                OutsideHumidity.Text = FormatHumidity(weather.Humidity);
                OutsidePressure.Text = FormatPressure(weather.Pressure);
            });
        }

        private string FormatTemperature(double temperature)
        {
            if (_isFahrenheit)
            {
                return (temperature * 1.8 + 32).ToString("N1") + " °F";
            }
            return temperature.ToString("N1") + " °C";
        }

        private string FormatHumidity(double humidity)
        {
            return humidity.ToString("N1") + "%";
        }

        private string FormatPressure(double pressure)
        {
            return pressure.ToString("N1") + " kPa";
        }
    }
}
