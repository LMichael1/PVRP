using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public class Solution : ICloneable
    {
        public List<Route> Routes { get; set; }
        public double TotalDistance => Routes.Sum(route => route.Distance);
        public double TotalDistanceKm => Math.Round(TotalDistance / 1000.0, 1);
        public double TotalFitness => Routes.Sum(route => route.Fitness);
        public double TotalFitnessKm => Math.Round(TotalFitness / 1000.0, 1);
        public int NotEmptyRoutesCount => Routes.Count(route => route.Points.Count > 2);

        public Solution()
        {
            Routes = new List<Route>();
        }

        public Solution(int forecastDepth, IList<Vehicle> vehicles, Point depot)
        {
            Routes = new List<Route>();

            foreach (var vehicle in vehicles)
            {
                for (int i = 1; i <= forecastDepth; i++)
                {
                    Routes.Add(new Route(i, depot, vehicle));
                }
            }
        }

        public bool IsValid(IList<Point> points)
        {
            var isValid = true;

            foreach (var point in points)
            {
                if (point.IsDepot)
                {
                    foreach (var route in Routes)
                    {
                        if (route.Points[0] != point || route.Points.Last() != point)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    var pattern = point.Patterns[point.CurrentPattern];

                    foreach (var day in pattern)
                    {
                        var count = Routes.Where(r => r.Day == day && r.Points.Contains(point)).Count();
                        if (count != 1)
                        {
                            return false;
                        }
                    }
                }
            }

            return isValid;
        }

        public object Clone()
        {
            var solution = new Solution
            {
                Routes = Routes.Select(route => (Route)route.Clone()).ToList()
            };
            return solution;
        }
    }
}
