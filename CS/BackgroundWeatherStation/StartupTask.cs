﻿using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.System.Threading;

namespace BackgroundWeatherStation
{
    public sealed class StartupTask : IBackgroundTask
    {
        private WeatherStation _station = new WeatherStation();
        private IoTHubClient _client = new IoTHubClient();
        private ThreadPoolTimer _timer;
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a BackgroundTaskDeferral and hold it forever if initialization is sucessful.
            _deferral = taskInstance.GetDeferral();
            if (!await _station.InitI2c())
            {
                Debug.WriteLine("I2C initialization failed");
                _deferral.Complete();
                return;
            }
            await _client.InitAsync();

            taskInstance.Canceled += (IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) =>
            {
                Debug.WriteLine("Cancelled: reason " + reason);
            };

            _timer = ThreadPoolTimer.CreatePeriodicTimer(LogSensorData, TimeSpan.FromSeconds(5));
        }

        private async void LogSensorData(ThreadPoolTimer timer)
        {
            var temperature = _station.ReadTemperature();
            var humidity = _station.ReadHumidity();
            var pressure = _station.ReadPressure();

            _client.LogDataAsync(temperature, humidity, pressure);

            ValueSet message = new ValueSet();
            message["temperature"] = temperature;
            message["humidity"] = humidity;
            message["pressure"] = pressure;
            await AppServiceBridge.SendMessageAsync(message);

            Debug.WriteLine("Logged data");
        }
    }
}
