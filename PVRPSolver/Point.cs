using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public class Point
    {
        public int ID { get; }
        public int ID2 { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public bool IsDepot => ID < 0;
        public bool IsVisited { get; set; }
        public List<TimeWindow> TimeWindows { get; }
        public int ServiceTime { get; }
        public int PenaltyLate { get; }
        public int PenaltyWait { get; }
        public Dictionary<int, double> Distances { get; set; }
        public Dictionary<int, int> Times { get; set; }
        public Dictionary<int, List<int>> Patterns { get; set; }
        public int CurrentPattern { get; set; }

        public Point(int id, int id2, double latitude, double longitude, IEnumerable<TimeWindow> timeWindows, 
            int serviceTime, int penaltyLate, int penaltyWait)
        {
            ID = id;
            ID2 = id2;
            Latitude = latitude;
            Longitude = longitude;
            Distances = new Dictionary<int, double>();
            Times = new Dictionary<int, int>();
            Patterns = new Dictionary<int, List<int>>();
            TimeWindows = timeWindows.OrderBy(window => window.Start).ToList();
            ServiceTime = serviceTime;
            PenaltyLate = penaltyLate;
            PenaltyWait = penaltyWait;
            CurrentPattern = -1;
        }

        public override string ToString()
        {
            return string.Format("ID: {0}\nLatitude: {1}\nLongitude: {2}",
                ID, Latitude, Longitude);
        }
    }
}
