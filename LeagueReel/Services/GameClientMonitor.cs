using LeagueReel.Helpers;
using LeagueReel.Models;
using LeagueReel.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueReel.Services
{
    public class GameClientMonitor
    {
        private readonly GameClientService clientService;
        private Timer timer;
        private EventProcessor eventProcessor;

        public GameClientMonitor(GameClientService clientService)
        {
            this.clientService = clientService;
            eventProcessor = new EventProcessor();
        }

        public void StartMonitoring(TimeSpan interval, DashboardViewModel dashboardViewModel)
        {
            timer = new Timer(async _ => await CheckGameClient(dashboardViewModel), null, TimeSpan.Zero, interval);
        }

        public void StopMonitoring()
        {
            timer?.Dispose();
        }

        private async Task CheckGameClient(DashboardViewModel dashBoardViewModel)
        {
            try
            {
                var data = await clientService.GetGameData("https://127.0.0.1:2999/liveclientdata/eventdata");

                dashBoardViewModel.GameClientConnected = true;

                if(eventProcessor.ProcessEvents(data))
                {
                    dashBoardViewModel.OnCounterIncrement();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get data: " + ex.Message);
                dashBoardViewModel.GameClientConnected = false;
            }
        }
    }

}
