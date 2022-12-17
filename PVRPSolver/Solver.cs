using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public class Solver
    {
        private IList<Vehicle> vehicles;
        private IList<Point> points;
        private int forecastDepth;

        public Solution Solution { get; private set; }

        public Solver(IList<Vehicle> vehicles, IList<Point> points, int forecastDepth)
        {
            this.vehicles = vehicles;
            this.points = points;
            this.forecastDepth = forecastDepth;
        }

        public void RunOptimization()
        {
            Console.WriteLine(Solution.TotalFitnessKm);
            OptimizeDayRoutes();
            Console.WriteLine(Solution.TotalFitnessKm);
            ShiftPatterns();
            Console.WriteLine(Solution.TotalFitnessKm);
            OptimizeDayRoutes();
            Console.WriteLine(Solution.TotalFitnessKm);
            SwapPatterns();
            Console.WriteLine(Solution.TotalFitnessKm);
        }

        private void SwapPatterns()
        {
            var shouldRestart = false;
            var customerPoints = points.Where(p => !p.IsDepot).ToList();

            do
            {
                shouldRestart = false;

                for (int i = 0; i < customerPoints.Count - 1; i++)
                {
                    for (int j = i + 1; j < customerPoints.Count; j++)
                    {
                        var solution = (Solution)Solution.Clone();

                        var routesI = solution.Routes.Where(r => r.Points.Contains(customerPoints[i]));
                        var routesJ = solution.Routes.Where(r => r.Points.Contains(customerPoints[j]));

                        foreach (var route in routesI)
                        {
                            route.RemovePoint(customerPoints[i]);
                        }

                        foreach (var route in routesJ)
                        {
                            route.RemovePoint(customerPoints[j]);
                        }

                        var swapPoints = new List<Point>(2)
                        {
                            customerPoints[j],
                            customerPoints[i]
                        };

                        var patternKeys = new List<int>(2);

                        foreach (var point in swapPoints)
                        {
                            var bestPattern = point.Patterns.First(pattern => pattern.Key == point.CurrentPattern);
                            var bestPatternRoutes = new List<Route>();
                            var bestPatternFitness = double.MaxValue;

                            foreach (var pattern in point.Patterns)
                            {
                                var patternRoutes = new List<Route>();

                                foreach (var day in pattern.Value)
                                {
                                    var dayRoutes = solution.Routes.Where(r => r.Day == day).ToList();
                                    Route bestDayRoute = null;
                                    var bestDayRouteFitness = double.MaxValue;

                                    foreach (var dayRoute in dayRoutes)
                                    {
                                        for (int k = 1; k < dayRoute.Points.Count; k++)
                                        {
                                            dayRoute.InsertPoint(k, point);
                                            var fitness = dayRoute.Fitness;
                                            if (fitness < bestDayRouteFitness)
                                            {
                                                bestDayRoute = (Route)dayRoute.Clone();
                                                bestDayRouteFitness = fitness;
                                            }
                                            dayRoute.RemovePoint(k);
                                        }
                                    }

                                    patternRoutes.Add(bestDayRoute);
                                }

                                var tempRoutes = solution.Routes.Where(r => !patternRoutes.Any(pr => pr.Day == r.Day && pr.Vehicle.ID == r.Vehicle.ID)).ToList();
                                tempRoutes.AddRange(patternRoutes);
                                var newFitness = tempRoutes.Sum(r => r.Fitness);

                                if (newFitness < bestPatternFitness && Math.Abs(newFitness - bestPatternFitness) > 1e-10)
                                {
                                    bestPatternFitness = newFitness;
                                    bestPatternRoutes = patternRoutes;
                                    bestPattern = pattern;
                                }
                            }

                            solution.Routes = solution.Routes.Where(r => !bestPatternRoutes.Any(pr => pr.Day == r.Day && pr.Vehicle.ID == r.Vehicle.ID)).ToList();
                            solution.Routes.AddRange(bestPatternRoutes);
                            patternKeys.Add(bestPattern.Key);
                        }

                        // replace and restart if found better
                        if (solution.TotalFitness < Solution.TotalFitness)
                        {
                            swapPoints[0].CurrentPattern = patternKeys[0];
                            swapPoints[1].CurrentPattern = patternKeys[1];
                            Solution = solution;
                            shouldRestart = true;

                            break;
                        }
                    }

                    if (shouldRestart)
                    {
                        break;
                    }
                }
            }
            while (shouldRestart);
        }

        private void ShiftPatterns()
        {
            var shouldRestart = false;

            do
            {
                foreach (var route in Solution.Routes.Where(r => !r.IsEmpty))
                {
                    for (int i = 1; i < route.Points.Count - 1; i++)
                    {
                        var point = route.Points[i];

                        if (point.Patterns.Count < 2)
                        {
                            shouldRestart = false;
                            continue;
                        }

                        var currentPattern = point.Patterns[point.CurrentPattern];
                        var currentRoutes = Solution.Routes.Where(r => r.Points.Contains(point)).ToList();
                        var currentFitness = currentRoutes.Sum(r => r.Fitness);

                        var otherPatterns = point.Patterns.Where(pattern => pattern.Key != point.CurrentPattern).ToList();
                        var bestPattern = point.Patterns.First(pattern => pattern.Key == point.CurrentPattern);
                        var bestPatternRoutes = currentRoutes.Select(r => (Route)r.Clone()).ToList();
                        var bestPatternFitness = Solution.TotalFitness;

                        foreach (var patternRoute in currentRoutes)
                        {
                            patternRoute.RemovePoint(point);
                        }

                        foreach (var pattern in otherPatterns)
                        {
                            var patternRoutes = new List<Route>();

                            foreach (var day in pattern.Value)
                            {
                                var dayRoutes = Solution.Routes.Where(r => r.Day == day).ToList();
                                Route bestDayRoute = null;
                                var bestDayRouteFitness = double.MaxValue;

                                foreach (var dayRoute in dayRoutes)
                                {
                                    for (int j = 1; j < dayRoute.Points.Count; j++)
                                    {
                                        dayRoute.InsertPoint(j, point);
                                        var fitness = dayRoute.Fitness;
                                        if (fitness < bestDayRouteFitness)
                                        {
                                            bestDayRoute = (Route)dayRoute.Clone();
                                            bestDayRouteFitness = fitness;
                                        }
                                        dayRoute.RemovePoint(j);
                                    }
                                }

                                patternRoutes.Add(bestDayRoute);
                            }

                            var tempRoutes = Solution.Routes.Where(r => !patternRoutes.Any(pr => pr.Day == r.Day && pr.Vehicle.ID == r.Vehicle.ID)).ToList();
                            tempRoutes.AddRange(patternRoutes);
                            var newFitness = tempRoutes.Sum(r => r.Fitness);

                            if (newFitness < bestPatternFitness && Math.Abs(newFitness - bestPatternFitness) > 1e-10)
                            {
                                bestPatternFitness = newFitness;
                                bestPatternRoutes = patternRoutes;
                                bestPattern = pattern;
                            }
                        }

                        if (point.CurrentPattern != bestPattern.Key)
                        {
                            point.CurrentPattern = bestPattern.Key;
                            shouldRestart = true;
                        }
                        else
                        {
                            shouldRestart = false;
                        }

                        Solution.Routes = Solution.Routes.Where(r => !bestPatternRoutes.Any(pr => pr.Day == r.Day && pr.Vehicle.ID == r.Vehicle.ID)).ToList();
                        Solution.Routes.AddRange(bestPatternRoutes);

                        //Console.WriteLine("Routes count: {0}, {1}", Solution.Routes.Where(r => !r.IsEmpty).Count(), Solution.TotalFitnessKm);

                        if (shouldRestart)
                        {
                            break;
                        }
                    }
                    if (shouldRestart)
                    {
                        break;
                    }
                }
            }
            while (shouldRestart);

            Solution.Routes = Solution.Routes.OrderBy(r => r.Day).ToList();
        }

        private void OptimizeDayRoutes()
        {
            for (int day = 1; day <= forecastDepth; day++)
            {
                var dayRoutes = Solution.Routes.Where(r => r.Day == day).ToList();

                if (dayRoutes.Count > 1)
                {
                    ProcessShift1_0(dayRoutes);

                    Process2opt(dayRoutes);

                    ProcessSwap1_1(dayRoutes);

                    Process2opt(dayRoutes);

                    ProcessShift0_1(dayRoutes);

                    Process2opt(dayRoutes);
                }
                else
                {
                    Process2opt(dayRoutes);
                }
            }
        }

        private void ProcessShift0_1(IList<Route> routes)
        {
            for (int i = 0; i < routes.Count - 1; i++)
            {
                for (int j = i + 1; j < routes.Count; j++)
                {
                    Shift(routes[j], routes[i]);
                }
            }
        }

        private void ProcessShift1_0(IList<Route> routes)
        {
            for (int i = 0; i < routes.Count - 1; i++)
            {
                for (int j = i + 1; j < routes.Count; j++)
                {
                    Shift(routes[i], routes[j]);
                }
            }
        }

        private void Shift(Route first, Route second)
        {
            var shouldRestart = true;

            //if (second.IsEmpty)
            //{
            //    Console.WriteLine();
            //}

            while (shouldRestart)
            {
                shouldRestart = false;

                var minLength = first.Fitness + second.Fitness;

                for (int i = 1; i < first.Points.Count - 1; i++)
                {
                    var minIndex = 0;

                    for (int j = 1; j < second.Points.Count; j++)
                    {
                        var newFirst = (Route)first.Clone();
                        var newSecond = (Route)second.Clone();

                        newSecond.InsertPoint(j, newFirst.Points[i]);
                        newFirst.RemovePoint(i);

                        var newLength = newFirst.Fitness + newSecond.Fitness;

                        if (newLength < minLength)
                        {
                            minLength = newLength;
                            minIndex = j;
                        }
                    }

                    if (minIndex > 0)
                    {
                        second.InsertPoint(minIndex, first.Points[i]);
                        first.RemovePoint(i);
                        shouldRestart = true;
                        break;
                    }
                }
            }
        }

        private void ProcessSwap1_1(IList<Route> routes)
        {
            for (int i = 0; i < routes.Count - 1; i++)
            {
                for (int j = i + 1; j < routes.Count; j++)
                {
                    Swap(routes[i], routes[j]);
                }
            }
        }

        private void Swap(Route first, Route second)
        {
            if (first.IsEmpty || second.IsEmpty) return;

            var shouldRestart = true;

            while (shouldRestart)
            {
                shouldRestart = false;

                var totalLength = first.Fitness + second.Fitness;

                for (int i = 1; i < first.Points.Count - 1; i++)
                {
                    for (int j = 1; j < second.Points.Count - 1; j++)
                    {
                        var newFirst = (Route)first.Clone();
                        var newSecond = (Route)second.Clone();

                        var firstPoint = newFirst.Points[i];
                        var secondPoint = newSecond.Points[j];

                        newFirst.RemovePoint(i);
                        newSecond.RemovePoint(j);

                        newFirst.InsertPoint(i, secondPoint);
                        newSecond.InsertPoint(j, firstPoint);

                        var newLength = newFirst.Fitness + newSecond.Fitness;

                        if (newLength < totalLength)
                        {
                            first.RemovePoint(i);
                            second.RemovePoint(j);

                            first.InsertPoint(i, secondPoint);
                            second.InsertPoint(j, firstPoint);

                            shouldRestart = true;
                            break;
                        }
                    }

                    if (shouldRestart) break;
                }
            }
        }

        private void Process2opt(IList<Route> routes)
        {
            foreach (var route in routes)
            {
                Process2opt(route);
            }
        }

        private void Process2opt(Route route)
        {
            if (route.Points.Count < 4) return;

            var shouldRestart = true;

            while (shouldRestart)
            {
                var bestDistance = route.Fitness;

                for (int i = 1; i <= route.Points.Count - 3; i++)
                {
                    shouldRestart = false;

                    for (int k = i + 1; k <= route.Points.Count - 2; k++)
                    {
                        var newRoute = (Route)route.Clone();
                        newRoute.Points.Reverse(i, k - i + 1);

                        var newDistance = newRoute.Fitness;

                        if (newDistance < 0)
                        {
                            Console.WriteLine(newDistance);
                            Console.WriteLine(newRoute.Fitness);
                        }

                        if (newDistance < bestDistance)
                        {
                            route.Points.Reverse(i, k - i + 1);
                            shouldRestart = true;
                            break;
                        }
                    }

                    if (shouldRestart) break;
                }
            }
        }

        public Solution GetInitialSolution()
        {
            var depot = points.First(point => point.IsDepot);

            Solution = new Solution(forecastDepth, vehicles, depot);

            var currentPoint = depot;
            var processedPointsCount = 1;

            while (processedPointsCount < points.Count)
            {
                if (currentPoint != depot)
                {
                    var patternID = currentPoint.Patterns.Keys.First();
                    var days = currentPoint.Patterns[patternID];
                    currentPoint.CurrentPattern = patternID;

                    foreach (var day in days)
                    {
                        var route = Solution.Routes.First(route => route.Day == day);
                        route.AddPoint(currentPoint);

                        //if (route.EndTime - route.StartTime > 28800)
                        //{

                        //}
                    }

                    currentPoint.IsVisited = true;
                    processedPointsCount++;
                }

                var destinationsIDs = points.Where(point => !point.IsDepot && !point.IsVisited).Select(point => point.ID).ToList();

                if (destinationsIDs.Count == 0) break;

                var nearestDestinationsIDs = currentPoint.Distances.Where(item => destinationsIDs.Contains(item.Key)).OrderBy(item => item.Value).Select(item => item.Key);

                currentPoint = points.FirstOrDefault(point => point.ID == nearestDestinationsIDs.First());
            }

            return Solution;
        }
    }
}
