using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeagueReel.Models;
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
                _gameClientMonitor.StartMonitoring(TimeSpan.FromSeconds(10), this);
                Task.Run(() => TestLeagueConnection());
            }
        }

        public void OnNavigatedFrom()
        {
            _cts.Cancel();
            _gameClientMonitor.StopMonitoring();
        }

        public void OnCounterIncrement(string fileName)
        {
            if (_screenRecorderService.IsRecording)
            {
                Task.Run(() => _screenRecorderService.SaveHighlight(fileName, 100), _cts.Token);
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
                // Task was cancelled, handle if necessary.
            }
            catch (Exception ex)
            {
                GameClientStatus = "Waiting for game client...";
            }
        }

        private async Task WaitForGameClient()
        {
            while (!GameClientConnected)
            {

                if (_cts.Token.IsCancellationRequested)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                }

                await Task.Delay(4000, _cts.Token);
            }

            _screenRecorderService.Start();

            GameClientStatus = "Connected and Recording";
            IsConnecting = false;
        }
    }

}
