// See https://aka.ms/new-console-template for more information
using PVRPSolver;

Console.WriteLine("Hello, World!");

//var parser = new Parser("001_sched_3410_07112021.txt");
//var parser = new Parser("016_sched_161.txt");
var parser = new Parser("017_sched_162.txt");
//var parser = new Parser("018_sched_163.txt");
//var parser = new Parser("019_sched_164.txt");
//var parser = new Parser("020_sched_165.txt");
//var parser = new Parser("021_sched_171.txt");
//var parser = new Parser("013_sched_3410_03042023.txt");
await parser.Parse();

Console.WriteLine("Forecast depth: {0}", parser.ForecastDepth);
Console.WriteLine("Points count: {0}", parser.Points.Count);
Console.WriteLine("Vehicles count: {0}", parser.Vehicles.Count);

var solver = new Solver(parser.Vehicles, parser.Points, parser.ForecastDepth);

//var solution = solver.GetInitialSolution();
var solution = solver.GetGreedyInitialSolution();

Console.WriteLine("Before optimization: {0} km, {1} routes, {2} km", solver.Solution.TotalFitnessKm, solver.Solution.Routes.Count(r => !r.IsEmpty), solver.Solution.TotalDistanceKm);

foreach (var route in solver.Solution.Routes.Where(r => !r.IsEmpty))
{
    Console.WriteLine("Day: {0}, Start: {1}, End: {2}", route.Day, route.StartTimeString, route.EndTimeString);
}

solver.RunOptimization();

Console.WriteLine("After optimization: {0} km, {1} routes, {2} km", solver.Solution.TotalFitnessKm, solver.Solution.Routes.Count(r => !r.IsEmpty), solver.Solution.TotalDistanceKm);

foreach (var route in solver.Solution.Routes.Where(r => !r.IsEmpty))
{
    Console.WriteLine("Day: {0}, Start: {1}, End: {2}", route.Day, route.StartTimeString, route.EndTimeString);
}

var isValid = solver.Solution.IsValid(parser.Points);
Console.WriteLine("Is valid: {0}", isValid);