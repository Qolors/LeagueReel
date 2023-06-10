using CommunityToolkit.Mvvm.ComponentModel;
using LeagueReel.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui.Common.Interfaces;

namespace LeagueReel.ViewModels
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ScreenRecorderService _screenRecorderService;
        private readonly GameClientMonitor _gameClientMonitor;

        [ObservableProperty]
        private bool gameClientConnected = false;
        [ObservableProperty]
        private bool isConnecting = true;
        [ObservableProperty]
        private string gameClientStatus = "Waiting for game client...";

        public DashboardViewModel(ScreenRecorderService screenRecorderService, GameClientMonitor gameClientMonitor)
        {
            _screenRecorderService = screenRecorderService;
            _gameClientMonitor = gameClientMonitor;
        }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _gameClientMonitor.StartMonitoring(TimeSpan.FromSeconds(5), this);
                Task.Run(() => TestLeagueConnection());
            }
        }

        public void OnNavigatedFrom()
        {
            //TODO --> Determine if this is needed
            //_cts.Cancel();
            //_gameClientMonitor.StopMonitoring();
        }

        public void OnCounterIncrement(string fileName)
        {
            if (_screenRecorderService.IsRecording)
            {
                Task.Run(() => _screenRecorderService.SaveHighlight(fileName), _cts.Token);
            }
        }

        private async Task TestLeagueConnection()
        {
            try
            {
                await WaitForGameClient();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                GameClientStatus = "Waiting for game client...";
            }
        }

        private void StopRecording()
        {
            _screenRecorderService.Stop();
            Task.Run(() => WaitForGameClient());
        }

        private async Task WaitForGameClient()
        {
            while (!GameClientConnected)
            {

                if (_cts.Token.IsCancellationRequested)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                }

                Debug.WriteLine("Not Connected Yet");

                await Task.Delay(15000, _cts.Token);
            }

            _screenRecorderService.Start();

            GameClientStatus = "Connected and Recording";
            IsConnecting = false;
        }

        public void Flush()
        {
            GameClientStatus = "Waiting for game to start...";
            IsConnecting = true;
            GameClientConnected = false;

            if (_screenRecorderService.IsRecording)
            {
                StopRecording();
            }
        }
    }

}
