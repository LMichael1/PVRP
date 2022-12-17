using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public enum LineType
    {
        None,
        Demand,
        TimeWindows,
        Point,
        Vehicle,
        DistancesMatrix,
        TimesMatrix,
        CommonParameters,
        Pattern
    }

    public class Parser
    {
        private readonly string _filePath;

        private int _timeWindowsCount;

        public List<Point> Points { get; }
        public List<Vehicle> Vehicles { get; }
        public int ForecastDepth { get; private set; }

        public Parser(string filePath)
        {
            _filePath = filePath;
            Points = new List<Point>();
            Vehicles = new List<Vehicle>();
        }

        public async Task Parse()
        {
            using (StreamReader sr = new StreamReader(_filePath, Encoding.Default))
            {
                string line;

                var lineType = LineType.None;

                while ((line = await sr.ReadLineAsync()) != null)
                {
                    if (line.Contains("DEMAND_SIZE"))
                    {
                        lineType = LineType.Demand;
                        continue;
                    }

                    if (line.Contains("TIME_WINDOWS"))
                    {
                        lineType = LineType.TimeWindows;
                        continue;
                    }

                    if (line.Contains("TIME_WINDOWS") || line.Contains("GEO-FENCE"))
                    {
                        lineType = LineType.None;
                        continue;
                    }

                    if (line.Contains("COMMON_PARAMETERS"))
                    {
                        lineType = LineType.CommonParameters;
                        continue;
                    }

                    if (line.Contains("POINTS"))
                    {
                        lineType = LineType.Point;
                        continue;
                    }

                    if (line.Contains("=CARS="))
                    {
                        lineType = LineType.Vehicle;
                        continue;
                    }

                    if (line.Contains("DISTANCE"))
                    {
                        lineType = LineType.DistancesMatrix;
                        continue;
                    }

                    if (line.Contains("TIME"))
                    {
                        lineType = LineType.TimesMatrix;
                        continue;
                    }

                    if (lineType == LineType.DistancesMatrix && line[0] != '0')
                    {
                        continue;
                    }

                    if (lineType == LineType.TimesMatrix && line[0] != '0')
                    {
                        lineType = LineType.None;
                        continue;
                    }

                    if (line.Contains("AVAILABLE_PATTERN"))
                    {
                        lineType = LineType.Pattern;
                        continue;
                    }

                    if (line.Contains("CAR_LINK_DIST_TIME"))
                    {
                        break;
                    }

                    switch (lineType)
                    {
                        case LineType.TimeWindows:
                            ParseTimeWindows(line);
                            break;
                        case LineType.Point:
                            ParsePoints(line);
                            break;
                        case LineType.Vehicle:
                            ParseVehicles(line);
                            break;
                        case LineType.DistancesMatrix:
                            ParseDistancesMatrix(line);
                            break;
                        case LineType.TimesMatrix:
                            ParseTimesMatrix(line);
                            break;
                        case LineType.CommonParameters:
                            ParseCommonParameters(line);
                            break;
                        case LineType.Pattern:
                            ParsePattern(line);
                            break;
                        default:
                            break;
                    }
                }
            }

            var pointsIDs = Points.Select(point => point.ID);

            foreach (var point in Points)
            {
                var uncommonIDs = pointsIDs.Except(point.Distances.Keys);

                foreach (var id in uncommonIDs)
                {
                    point.Distances.Add(id, 0.0);
                }
            }
        }

        private void ParseTimeWindows(string line)
        {
            _timeWindowsCount = Convert.ToInt32(line);
        }

        private void ParsePoints(string line)
        {
            var splitted = line.Split(new[] { ',' });

            var id2 = Convert.ToInt32(splitted[0]);
            var id = Convert.ToInt32(splitted[1]);
            var latitude = Convert.ToDouble(splitted[2], CultureInfo.InvariantCulture);
            var longitude = Convert.ToDouble(splitted[3], CultureInfo.InvariantCulture);

            var timeWindows = new List<TimeWindow>();
            var index = 4;

            for (int i = 0; i < _timeWindowsCount; i++)
            {
                var start = Convert.ToInt32(splitted[index]);
                var end = Convert.ToInt32(splitted[index + 1]);

                if (start != end)
                {
                    timeWindows.Add(new TimeWindow(start, end));
                }

                index += 2;
            }

            var serviceTime = Convert.ToInt32(splitted[index]);
            var penaltyLate = Convert.ToInt32(splitted[index + 2]);
            var penaltyWait = Convert.ToInt32(splitted[index + 3]);

            Points.Add(new Point(id, id2, latitude, longitude, timeWindows, serviceTime, penaltyLate, penaltyWait));
        }

        private void ParseVehicles(string line)
        {
            var splitted = line.Split(new[] { ',' });

            var id = Convert.ToInt32(splitted[0]);

            var vehicle = new Vehicle(id);
            Vehicles.Add(vehicle);
        }

        private void ParseDistancesMatrix(string line)
        {
            var splitted = line.Split(new[] { ',' });

            var firstIndex = Convert.ToInt32(splitted[1]);
            var secondIndex = Convert.ToInt32(splitted[2]);

            var distance = Convert.ToDouble(splitted[3], CultureInfo.InvariantCulture);

            Points[firstIndex].Distances.Add(Points[secondIndex].ID, distance);
        }

        private void ParseTimesMatrix(string line)
        {
            var splitted = line.Split(new[] { ',' });

            var firstIndex = Convert.ToInt32(splitted[1]);
            var secondIndex = Convert.ToInt32(splitted[2]);

            var time = Convert.ToInt32(splitted[3]);

            Points[firstIndex].Times.Add(Points[secondIndex].ID, time);
        }

        private void ParseCommonParameters(string line)
        {
            if (line.Contains("ForecastDepth"))
            {
                var splitted = line.Split(new[] {','});
                ForecastDepth = Convert.ToInt32(splitted[1]);
            }
        }

        private void ParsePattern(string line)
        {
            var splitted = line.Split(new[] {','});

            var pointID2 = Convert.ToInt32(splitted[0]);
            var patternID = Convert.ToInt32(splitted[1]);
            var day = Convert.ToInt32(splitted[2]);

            var point = Points.First(point => point.ID2 == pointID2);

            if (!point.Patterns.ContainsKey(patternID))
            {
                point.Patterns[patternID] = new List<int>();
            }

            point.Patterns[patternID].Add(day);
        }
    }
}
