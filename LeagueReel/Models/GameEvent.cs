using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueReel.Models
{
    public class GameEvent
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public double EventTime { get; set; }
        public string? KillerName { get; set; }
        public string[]? Asisters { get; set; }
    }


    public class EventResponse
    {
        public List<GameEvent> Events { get; set; }
    }
}
