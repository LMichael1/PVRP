using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public class Route
    {
        public Vehicle Vehicle { get; set; }
        public List<Point> Points { get; set; }
        public int Day { get; set; }
        public int StartTime { get; set; }
        public string StartTimeString => TimeSpan.FromSeconds(StartTime).ToString(@"dd\.hh\:mm\:ss");
        public double Distance
        {
            get
            {
                double length = 0.0;

                for (int i = 0; i < Points.Count - 1; i++)
                {
                    length += Points[i].Distances[Points[i + 1].ID];
                }

                return length;
            }
        }
        public double DistanceKm => Math.Round(Distance / 1000.0, 1);
        public double Fitness
        {
            get
            {
                var length = 0.0;
                var time = StartTime;

                for (int i = 0; i < Points.Count - 1; i++)
                {
                    length += Points[i].Distances[Points[i + 1].ID];

                    if (Points[i].Times[Points[i + 1].ID] >= 800000000)
                    {
                        length = 800000000;
                        return length;
                    }

                    time += Points[i].Times[Points[i + 1].ID];

                    // Если попадает в окно

                    var window = Points[i + 1].TimeWindows.FirstOrDefault(window => time >= window.Start
                        && time + Points[i + 1].ServiceTime <= window.End);

                    if (window != null)
                    {
                        time += Points[i + 1].ServiceTime;
                        continue;
                    }

                    // Если попадает в окно, но не успевает загрузиться

                    window = Points[i + 1].TimeWindows.FirstOrDefault(window => time >= window.Start
                        && time <= window.End);

                    if (window != null)
                    {
                        time += Points[i + 1].ServiceTime;

                        var penalty = (time - window.End) * Points[i + 1].PenaltyLate / 60;
                        length += penalty;

                        continue;
                    }

                    // Если приехал раньше или позже

                    var minTime = int.MaxValue;
                    var nearestWindow = Points[i + 1].TimeWindows.FirstOrDefault();

                    foreach (var timeWindow in Points[i + 1].TimeWindows)
                    {
                        if (Math.Abs(time - timeWindow.Start) < minTime)
                        {
                            minTime = time - timeWindow.Start;
                            nearestWindow = timeWindow;
                        }

                        if (Math.Abs(time - timeWindow.End) < minTime)
                        {
                            minTime = time - timeWindow.End;
                            nearestWindow = timeWindow;
                        }
                    }

                    if (minTime > 0) // опоздал
                    {
                        time += Points[i + 1].ServiceTime;

                        var penalty = minTime * Points[i + 1].PenaltyLate / 60;
                        length += penalty;

                        continue;
                    }
                    else // приехал раньше
                    {
                        time += Math.Abs(minTime); // ждёт открытия
                        time += Points[i + 1].ServiceTime;

                        var penalty = Math.Abs(minTime) * Points[i + 1].PenaltyWait / 60;
                        length += penalty;
                    }
                }

                if (length < 0)
                {
                    length = double.MaxValue;
                }
                return length;
            }
        }
        public string EndTimeString => TimeSpan.FromSeconds(EndTime).ToString(@"dd\.hh\:mm\:ss");

        public double FitnessKm => Math.Round(Fitness / 1000.0, 1);
        public bool IsEmpty => Points.Count == 2;

        public int EndTime
        {
            get
            {
                var time = StartTime;

                for (int i = 0; i < Points.Count - 1; i++)
                {
                    time += Points[i].Times[Points[i + 1].ID];

                    // Если попадает в окно

                    var window = Points[i + 1].TimeWindows.FirstOrDefault(window => time >= window.Start
                        && time + Points[i + 1].ServiceTime <= window.End);

                    if (window != null)
                    {
                        time += Points[i + 1].ServiceTime;
                        continue;
                    }

                    // Если попадает в окно, но не успевает загрузиться

                    window = Points[i + 1].TimeWindows.FirstOrDefault(window => time >= window.Start
                        && time <= window.End);

                    if (window != null)
                    {
                        time += Points[i + 1].ServiceTime;

                        continue;
                    }

                    // Если приехал раньше или позже

                    var minTime = int.MaxValue;
                    var nearestWindow = Points[i + 1].TimeWindows.FirstOrDefault();

                    foreach (var timeWindow in Points[i + 1].TimeWindows)
                    {
                        if (Math.Abs(time - timeWindow.Start) < minTime)
                        {
                            minTime = time - timeWindow.Start;
                            nearestWindow = timeWindow;
                        }

                        if (Math.Abs(time - timeWindow.End) < minTime)
                        {
                            minTime = time - timeWindow.End;
                            nearestWindow = timeWindow;
                        }
                    }

                    if (minTime > 0) // опоздал
                    {
                        time += Points[i + 1].ServiceTime;

                        continue;
                    }
                    else // приехал раньше
                    {
                        time += Math.Abs(minTime); // ждёт открытия
                        time += Points[i + 1].ServiceTime;
                    }
                }

                return time;
            }
        }

        public Route(int day, Point depot, Vehicle vehicle)
        {
            Day = day;
            Points = new List<Point>
            {
                depot,
                depot
            };
            StartTime = -1;
            Vehicle = vehicle;
        }

        public void AddPoint(Point point)
        {
            Points.Insert(Points.Count - 1, point);

            if (StartTime == -1)
            {
                StartTime = point.TimeWindows[0].Start - Points[0].Times[point.ID];
            }
        }
        public void InsertPoint(int index, Point point)
        {
            Points.Insert(index, point);

            StartTime = Points[1].TimeWindows[0].Start - Points[0].Times[Points[1].ID];
        }

        public void RemovePoint(int index)
        {
            Points.RemoveAt(index);

            if (Points.Count > 2)
            {
                StartTime = Points[1].TimeWindows[0].Start - Points[0].Times[Points[1].ID];
            }
            else
            {
                StartTime = -1;
            }
        }

        public void RemovePoint(Point point)
        {
            Points.Remove(point);

            if (Points.Count > 2)
            {
                StartTime = Points[1].TimeWindows[0].Start - Points[0].Times[Points[1].ID];
            }
            else
            {
                StartTime = -1;
            }
        }


        public object Clone()
        {
            var clone = new Route(Day, Points[0], (Vehicle)Vehicle.Clone());

            clone.Points = new List<Point>();

            foreach (var point in Points)
            {
                clone.Points.Add(point);
            }

            clone.StartTime = StartTime;

            return clone;
        }
    }
}
