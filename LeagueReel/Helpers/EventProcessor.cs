using LeagueReel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LeagueReel.Helpers
{
    public class EventProcessor
    {
        private int latestEventId = -1;

        public bool ProcessEvents(string json)
        {
            var eventResponse = JsonConvert.DeserializeObject<EventResponse>(json);

            foreach (var gameEvent in eventResponse.Events)
            {
                if (gameEvent.EventID > latestEventId)
                {
                    // This is a new event. Process it as needed.
                    

                    // Update latestEventId.
                    latestEventId = gameEvent.EventID;

                    if (gameEvent.KillerName != null && gameEvent.KillerName == "man named Chris")
                    {
                        return true;
                    }
                    else if (gameEvent.Asisters?.Length > 0)
                    {
                        foreach (var assists in gameEvent.Asisters)
                        {
                            if (assists == "man named Chris")
                            {
                                return true;
                            }
                        }
                    }

                    
                }
            }

            return false;
        }
    }

}
