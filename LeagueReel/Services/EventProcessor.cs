using LeagueReel.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq;

namespace LeagueReel.Services
{
    public class EventProcessor
    {
        private int latestEventId = -1;

        private string userName = "";

        public bool HasName = false;

        public bool HasGameEnded = false;

        public int GetLatestEventId => latestEventId;

        public void SetUserName(string name)
        {
            userName = name.Trim('"');
            HasName = true;
        }

        public void Flush()
        {
            latestEventId = -1;
            HasName = false;
            HasGameEnded = false;
        }

        public bool ProcessEvents(string json)
        {
            var eventResponse = JsonConvert.DeserializeObject<EventResponse>(json);

            Debug.WriteLine(latestEventId);

            if (latestEventId == -1 && eventResponse?.Events.Count > 1)
            {
                Debug.WriteLine("Recording Started during active game, setting ID to the current highest");
                latestEventId = eventResponse.Events.Max(x => x.EventID);
                return false;
            }

            foreach (var gameEvent in eventResponse?.Events)
            {
                if (gameEvent.EventID > latestEventId)
                {

                    latestEventId = gameEvent.EventID;

                    if (gameEvent.KillerName != null && gameEvent.KillerName == userName)
                    {
                        return true;
                    }
                    else if (gameEvent.Asisters?.Length > 0)
                    {
                        foreach (var assists in gameEvent.Asisters)
                        {
                            if (assists == userName)
                            {
                                return true;
                            }
                        }
                    }
                    else if (gameEvent.EventName == "GameEnd")
                    {
                        Debug.WriteLine("Game Ended");
                        HasGameEnded = true;
                        return true;
                    }
                }
            }

            return false;
        }
    }

}
