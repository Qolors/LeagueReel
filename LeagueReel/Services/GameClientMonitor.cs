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
        //TODO --> This is kind of a hack for now, need to implement a better way to adjust the timer
        private bool adjustTimer = false;

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

                if (adjustTimer)
                {
                    timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
                    adjustTimer = false;
                }

                Debug.WriteLine("Trying");

                dashBoardViewModel.GameClientConnected = true;

                if (!eventProcessor.HasName) eventProcessor.SetUserName(await clientService.GetGameData("https://127.0.0.1:2999/liveclientdata/activeplayername"));

                if (eventProcessor.ProcessEvents(data))
                {
                    Debug.WriteLine("Processed");
                    if (eventProcessor.HasGameEnded)
                    {
                        Debug.WriteLine("Game End");
                        dashBoardViewModel.Flush();
                        eventProcessor.Flush();
                        
                    }
                    else
                    {
                        dashBoardViewModel.OnCounterIncrement(eventProcessor.GetLatestEventId.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("No Game Client Connection");
                dashBoardViewModel.Flush();
                eventProcessor.Flush();
                if (!adjustTimer)
                {
                    timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
                    adjustTimer = true;
                }
                
            }
        }
    }

}
